namespace SKIPPY.Models;

public class SettingsData
{
    // screen monitor
    public bool ScreenMonitorEnabled { get; set; } = false;
    public int ScreenMonitorIntervalSec { get; set; } = 3;

    // ai question (SiliconFlow)
    public string AiApiKey { get; set; } = "";
    public string AiChatModel { get; set; } = "deepseek-ai/DeepSeek-V3";
    public string AiSystemPrompt { get; set; } = SKIPPY.Services.AiConfigData.SKIPPY_DEFAULT_PROMPT;
    public bool AiAllowFileDelete { get; set; } = false;
}
