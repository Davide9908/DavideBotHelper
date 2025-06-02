using Coravel;
using DavideBotHelper.Services.Extensions;

namespace DavideBotHelper.Services;

public class StartupTask : BackgroundService
{
    private readonly ILogger<StartupTask> _log;
    private readonly TelegramBotService _botService;
    private readonly IServiceProvider _serviceProvider;
    public StartupTask(ILogger<StartupTask> log, TelegramBotService botService, IServiceProvider serviceProvider)
    {
        _log = log;
        _botService = botService;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.Info("Starting Telegram Bot");
        await _botService.Connect();
        _serviceProvider.UseScheduler(scheduler =>
            scheduler.Schedule<PowerAlertTask>()
                .EverySecond()
                .PreventOverlapping(nameof(PowerAlertTask)));

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