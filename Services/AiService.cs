using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SKIPPY.Services;

/// <summary>
/// STT (SiliconFlow TeleSpeechASR) + Chat (SiliconFlow) + tool calling.
/// Config loaded from ai.json, shared with settings dialog.
/// </summary>
public class AiService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(60) };
    private readonly Func<AiConfigData> _getConfig;

    public AiService(Func<AiConfigData> getConfig)
    {
        _getConfig = getConfig;
    }

    // ── mic detection ─────────────────────────────────────────

    public static bool HasMicrophone()
    {
        try
        {
            if (OperatingSystem.IsLinux())
                return Run("which", "arecord") || Run("which", "parec");
            if (OperatingSystem.IsMacOS())
                return Run("which", "sox") || Run("which", "rec");
            if (OperatingSystem.IsWindows())
                return true; // assume yes on Windows — PowerShell audio capture always works
        }
        catch { }
        return false;
    }

    /// <summary>record audio to a WAV file, return path or null on failure</summary>
    public static string? RecordAudio(string outputPath)
    {
        try
        {
            File.Delete(outputPath);
            if (OperatingSystem.IsLinux())
            {
                if (Run("arecord", $"-d 5 -f cd -t wav {outputPath}") && File.Exists(outputPath))
                    return outputPath;
                if (Run("parec", $"--file-format=wav {outputPath}") && File.Exists(outputPath))
                    return outputPath;
            }
            if (OperatingSystem.IsMacOS())
            {
                if (Run("rec", $"-r 16000 -c 1 -b 16 {outputPath} trim 0 5") && File.Exists(outputPath))
                    return outputPath;
            }
            if (OperatingSystem.IsWindows())
            {
                // PowerShell audio recorder fallback: record 5s via built-in .NET
                var ps = $@"
Add-Type -AssemblyName System.Speech
$r = New-Object System.Speech.Recognition.SpeechRecognitionEngine
$r.SetInputToDefaultAudioDevice()
$r.Recognize()
";
                // PSH audio capture is unreliable; return null → user gets text input
                // Windows users should install arecord via MSYS2 or use text input
            }
        }
        catch { }
        return null;
    }

    // ── Speech-to-Text (SiliconFlow TeleSpeechASR) ────────────

    public async Task<string?> SpeechToTextAsync(string audioPath)
    {
        var cfg = _getConfig();
        if (string.IsNullOrWhiteSpace(cfg.ApiKey)) return null;

        try
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StreamContent(File.OpenRead(audioPath)), "file", Path.GetFileName(audioPath));
            form.Add(new StringContent(cfg.SttModel), "model");

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{cfg.ProviderUrl}/audio/transcriptions")
            {
                Content = form,
            };
            req.Headers.Add("Authorization", $"Bearer {cfg.ApiKey}");

            using var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("text").GetString()?.Trim();
        }
        catch { return null; }
    }

    // ── Chat with tool calling ────────────────────────────────

    public async Task<string?> ChatAsync(string userMessage, Action<string>? onToolResult = null)
    {
        var cfg = _getConfig();
        if (string.IsNullOrWhiteSpace(cfg.ApiKey)) return "请先在设置中填写 SiliconFlow API Key。\n\n获取 Key：https://cloud.siliconflow.cn/account/ak";

        var messages = new List<JsonObject>
        {
            new() { ["role"] = "system", ["content"] = cfg.SystemPrompt },
            new() { ["role"] = "user", ["content"] = userMessage },
        };

        var tools = GetToolDefinitions(cfg);

        for (int round = 0; round < 5; round++) // max tool-call rounds
        {
            var body = new JsonObject
            {
                ["model"] = cfg.ChatModel,
                ["messages"] = JsonSerializer.SerializeToNode(messages),
                ["temperature"] = 0.7,
                ["max_tokens"] = 800,
            };
            if (tools.Count > 0) body["tools"] = JsonSerializer.SerializeToNode(tools);

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{cfg.ProviderUrl}/chat/completions")
            {
                Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
            };
            req.Headers.Add("Authorization", $"Bearer {cfg.ApiKey}");

            using var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                var errBody = await resp.Content.ReadAsStringAsync();
                try { using var ed = JsonDocument.Parse(errBody); return "API 错误：" + ed.RootElement.GetProperty("error").GetProperty("message").GetString(); }
                catch { return $"API 请求失败 (HTTP {resp.StatusCode})"; }
            }

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var choice = doc.RootElement.GetProperty("choices")[0];
            var msg = choice.GetProperty("message");

            // check for tool calls
            if (msg.TryGetProperty("tool_calls", out var tcs) && tcs.GetArrayLength() > 0)
            {
                // add assistant message (with tool_calls) to history
                messages.Add(new JsonObject
                {
                    ["role"] = "assistant",
                    ["content"] = msg.TryGetProperty("content", out var c) ? c.GetString() : null,
                    ["tool_calls"] = JsonSerializer.SerializeToNode(tcs),
                });

                foreach (var tc in tcs.EnumerateArray())
                {
                    var fn = tc.GetProperty("function");
                    string fnName = fn.GetProperty("name").GetString()!;
                    string fnArgs = fn.GetProperty("arguments").GetString()!;
                    string toolId = tc.GetProperty("id").GetString()!;

                    string result = ExecuteTool(fnName, fnArgs, cfg);
                    onToolResult?.Invoke($"[{fnName}] {result}");

                    messages.Add(new JsonObject
                    {
                        ["role"] = "tool",
                        ["tool_call_id"] = toolId,
                        ["content"] = result,
                    });
                }
                continue; // next round — LLM gets tool results
            }

            // plain text response
            return msg.GetProperty("content").GetString()?.Trim();
        }

        return "(达到最大工具调用轮数)";
    }

    // ── tool definitions (OpenAI-compatible) ─────────────────

    private static List<JsonObject> GetToolDefinitions(AiConfigData cfg)
    {
        var tools = new List<JsonObject>
        {
            new()
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "create_file",
                    ["description"] = "创建一个新文件并写入内容",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["file_name"] = new JsonObject { ["type"] = "string", ["description"] = "文件名（相对或绝对路径）" },
                            ["content"] = new JsonObject { ["type"] = "string", ["description"] = "文件内容" },
                        },
                        ["required"] = JsonSerializer.SerializeToNode(new[] { "file_name", "content" }),
                    },
                },
            },
            new()
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "update_file",
                    ["description"] = "更新文件中的指定行范围",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["file_name"] = new JsonObject { ["type"] = "string", ["description"] = "要更新的文件名" },
                            ["line_start"] = new JsonObject { ["type"] = "integer", ["description"] = "起始行号（从1开始）" },
                            ["line_end"] = new JsonObject { ["type"] = "integer", ["description"] = "结束行号（包含）" },
                            ["new_content"] = new JsonObject { ["type"] = "string", ["description"] = "替换内容（多行用\n分隔）" },
                        },
                        ["required"] = JsonSerializer.SerializeToNode(new[] { "file_name", "line_start", "line_end", "new_content" }),
                    },
                },
            },
        };

        if (cfg.AllowFileDelete)
        {
            tools.Add(new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "delete_file",
                    ["description"] = "删除指定文件（危险操作，需用户授权）",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["file_name"] = new JsonObject { ["type"] = "string", ["description"] = "要删除的文件名" },
                        },
                        ["required"] = JsonSerializer.SerializeToNode(new[] { "file_name" }),
                    },
                },
            });
        }

        return tools;
    }

    // ── tool execution ────────────────────────────────────────

    private static string ExecuteTool(string name, string argsJson, AiConfigData cfg)
    {
        try
        {
            using var args = JsonDocument.Parse(argsJson);
            var root = args.RootElement;

            switch (name)
            {
                case "create_file":
                {
                    string fn = root.GetProperty("file_name").GetString()!;
                    string content = root.GetProperty("content").GetString()!;
                    fn = SanitizePath(fn);
                    Directory.CreateDirectory(Path.GetDirectoryName(fn) ?? ".");
                    File.WriteAllText(fn, content);
                    return $"文件已创建：{fn} ({content.Length} 字符)";
                }
                case "update_file":
                {
                    string fn = root.GetProperty("file_name").GetString()!;
                    int start = root.GetProperty("line_start").GetInt32();
                    int end = root.GetProperty("line_end").GetInt32();
                    string newContent = root.GetProperty("new_content").GetString()!;
                    fn = SanitizePath(fn);
                    if (!File.Exists(fn)) return $"文件不存在：{fn}";
                    var lines = File.ReadAllLines(fn).ToList();
                    if (start < 1) start = 1;
                    if (end > lines.Count) end = lines.Count;
                    if (start > end) return $"行范围无效：{start}-{end}";
                    lines.RemoveRange(start - 1, end - start + 1);
                    lines.InsertRange(start - 1, newContent.Split('\n'));
                    File.WriteAllLines(fn, lines);
                    return $"文件已更新：{fn} (行 {start}-{end}, 现在 {lines.Count} 行)";
                }
                case "delete_file":
                {
                    if (!cfg.AllowFileDelete) return "删除操作未启用（请在设置中开启）";
                    string fn = root.GetProperty("file_name").GetString()!;
                    fn = SanitizePath(fn);
                    if (!File.Exists(fn)) return $"文件不存在：{fn}";
                    File.Delete(fn);
                    return $"文件已删除：{fn}";
                }
                default:
                    return $"未知工具：{name}";
            }
        }
        catch (Exception ex)
        {
            return $"工具执行失败：{ex.Message}";
        }
    }

    /// <summary>prevent path traversal — only allow files under working dir</summary>
    private static string SanitizePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            // strip leading slashes/drives, prepend working dir
            path = path.TrimStart('/', '\\').TrimStart(Path.DirectorySeparatorChar);
            if (path.Length >= 2 && path[1] == ':') path = path[2..].TrimStart('/', '\\');
        }
        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
    }

    // ── helper ────────────────────────────────────────────────

    private static bool Run(string exe, string args)
    {
        try
        {
            var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe, args)
            {
                CreateNoWindow = true, UseShellExecute = false,
                RedirectStandardOutput = true, RedirectStandardError = true,
            });
            p?.WaitForExit(10000);
            return p?.ExitCode == 0;
        }
        catch { return false; }
    }
}
