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
    private const string ApiBaseUrl = "";     // 填写 API 地址
    private const string ApiKey = "";          // 填写 API Key

    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    /// <summary>
    /// send text to api.
    /// </summary>
    public static async Task<string?> RoastAsync(string articleContent)
    {
        if (string.IsNullOrWhiteSpace(ApiBaseUrl) || string.IsNullOrWhiteSpace(ApiKey))
        {
            return "⚠️ API 未配置。\n请在 AiRoastService.cs 中填写 ApiBaseUrl 和 ApiKey。\n\nAPI 需自行实现，仅 releases 内程序可正常调用。";
        }

        if (string.IsNullOrWhiteSpace(articleContent))
            return null;

        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["api_content"] = articleContent,
                ["api_key"] = ApiKey,
            });

            using var response = await _httpClient.PostAsync(ApiBaseUrl, content);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("comment", out var commentProp))
            {
                return commentProp.GetString();
            }

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
}
