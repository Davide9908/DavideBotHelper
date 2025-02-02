using DavideBotHelper.Services.Extensions;

namespace DavideBotHelper.Services;

public class StartupTask : BackgroundService
{
    private readonly ILogger<StartupTask> _log;
    private readonly TelegramBotService _botService;
    public StartupTask(ILogger<StartupTask> log, TelegramBotService botService)
    {
        _log = log;
        _botService = botService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.Info("Starting Telegram Bot");
        await _botService.Connect();
        // while (!stoppingToken.IsCancellationRequested)
        // {
        //     if (_logger.IsEnabled(LogLevel.Information))
        //     {
        //         LoggerExtensions.LogInformation(_logger, "Worker running at: {time}", DateTimeOffset.Now);
        //     }
        //
        //     await Task.Delay(1000, stoppingToken);
        // }
    }
}