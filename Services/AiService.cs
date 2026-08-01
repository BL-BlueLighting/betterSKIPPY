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
                return HasExe("arecord") || HasExe("parec") || HasExe("pw-record");
            if (OperatingSystem.IsMacOS())
                return HasExe("sox") || HasExe("rec");
            if (OperatingSystem.IsWindows())
                return true; // assume yes on Windows — PowerShell audio capture always works
        }
        catch { }
        return false;
    }

    /// <summary>start push-to-talk recording, returns the process (null if no mic).
    /// call StopRecording() to stop and get the WAV path.</summary>
    public static System.Diagnostics.Process? StartRecording(string outputPath)
    {
        try
        {
            File.Delete(outputPath);
            var psi = GetRecordStartInfo(outputPath);
            if (psi == null) return null;
            var p = System.Diagnostics.Process.Start(psi);
            return p;
        }
        catch { return null; }
    }

    /// <summary>kill the recording process, wait for file, return path or null</summary>
    public static string? StopRecording(System.Diagnostics.Process? proc, string outputPath)
    {
        if (proc == null) return null;
        try
        {
            if (!proc.HasExited)
            {
                proc.Kill(entireProcessTree: true);
                proc.WaitForExit(3000);
            }
        }
        catch { }
        // wait up to 2s for the file to flush
        for (int i = 0; i < 20; i++)
        {
            if (File.Exists(outputPath) && new FileInfo(outputPath).Length > 100) return outputPath;
            Thread.Sleep(100);
        }
        return File.Exists(outputPath) ? outputPath : null;
    }

    private static System.Diagnostics.ProcessStartInfo? GetRecordStartInfo(string outputPath)
    {
        if (OperatingSystem.IsLinux())
        {
            // All STT APIs want 16kHz mono. Record in that format directly.
            if (HasExe("arecord"))
                return new System.Diagnostics.ProcessStartInfo("arecord", $"-f S16_LE -r 16000 -c 1 -t wav {outputPath}")
                { CreateNoWindow = true, UseShellExecute = false };
            if (HasExe("parec"))
                return new System.Diagnostics.ProcessStartInfo("parec", $"--file-format=wav --rate=16000 --channels=1 {outputPath}")
                { CreateNoWindow = true, UseShellExecute = false };
            if (HasExe("pw-record"))
                return new System.Diagnostics.ProcessStartInfo("pw-record", $"--rate 16000 --channels 1 --format s16 --container wav {outputPath}")
                { CreateNoWindow = true, UseShellExecute = false };
        }
        if (OperatingSystem.IsMacOS())
        {
            if (HasExe("sox"))
                return new System.Diagnostics.ProcessStartInfo("sox", $"-d -r 16000 -c 1 -b 16 {outputPath}")
                { CreateNoWindow = true, UseShellExecute = false };
        }
        return null;
    }

    private static bool HasExe(string exe)
    {
        // try running it directly — `command -v` doesn't fork a subprocess
        try
        {
            var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe, "--version")
            { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true });
            p?.WaitForExit(2000);
            return p?.ExitCode == 0;
        }
        catch { return false; }
    }

    // ── Speech-to-Text (SiliconFlow TeleSpeechASR) ────────────

    /// <summary>returns transcribed text, or error string starting with "ERR:" on failure</summary>
    public async Task<string> SpeechToTextAsync(string audioPath)
    {
        var cfg = _getConfig();
        var stt = cfg.Stt;
        if (string.IsNullOrWhiteSpace(stt.ApiKey)) return "ERR:未配置 STT API Key";

        if (!File.Exists(audioPath)) return "ERR:录音文件不存在";
        var fi = new FileInfo(audioPath);
        if (fi.Length < 100) return $"ERR:录音文件过小 ({fi.Length} bytes)";

        try
        {
            // build multipart form manually — all bytes, no StreamWriter position bugs
            var fileBytes = File.ReadAllBytes(audioPath);
            var boundary = "----SkpStt" + Guid.NewGuid().ToString("N")[..8];
            var enc = new UTF8Encoding(false);

            static byte[] B(string s, UTF8Encoding e) => e.GetBytes(s);

            var parts = new List<byte[]>();
            parts.Add(B($"--{boundary}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"{Path.GetFileName(audioPath)}\"\r\nContent-Type: audio/wav\r\n\r\n", enc));
            parts.Add(fileBytes);
            parts.Add(B($"\r\n--{boundary}\r\nContent-Disposition: form-data; name=\"model\"\r\n\r\n{stt.Model}\r\n--{boundary}--\r\n", enc));

            var bodyBytes = new byte[parts.Sum(p => p.Length)];
            int offset = 0;
            foreach (var p in parts) { Buffer.BlockCopy(p, 0, bodyBytes, offset, p.Length); offset += p.Length; }

            using var content = new ByteArrayContent(bodyBytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("multipart/form-data");
            content.Headers.ContentType.Parameters.Add(
                new System.Net.Http.Headers.NameValueHeaderValue("boundary", boundary));

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{stt.BaseUrl}/audio/transcriptions")
            {
                Content = content,
            };
            req.Headers.Add("Authorization", $"Bearer {stt.ApiKey}");

            using var resp = await _http.SendAsync(req);
            var respBody = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                var shortBody = respBody.Length > 200 ? respBody[..200] + "..." : respBody;
                return $"ERR:API {resp.StatusCode}: {shortBody}";
            }

            using var doc = JsonDocument.Parse(respBody);
            var text = doc.RootElement.GetProperty("text").GetString()?.Trim();
            return string.IsNullOrWhiteSpace(text) ? "ERR:API 返回空文本" : text;
        }
        catch (Exception ex) { return $"ERR:{ex.Message}"; }
    }

    // ── Chat with tool calling ────────────────────────────────

    public async Task<string?> ChatAsync(string userMessage, Action<string>? onToolResult = null)
    {
        var cfg = _getConfig();
        var chat = cfg.Chat;
        if (string.IsNullOrWhiteSpace(chat.ApiKey)) return "请先在设置中填写对话 API Key。";

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
                ["model"] = chat.Model,
                ["messages"] = JsonSerializer.SerializeToNode(messages),
                ["temperature"] = 0.7,
                ["max_tokens"] = 800,
            };
            if (tools.Count > 0) body["tools"] = JsonSerializer.SerializeToNode(tools);

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{chat.BaseUrl}/chat/completions")
            {
                Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
            };
            req.Headers.Add("Authorization", $"Bearer {chat.ApiKey}");

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

}
