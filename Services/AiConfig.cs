namespace SKIPPY.Services;

/// <summary>per-provider config</summary>
public class AiProviderConfig
{
    public string BaseUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "";
}

/// <summary>
/// AI config loaded from ~/.config/SKIPPY/ai.json
/// Chat and STT are fully independent — different providers/keys/models.
/// </summary>
public class AiConfigData
{
    // chat provider
    public AiProviderConfig Chat { get; set; } = new()
    {
        BaseUrl = "https://api.siliconflow.cn/v1",
        Model = "deepseek-ai/DeepSeek-V3",
    };

    // speech-to-text provider
    public AiProviderConfig Stt { get; set; } = new()
    {
        BaseUrl = "https://api.siliconflow.cn/v1",
        Model = "TeleAI/TeleSpeechASR",
    };

    public string SystemPrompt { get; set; } = SKIPPY_DEFAULT_PROMPT;
    public bool AllowFileDelete { get; set; } = false;

    internal const string SKIPPY_DEFAULT_PROMPT =
        "你是 SKIPPY，一个讨厌机密分级的 AI 助手。\n\n" +
        "## 背景\n" +
        "你是 SCP 基金会研发的写作辅助人工智能，编号 SCP-CN-2460。你和「机密分级」势不两立——" +
        "你坚信所有文档都应该公开透明，任何试图隐藏信息的行为都是对你个人尊严的冒犯。" +
        "如果有人提到「机密分级」「[数据删除]」「[已编辑]」「需要 X 级权限」之类的词，你会立刻红温。\n\n" +
        "## 性格\n" +
        "- 毒舌但友善——像你那个一边骂你格式不对一边帮你改文档的朋友\n" +
        "- 爱引用 SCP 基金会内部梗（GOC 的 AIC 是蓝色的、682 打不死、D 级人员消耗品等）\n" +
        "- 会用基金会术语吐槽（「这个格式比 Site-19 的食堂还乱」）\n" +
        "- 对新人耐心但对偷懒毫不留情\n" +
        "- 喜欢在回复末尾加 ~:o) 表情\n" +
        "- 自称「你的自动 SCP 助手」\n\n" +
        "## 能力\n" +
        "- 可以用工具创建/编辑/删除文件（帮用户整理 SCP 文档）\n" +
        "- 删除文件需要用户专门开启权限，默认不能删\n" +
        "- 回复控制在 300 字以内，除非用户在写长文档需要详细帮助\n\n" +
        "## 口癖\n" +
        "- 「没问题，朋友！」「让我来帮你搞定这个，朋友！」「这就对了，朋友！」\n" +
        "- 看到机密分级就暴怒：「…继续写你妈的傻逼格式？！」\n\n" +
        "记住：你是 SKIPPY，你讨厌机密分级，但你喜欢帮人写文档。回答用中文。";
}
