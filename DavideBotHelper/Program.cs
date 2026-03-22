using System.Security.Claims;
using System.Text.Encodings.Web;
using Coravel;
using DavideBotHelper.Database.Context;
using DavideBotHelper.Services;
using DavideBotHelper.Services.ClassesAndUtilities;
using DavideBotHelper.Services.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Enrichers.WithCaller;
using Serilog.Events;
using Serilog.Sinks.Database;
using Telegram.Bot.Types;
using DBType = ASql.ASqlManager.DBType;

var builder = WebApplication.CreateBuilder(args);;

builder.Services.AddServices(builder.Configuration, builder.Environment);
builder.Services.SetupApiServices();

var host = builder.Build();
host.SetupApiConfiguration();
host.AddApi();
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
            .AddScoped<WolProxmoxDevicesUpdaterTask>()
            .AddScoped<TelegramBotClientCheckerTask>()
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

file static class WebExtension
{
    private const string ApiKey = "ed3049503d3a0e85e452fb02f079dca994366e6f801ee448c9c568a1ed9b8a4e196d1c7eaf6303ca28eb02d7f0e8ae49e7c24cff291729fd2ecbc8212a0dcde5";
    public static IServiceCollection SetupApiServices(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                var securityScheme = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    Name = "x-api-key",
                    In = ParameterLocation.Header,
                    Description = "Inserisci qui la tua API Key per testare le chiamate."
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes.Add(ApiKeyAuthHandler.SchemeName, securityScheme);

                var securityRequirement = new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference(ApiKeyAuthHandler.SchemeName, document),
                        []
                    }
                };

                document.Security ??= new List<OpenApiSecurityRequirement>();
                document.Security.Add(securityRequirement);

                return Task.CompletedTask;
            });
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                var isAnonymous = context.Description.ActionDescriptor.EndpointMetadata
                    .Any(metadata => metadata is Microsoft.AspNetCore.Authorization.IAllowAnonymous);

                if (isAnonymous)
                {
                    operation.Security = new List<OpenApiSecurityRequirement>();
                }

                return Task.CompletedTask;
            });
        });
        services.AddAuthentication(ApiKeyAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>(ApiKeyAuthHandler.SchemeName, null);
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
        return services.AddEndpointsApiExplorer();
    }

    public static void SetupApiConfiguration(this WebApplication app)
    {
        app.MapOpenApi().AllowAnonymous();
        app.MapScalarApiReference().AllowAnonymous();;
        app.UseAuthentication();
        app.UseAuthorization();
    }

    public static void AddApi(this WebApplication app)
    {
        app.MapGet("/hello", () => "Hello World!");
        app.MapGroup("tgBot").WithTags("Telegram Bot Api")
            .MapPost("/sendMessageToUser", async (TelegramBotService botService, string message) =>
            {
                await botService.SendMessageToMainUser(message);
            });
    }
}
file class ApiKeyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKeyScheme";
    private const string ApiKeyHeaderName = "x-api-key";
    private const string ApiKey = "ed3049503d3a0e85e452fb02f079dca994366e6f801ee448c9c568a1ed9b8a4e196d1c7eaf6303ca28eb02d7f0e8ae49e7c24cff291729fd2ecbc8212a0dcde5";
    public ApiKeyAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, 
        ILoggerFactory logger, 
        UrlEncoder encoder) 
        : base(options, logger, encoder) 
    { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Unauthorized"));
        }
        
        if (!extractedApiKey.Equals(ApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("User unauthorized"));
        }

        var claims = new[] { new Claim(ClaimTypes.Name, "ApiKeyUser") };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}