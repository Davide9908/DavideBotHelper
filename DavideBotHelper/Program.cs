
using DavideBotHelper.Services;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Database;
using DBType = ASql.ASqlManager.DBType;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddServices();

var host = builder.Build();
host.Run();

file static class ServiceExtension
{
    public static void AddServices(this IServiceCollection services)
    {
        services.AddHostedService<StartupTask>();
        services.AddSingleton<TelegramBotService>();
        services.AddScoped<ExcelMovimentiService>();
        services.AddSerilog( c=> c.WriteTo.Console()
                .WriteTo.Database(DBType.Sqlite,"Data Source=DavideBotHelper.db", "system_log",LogEventLevel.Verbose,false,1));
    }
}