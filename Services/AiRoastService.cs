using System.Net.Http;

namespace SKIPPY.Services;

/// <summary>
/// AI roast — uses the same SiliconFlow config as AiService.
/// sends SCP article content to the LLM and returns roast commentary.
/// </summary>
public class AiRoastService(Func<AiConfigData> getConfig)
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };

    private static readonly string RoastPrompt =
        "你是一个毒舌的 SCP 基金会文章审稿人。用户提交了一段 SCP 文档草稿，" +
        "请用犀利、幽默的中文吐槽这篇文档。指出格式问题、逻辑漏洞、设定矛盾。" +
        "回复控制在 200 字以内。吐槽完之后如果看到机密分级相关的内容请特别暴躁。";

    public async Task<string?> RoastAsync(string articleContent)
    {
        var cfg = getConfig();
        var chat = cfg.Chat;
        if (string.IsNullOrWhiteSpace(chat.ApiKey))
            return "⚠️ 请先在 AI 设置中填写对话 API Key。";

        if (string.IsNullOrWhiteSpace(articleContent)) return null;

        try
        {
            var body = System.Text.Json.JsonSerializer.Serialize(new
            {
                model = chat.Model,
                messages = new[]
                {
                    new { role = "system", content = RoastPrompt },
                    new { role = "user", content = articleContent },
                },
                temperature = 0.9,
                max_tokens = 500,
            });

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{chat.BaseUrl}/chat/completions")
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
            };
            req.Headers.Add("Authorization", $"Bearer {chat.ApiKey}");

            using var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return $"API 请求失败 (HTTP {resp.StatusCode})";

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var comment = doc.RootElement.GetProperty("choices")[0]
                .GetProperty("message").GetProperty("content").GetString() ?? "";
            return comment.Trim();
        }
        catch (Exception ex) { return $"请求失败：{ex.Message}"; }
    }
}
