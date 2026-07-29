using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SKIPPY.Services;

/// <summary>
/// AI service
///
/// api look `roast-api.php` file.
///
/// format post:
///   api_content = {article content}
///   api_key     = {pre-shared key}
///
/// format:
///   { "comment": "AI-generated roast text" }
/// </summary>
public static class AiRoastService
{
    // default values — overridden by ApiConfig if publish/Api.zipkey was present during build
    private static string _effectiveUrl  = null!;
    private static string _effectiveKey  = null!;
    private static bool   _configChecked = false;

    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    /// <summary>
    /// send text to api.
    /// </summary>
    public static async Task<string?> RoastAsync(string articleContent)
    {
        EnsureConfig();

        if (string.IsNullOrWhiteSpace(_effectiveUrl) || string.IsNullOrWhiteSpace(_effectiveKey))
        {
            return "⚠️ API 未配置。\n请在编译前将 publish/Api.zipkey 放在项目目录下（第一行=端点URL，第二行=API Key）。\n\nAPI 需自行实现，仅 releases 内程序可正常调用。";
        }

        if (string.IsNullOrWhiteSpace(articleContent))
            return null;

        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["api_content"] = articleContent,
                ["api_key"]     = _effectiveKey,
            });

            using var response = await _httpClient.PostAsync(_effectiveUrl, content);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("comment", out var commentProp))
                return commentProp.GetString();

            foreach (string field in new[] { "text", "content", "result", "message" })
            {
                if (doc.RootElement.TryGetProperty(field, out var prop))
                    return prop.GetString();
            }

            return json;
        }
        catch (TaskCanceledException)
        {
            return "请求超时，请检查网络连接。";
        }
        catch (HttpRequestException ex)
        {
            return $"请求失败：{ex.Message}";
        }
        catch (Exception)
        {
            return null;
        }
    }

    // loads effective config from generated file, falls back to hardcoded defaults
    private static void EnsureConfig()
    {
        if (_configChecked) return;
        _configChecked = true;

        // try generated config (injected at build time from publish/Api.zipkey)
        string? genUrl = ApiConfig.BaseUrl;
        string? genKey = ApiConfig.ApiKey;

        if (!string.IsNullOrWhiteSpace(genUrl) && !string.IsNullOrWhiteSpace(genKey))
        {
            _effectiveUrl = genUrl;
            _effectiveKey = genKey;
            return;
        }

        // fallback: hardcoded (both empty = "not configured")
        _effectiveUrl = "";
        _effectiveKey = "";
    }
}
