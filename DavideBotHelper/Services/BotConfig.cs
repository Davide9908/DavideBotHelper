namespace DavideBotHelper.Services;

public class BotConfig
{
    public int TelegramApiId { get; set; }
    public string TelegramAccessHash { get; set; }
    public string TelegramBotToken { get; set; }

    public BotConfig(int telegramApiId, string telegramAccessHash, string telegramBotToken)
    {
        TelegramApiId = telegramApiId;
        TelegramAccessHash = telegramAccessHash;
        TelegramBotToken = telegramBotToken;
    }
    public BotConfig(){}
}