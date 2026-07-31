namespace SKIPPY.Services;

/// <summary>
/// AI 吐槽 API 配置。
/// 请求格式 (POST, form-urlencoded): api_content={文章内容} & api_key={密钥}
/// 响应格式 (JSON): { "comment": "AI 生成的吐槽文本" }
/// 服务端实现见仓库内 roast-api.php
/// </summary>
internal static class ApiConfig
{
    public static readonly string BaseUrl = "https://games.airoj.cn/webapi.php";
    public static readonly string ApiKey  = "i-am-the-real-api-auth-key-i-am-real-skippy-built-by-bluelighting-please-let-me-pass-and-if-you-watched-this-key-with-decompiling-way-then-please-use-my-api-endpoint-lightly-because-i-am-very-poor-thanks";
}
