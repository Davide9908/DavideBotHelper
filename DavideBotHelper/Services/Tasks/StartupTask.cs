using Coravel;
using DavideBotHelper.Services.Extensions;

namespace DavideBotHelper.Services.Tasks;

public class StartupTask : BackgroundService
{
    private readonly ILogger<StartupTask> _log;
    private readonly TelegramBotService _botService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly bool _isDevelopment;
    public StartupTask(ILogger<StartupTask> log, TelegramBotService botService, IServiceProvider serviceProvider, IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        _log = log;
        _botService = botService;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _isDevelopment = hostEnvironment.IsDevelopment();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.Info("Starting Telegram Bot");
        await _botService.Connect();

        var enabledTasks = _configuration.GetSection("EnabledTasks").Get<string[]>() ?? [];
        if (!_isDevelopment || enabledTasks.Contains("PowerAlertTask"))
        {
            _serviceProvider.UseScheduler(scheduler =>
                    scheduler.Schedule<PowerAlertTask>()
                        .EverySecond()
                        .PreventOverlapping(nameof(PowerAlertTask)))
                .LogScheduledTaskProgress();
        }
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