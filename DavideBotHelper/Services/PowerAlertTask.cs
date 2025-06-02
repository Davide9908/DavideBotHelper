using System.Reflection;
using Coravel.Invocable;
using Telegram.Bot.Types;
using WTelegram.Types;
using Message = Telegram.Bot.Types.Message;

namespace DavideBotHelper.Services;

public class PowerAlertTask : IInvocable
{
    private const string UpsFlag = "/var/upsStatus/upsOnBattery";
    private const string UpsLock = "/var/upsStatus/botHelper.lock";
    private readonly ILogger<PowerAlertTask> _logger;
    private readonly TelegramBotService _telegramBotService;

    public PowerAlertTask(ILogger<PowerAlertTask> logger, TelegramBotService telegramBotService)
    {
        _logger = logger;
        _telegramBotService = telegramBotService;
    }
    public async Task Invoke()
    {
        if (!File.Exists(UpsFlag))
        {
            if (!File.Exists(UpsLock))
            {
                return;
            }

            if (PowerAlertFlag.FirstRun)
            {
                File.Delete(UpsLock);
                PowerAlertFlag.FirstRun = false;
                return;
            }
            
            string[] fileLines = File.ReadAllLines(UpsLock);
            DateTime lastOutage = new DateTime(long.Parse(fileLines[0]));
            int minutes = Convert.ToInt32((DateTime.Now - lastOutage).TotalMinutes);
            if (PowerAlertFlag.LastAlert is null)
            {
                _ = await _telegramBotService.SendMessage(new ChatId(38076310), $"Corrente ripristinata.\nLa mancanza è durata {minutes} minuti");
            }
            else
            {
                _ = await _telegramBotService.SendMessage(new ChatId(38076310), $"Corrente ripristinata.\nLa mancanza è durata {minutes} minuti", PowerAlertFlag.LastAlert);
            }
            File.Delete(UpsLock);
            return;
        }

        File.WriteAllText(UpsFlag, DateTime.Now.Ticks.ToString());
        PowerAlertFlag.LastAlert = await  _telegramBotService.SendMessage(new ChatId(38076310), "Rilevata mancanza corrente!");
        
    }
}

internal static class PowerAlertFlag
{
    public static bool FirstRun { get; set; } = true;
    public static Message? LastAlert { get; set; }
}