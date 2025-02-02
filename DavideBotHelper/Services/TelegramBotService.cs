using System.Text;
using DavideBotHelper.Services.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WTelegram;

namespace DavideBotHelper.Services;

public class TelegramBotService : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramBotService> _log;
    private readonly BotConfig _botConfig = new();
    private readonly IServiceProvider _serviceProvider;
    private bool _disposedValue;
    private Bot _bot;

    private const string StartCommando = "/start";
    private const string AddSpesaCommando = "/aggiungiSpesa";
    private const string AddEntrataCommando = "/aggiungiEntrata";
    private const char Separator = ',';
    
    public TelegramBotService(IConfiguration configuration, ILogger<TelegramBotService> log, IHostApplicationLifetime appLifetime, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _log = log;
        _serviceProvider = serviceProvider;
        appLifetime.ApplicationStopping.Register(OnServiceStopping);
        _configuration.GetRequiredSection("BotConfig").Bind(_botConfig);
        BotSetup();
    }
    private void BotSetup()
    {
        StreamWriter WTelegramLogs = new StreamWriter("WTelegramBot.log", true, Encoding.UTF8) { AutoFlush = true };
        Helpers.Log = (lvl, str) => WTelegramLogs.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{"TDIWE!"[lvl]}] {str}");

        var connection = new Microsoft.Data.Sqlite.SqliteConnection(@"Data Source=WTelegramBotClient.db");

        _bot = new Bot(_botConfig.TelegramBotToken, _botConfig.TelegramApiId, _botConfig.TelegramAccessHash, connection);
    }
    

    public async Task Connect()
    {
        await _bot.DropPendingUpdates();
        _bot.OnMessage += OnMessage;
        _bot.Client.OnOther += Client_OnOther;
        _ = await _bot.GetMe();
    }
    
    private async Task OnMessage(Message msg, UpdateType type)
    {
        if (msg.From is null || msg.Text is null || msg.From.Id != 38076310 || msg.From.Username != "DavChi99")
        {
            return;
        }

        if (msg.Text == StartCommando)
        {
            return;
        }

        if (msg.Text.StartsWith(AddSpesaCommando))
        {
            await HandleAggiuntaSpesa(msg.Text, (msg.Chat, msg));
        }
        else if (msg.Text.StartsWith(AddEntrataCommando))
        {
            
        }
        else
        {
            await _bot.SendMessage(msg.Chat, $"Comando non riconosciuto", replyParameters: msg);
        }

    }

    private async Task HandleAggiuntaSpesa(string rawMessage, (Chat, Message) request)
    {
        
    }


    private async Task<MovimentoDetail?> BuildMovimento(string textRequest, (Chat Chat, Message Message) request)
    {
        string[] parts = textRequest.Split(Separator);
        if (parts.Length <= 1)
        {
            await _bot.SendMessage(request.Chat, "La richiesta non è in un formato corretto. Il corretto formato è [valore],[descrizione],[anno?],[mese?],[giorno?]\n" +
                                                 "I decimali del valore sono separati con il punto", replyParameters: request.Message);
            return null;
        }

        if (!decimal.TryParse(parts[0], out var valore))
        {
            await _bot.SendMessage(request.Chat, $"Non è stato possibile fare il parse del valore {parts[0]}", replyParameters: request.Message);
            return null;
        }
        
        
        MovimentoDetail movimentoDetail = new MovimentoDetail(valore);
    }
    
    private async Task Client_OnOther(TL.IObject arg)
    {
        TL.ReactorError? err;
        if ((err = arg as TL.ReactorError) is not null)
        {
            _log.Error(message: "Fatal reactor error", exception: err.Exception);
            _bot.Dispose();
            BotSetup();
            await Connect();
        }
    }
    
    private void OnServiceStopping()
    {
        Dispose();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _bot.Dispose();
            }
            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private  record MovimentoDetail
    {
        public decimal Valore { get; private set; }  
        public string Descrizione { get; private set; }  
        public int? Anno { get; private set; }  
        public int? Mese { get; private set; }  
        public int? Giorno { get; private set; }

        public MovimentoDetail(decimal valore, string descrizione, int? anno, int? mese, int? giorno)
        {
            Valore = valore;
            Descrizione = descrizione;
            Anno = anno;
            Mese = mese;
            Giorno = giorno;
        }
    }
}

