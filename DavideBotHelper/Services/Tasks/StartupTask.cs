using Coravel;
using DavideBotHelper.Services.ClassesAndUtilities;
using DavideBotHelper.Services.Extensions;

namespace DavideBotHelper.Services.Tasks;

public class StartupTask : BaseTask
{
    private readonly ILogger<StartupTask> _log;
    private readonly TelegramBotService _botService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly bool _isDevelopment;
    public StartupTask(ILogger<StartupTask> log, TelegramBotService botService, IServiceProvider serviceProvider, IConfiguration configuration, IHostEnvironment hostEnvironment) : base(log)
    {
        _log = log;
        _botService = botService;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _isDevelopment = hostEnvironment.IsDevelopment();
    }

    protected override async Task Run()
    {
        _log.Info("Starting Telegram Bot");
        await _botService.Connect();

        if (_isDevelopment)
        {
            var enabledTasks = _configuration.GetSection("EnabledTasks").Get<string[]>() ?? [];
            _serviceProvider.UseScheduler(scheduler =>
                {
                    if (enabledTasks.Contains(nameof(PowerAlertTask)))
                    {
                        scheduler.Schedule<PowerAlertTask>()
                            .EverySeconds(Constants.Every3Seconds)
                            .PreventOverlapping(nameof(PowerAlertTask));
                    }
                    if (enabledTasks.Contains(nameof(GithubReleasesCheckerTask)))
                    {
                        scheduler.Schedule<GithubReleasesCheckerTask>()
                            .DailyAtHour(14)
                            .Zoned(TimeZoneInfo.Local)
                            .PreventOverlapping(nameof(GithubReleasesCheckerTask));
                    }
                    if (enabledTasks.Contains(nameof(GithubReleaseDownloadTask)))
                    {
                        scheduler.Schedule<GithubReleaseDownloadTask>()
                            .EveryThirtySeconds()
                            .PreventOverlapping(nameof(GithubReleaseDownloadTask));
                    }
                    if (enabledTasks.Contains(nameof(SendReleaseAssetTask)))
                    {
                        scheduler.Schedule<SendReleaseAssetTask>()
                            .Cron(Constants.Every25MinutesCron)
                            .PreventOverlapping(nameof(SendReleaseAssetTask));
                    }
                })
                .LogScheduledTaskProgress();
        }
        else
        {
            _serviceProvider.UseScheduler(scheduler =>
                {
                    scheduler.Schedule<PowerAlertTask>()
                        .EverySeconds(Constants.Every3Seconds)
                        .PreventOverlapping(nameof(PowerAlertTask));
                    scheduler.Schedule<GithubReleasesCheckerTask>()
                        .DailyAtHour(14)
                        .Zoned(TimeZoneInfo.Local)
                        .RunOnceAtStart()
                        .PreventOverlapping(nameof(GithubReleasesCheckerTask));
                    scheduler.Schedule<GithubReleaseDownloadTask>()
                        .EveryThirtyMinutes()
                        .PreventOverlapping(nameof(GithubReleaseDownloadTask))
                        .RunOnceAtStart();
                    scheduler.Schedule<SendReleaseAssetTask>()
                        .Cron(Constants.Every25MinutesCron)
                        .PreventOverlapping(nameof(SendReleaseAssetTask));
                })
                .LogScheduledTaskProgress();
        }
    }
}