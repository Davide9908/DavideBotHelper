using Telegram.Bot.Types;
using Message = Telegram.Bot.Types.Message;

namespace DavideBotHelper.Services.Tasks;

public class PowerAlertTask : BaseTask
{
    
    // private const string UpsFlag = "G:\\test\\upsOnBattery";
    // private const string UpsLock = "G:\\test\\botHelper.lock";
    
    private const string UpsFlag = "/mnt/upsStatus/upsOnBattery";
    private const string UpsLock = "/mnt/upsStatus/botHelper.lock";
    
    
    private readonly ILogger<PowerAlertTask> _log;
    private readonly TelegramBotService _telegramBotService;
    private readonly ChatId _chatId = new ChatId(38076310);

    public PowerAlertTask(ILogger<PowerAlertTask> log, TelegramBotService telegramBotService) : base(log)
    {
        _log = log;
        _telegramBotService = telegramBotService;
    }


    protected override async Task Run()
    {
        try
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
                double minutesD = Math.Round((DateTime.Now - lastOutage).TotalMinutes, MidpointRounding.ToNegativeInfinity);
                int seconds = 0;
                int minutes = Convert.ToInt32(minutesD);
                if (minutes == 0)
                {
                   seconds = Convert.ToInt32((DateTime.Now - lastOutage).TotalSeconds);
                }
                if (PowerAlertFlag.LastAlert is null)
                {
                    if (seconds != 0)
                    {
                        _ = await _telegramBotService.SendMessage(_chatId,
                            $"Corrente ripristinata.\nLa mancanza è durata {seconds} secondi");
                    }
                    else
                    {
                        _ = await _telegramBotService.SendMessage(_chatId,
                            $"Corrente ripristinata.\nLa mancanza è durata {minutes} minuti");
                    }
                }
                else
                {
                    
                    if (seconds != 0)
                    {
                        _ = await _telegramBotService.SendMessage(_chatId, $"Corrente ripristinata.\nLa mancanza è durata {seconds} secondi", PowerAlertFlag.LastAlert);
                    }
                    else
                    {
                        _ = await _telegramBotService.SendMessage(_chatId, $"Corrente ripristinata.\nLa mancanza è durata {minutes} minuti", PowerAlertFlag.LastAlert);
                    }
                    //_ = await _telegramBotService.SendMessage(new ChatId(38076310), $"Corrente ripristinata.\nLa mancanza è durata {minutes} minuti", PowerAlertFlag.LastAlert);
                    //_ = await _telegramBotService.SendMessage(new ChatId(38076310), $"Corrente ripristinata.\nLa mancanza è durata {minutes} minuti");
                }

                File.Delete(UpsLock);
                PowerAlertFlag.LastAlert = null;
                return;
            }

            if (!File.Exists(UpsLock))
            {
                File.WriteAllText(UpsLock, DateTime.Now.Ticks.ToString());
                PowerAlertFlag.LastAlert =
                    await _telegramBotService.SendMessage(_chatId, "Rilevata mancanza corrente!");
                PowerAlertFlag.FirstRun = false;
            }
        }
        catch (Exception ex){
            _log.LogError(ex, "Error on task");
        }
    }
}

internal static class PowerAlertFlag
{
    public static bool FirstRun { get; set; } = true;
    public static Message? LastAlert { get; set; }
}