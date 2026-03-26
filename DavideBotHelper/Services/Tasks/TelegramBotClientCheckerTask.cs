
using System.Net.Sockets;
using DavideBotHelper.Services.Extensions;

namespace DavideBotHelper.Services.Tasks;

public class TelegramBotClientCheckerTask : BaseTask
{
    private readonly ILogger<TelegramBotClientCheckerTask> _log;
    private readonly TelegramBotService _botService;
    
    //TimeSpan's milliseconds are 10000 ticks, so i have to divide it in order to compare it correctly with Environment.TickCount64
    private const long TimeoutTicks = TimeSpan.TicksPerMinute / TimeSpan.TicksPerMillisecond * 5;
    
    public TelegramBotClientCheckerTask(ILogger<TelegramBotClientCheckerTask> log, TelegramBotService botService) : base(log)
    {
        _log = log;
        _botService = botService;
    }

    protected override async Task Run()
    {
        //_log.Info("running TelegramBotClientCheckerTask");
        var clientStart = _botService.ClientCreatedAt;
        if (DateTime.UtcNow - clientStart < TimeSpan.FromMinutes(3))
        {
            // _log.Info("Client started no more than 3 minutes");
            return;
        }
        long lastUpdateTicks = _botService.GetLastPong();
        // _log.Info("lastUpdateTicks {ticks}", lastUpdateTicks);
        long tickFromLastUpdate = Environment.TickCount64 - lastUpdateTicks;
        // _log.Info("tickFromLastUpdate {ticks}", tickFromLastUpdate);
        if (tickFromLastUpdate < TimeoutTicks)
        {
            return;
        }
        _log.Warning("Ping-Pong update timeout exceeded, now i'll proceed to recreate the client");
        int tryCount = 1;
        while (true)
        {
            try
            {
                bool reconnected = await _botService.DisposeClientAndReconnect(tryCount);
                if (!reconnected)
                {
                    _log.Warning("{taskname} could not get lock on semaphore (probably it was got by OnOther method)", nameof(TelegramBotClientCheckerTask));
                }
                break;
            }
            catch (SocketException se)
            {
                _log.Error(se, "Unable to connect, socket exception");
                tryCount++;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unable to connect telegram bot");
                tryCount++;
            }
            await Task.Delay(5000);
        }
    }
}