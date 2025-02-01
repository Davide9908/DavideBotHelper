using DavideBotHelper.Services.Extensions;
using WTelegram;

namespace DavideBotHelper.Services;

public class TelegramBotService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramBotService> _logger;
    private Bot? _bot;

    public TelegramBotService(IConfiguration configuration, ILogger<TelegramBotService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
    }
}