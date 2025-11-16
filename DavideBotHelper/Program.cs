using Coravel;
using DavideBotHelper.Database.Context;
using DavideBotHelper.Services;
using DavideBotHelper.Services.ClassesAndUtilities;
using DavideBotHelper.Services.Tasks;
using Serilog;
using Serilog.Enrichers.WithCaller;
using Serilog.Events;
using Serilog.Sinks.Database;
using DBType = ASql.ASqlManager.DBType;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddServices(builder.Configuration, builder.Environment);

var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    using (var dbContext = scope.ServiceProvider.GetRequiredService<DavideBotDbContext>())
    {
        dbContext.Migrate();
    }
    scope.ServiceProvider.UseScheduler(scheduler =>
    {
        scheduler.Schedule<StartupTask>()
            .EverySecond()
            .Once()
            .PreventOverlapping(nameof(StartupTask));
    });
}


host.Run();

file static class ServiceExtension
{
    public static void AddServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
    {
        services.AddDbContext<DavideBotDbContext>()
            .AddScoped<StartupTask>()
            .AddScoped<GithubApiHttpClientService>()
            .AddScoped<GithubReleasesCheckerTask>()
            .AddScoped<GithubReleaseDownloadTask>()
            .AddScoped<SendReleaseAssetTask>()
            .AddSingleton<TelegramBotService>()
            .AddScoped<ExcelMovimentiService>()
            .AddTransient<PowerAlertTask>()
            .AddScheduler()
            .AddSerilog( serilogConfig=>
            {
                if (env.IsDevelopment())
                {
                    serilogConfig = serilogConfig.MinimumLevel.Debug().WriteTo.Console();
                }
                else
                {
                    serilogConfig = serilogConfig.MinimumLevel.Information().WriteTo.Console();
                }

                string? connectionString = configuration.GetConnectionString("DavideBotDB");
                if (connectionString is null)
                {
                    throw new ApplicationException("Could not find connection string");
                }
                    
                serilogConfig.Enrich.WithCaller()
                    .WriteTo.Database(DBType.PostgreSQL, connectionString, "system_log",
                        LogEventLevel.Debug, false, 1);
            })
            .AddHttpClient<GithubApiHttpClientService>(options =>
            {
                options.DefaultRequestHeaders.UserAgent.TryParseAdd(Constants.HeaderUserAgent);
                options.DefaultRequestHeaders.Add("Authorization", configuration["GitHubAccessToken"]);
            });
    }
}