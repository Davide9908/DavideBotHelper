
using Coravel;
using DavideBotHelper.Database.Context;
using DavideBotHelper.Services;
using DavideBotHelper.Services.Tasks;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Database;
using DBType = ASql.ASqlManager.DBType;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddServices(builder.Configuration);

var host = builder.Build();
using (var scope = host.Services.CreateScope())
using (var dbContext = scope.ServiceProvider.GetRequiredService<DavideBotDbContext>())
{
    dbContext.Migrate();
}

host.Run();

file static class ServiceExtension
{
    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<StartupTask>()
            .AddDbContext<DavideBotDbContext>()
            .AddScoped<GithubApiHttpClientService>()
            .AddScoped<GithubReleasesCheckerTask>()
            .AddSingleton<TelegramBotService>()
            .AddScoped<ExcelMovimentiService>()
            .AddTransient<PowerAlertTask>()
            .AddScheduler()
            .AddSerilog( serilogConfig=>
            {
                serilogConfig = serilogConfig.MinimumLevel.Debug().WriteTo.Console();
                string? connectionString = configuration.GetConnectionString("DavideBotDB");
                if (connectionString is null)
                {
                    throw new ApplicationException("Could not find connection string");
                }
                    
                serilogConfig.WriteTo.Database(DBType.PostgreSQL, connectionString, "system_log",
                        LogEventLevel.Debug, false, 1);
            })
            .AddHttpClient<GithubApiHttpClientService>(options =>
            {
                options.DefaultRequestHeaders.UserAgent.TryParseAdd("dotNET HTTP Client/1.0 personal bot agent");
            });
    }
}