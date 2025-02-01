
using ASql;
using DavideBotHelper;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Database;
using DBType = ASql.ASqlManager.DBType;

var builder = Host.CreateApplicationBuilder(args);
ServiceExtension.AddServices(builder.Services);

var host = builder.Build();
HostingAbstractionsHostExtensions.Run(host);

file static class ServiceExtension
{
    public static void AddServices(this IServiceCollection services)
    {
        services.AddHostedService<Worker>();
        services.AddSerilog( c=> c.WriteTo.Database(DBType.Sqlite,"DavideBotHelper.db", "system_log",LogEventLevel.Verbose,false,1));
    }
}