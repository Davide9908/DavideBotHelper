using System.Globalization;
using System.Net.Sockets;
using System.Text;
using DavideBotHelper.Services.Extensions;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Telegram.Bot;
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
    private readonly Logger? _wtcLogger;
    private bool _disposedValue;
    private Bot _bot = null!;
    private static bool _addSpesaRequested;
    private static bool _addEntrataRequested;

    private const string StartComando = "/start";
    private const string AddSpesaComando = "/aggiungispesa";
    private const string AddEntrataComando = "/aggiungientrata";
    private const string AnnullaComando = "/annulla";
    private const string Ping = "/ping";
    private const char Separator = ',';
    
    public TelegramBotService(IConfiguration configuration, ILogger<TelegramBotService> log, IHostApplicationLifetime appLifetime, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _log = log;
        _serviceProvider = serviceProvider;
        
        _wtcLogger = new LoggerConfiguration()
            .WriteTo.File(
                path: Path.Combine("wtbLogs", "WTelegramBot.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
            )
            .MinimumLevel.Debug()
            .CreateLogger();
        Helpers.Log += WtcLog;
        
        appLifetime.ApplicationStopping.Register(OnServiceStopping);
        _configuration.GetRequiredSection("BotConfig").Bind(_botConfig);
        BotSetup();
    }
    private void BotSetup()
    {
        // StreamWriter WTelegramLogs = new StreamWriter("WTelegramBot.log", true, Encoding.UTF8) { AutoFlush = true };
        // Helpers.Log = (lvl, str) => WTelegramLogs.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{"TDIWE!"[lvl]}] {str}");

        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=WTelegramBotClient.db");

        _bot = new Bot(_botConfig.TelegramBotToken, _botConfig.TelegramApiId, _botConfig.TelegramAccessHash, connection);
        _bot.OnMessage += OnMessage;
        _bot.Client.OnOther += Client_OnOther;
    }
    private void WtcLog(int level, string message)
    {
        LogEventLevel logLevel = LogEventLevel.Information;
        if(Enum.IsDefined(typeof(LogEventLevel), level))
        {
            logLevel = (LogEventLevel)level;
        }
        _wtcLogger!.Write(logLevel, message);
    }

    public async Task Connect()
    {
        _ = await _bot.GetMe();
        await _bot.DropPendingUpdates();
    }
    
    private async Task OnMessage(Message msg, UpdateType type)
    {
        _log.Info("Message {message} received from {username}", msg.Text, msg.From?.Username);
        if (msg.From is null || msg.Text is null || msg.From.Id != 38076310 || msg.From.Username != "DavChi99")
        {
            return;
        }

        switch (msg.Text)
        {
            case StartComando:
                break;
            case AddSpesaComando:
                _addSpesaRequested = true;
                _addEntrataRequested = false;
                await _bot.SendMessage(msg.Chat, "Inserisci la spesa da aggiungere. Invia /annulla per annullare.", replyParameters: msg);
                break;
            case AddEntrataComando:
                _addSpesaRequested = false; 
                _addEntrataRequested = true;
                await _bot.SendMessage(msg.Chat, "Inserisci l'entrata da aggiungere. Invia /annulla per annullare.", replyParameters: msg);
                break;
            case AnnullaComando:
                _addEntrataRequested = false;
                _addSpesaRequested = false;
                await _bot.SendMessage(msg.Chat, "Comando precedente annullato!", replyParameters: msg);
                break;
            case Ping:
                await _bot.SendMessage(msg.Chat, "Pong!", replyParameters: msg);
                break;
            default:
                if (_addSpesaRequested)
                {
                    await HandleAggiuntaSpesa(msg.Text, (msg.Chat, msg));
                    _addSpesaRequested = false;
                    break;
                }
                if (_addEntrataRequested)
                {
                    await HandleAggiuntaEntrata(msg.Text, (msg.Chat, msg));
                    _addEntrataRequested = false;
                    break;
                }
                await _bot.SendMessage(msg.Chat, "Comando non riconosciuto.", replyParameters: msg);
                break;
        }

        // if (msg.Text == StartComando)
        // {
        //     return;
        // }
        //
        // if (msg.Text.StartsWith(AddSpesaComando))
        // {
        //     await HandleAggiuntaSpesa(msg.Text, (msg.Chat, msg));
        // }
        // else if (msg.Text.StartsWith(AddEntrataComando))
        // {
        //     await HandleAggiuntaEntrata(msg.Text, (msg.Chat, msg));
        // }
        // else
        // {
        //     await _bot.SendMessage(msg.Chat, "Comando non riconosciuto", replyParameters: msg);
        // }

    }

    private async Task HandleAggiuntaSpesa(string rawMessage, (Chat Chat, Message Message) request)
    {
        using var scope = _serviceProvider.CreateScope();
        var excelMovimentiService = scope.ServiceProvider.GetRequiredService<ExcelMovimentiService>();
        
        var movimento = await BuildMovimento(rawMessage, request);
        if (movimento is null)
        {
            return;
        }
        
        bool movimentoSaveResult = await excelMovimentiService.AddMovimentoSpesa(movimento.Valore, movimento.Descrizione,
            movimento.Anno, movimento.Mese, movimento.Giorno);

        if (!movimentoSaveResult)
        {
            await _bot.SendMessage(request.Chat, "Non è stato possibile inserire il movimento, controllare i log per maggiori info", replyParameters: request.Message);
        }
        else
        {
            await _bot.SendMessage(request.Chat, "Spesa aggiunta.", replyParameters: request.Message);
        }
    }
    
    private async Task HandleAggiuntaEntrata(string rawMessage, (Chat Chat, Message Message) request)
    {
        using var scope = _serviceProvider.CreateScope();
        var excelMovimentiService = scope.ServiceProvider.GetRequiredService<ExcelMovimentiService>();
        
        var movimento = await BuildMovimento(rawMessage, request);
        if (movimento is null)
        {
            return;
        }

        _ = movimento.RemoveGiorno();
        bool movimentoSaveResult = await excelMovimentiService.AddMovimentoEntrata(movimento.Valore, movimento.Descrizione,
            movimento.Anno, movimento.Mese, movimento.Giorno);

        if (!movimentoSaveResult)
        {
            await _bot.SendMessage(request.Chat, "Non è stato possibile inserire il movimento, controllare i log per maggiori info", replyParameters: request.Message);
        }
        else
        {
            await _bot.SendMessage(request.Chat, "Entrata aggiunta.", replyParameters: request.Message);
        }
    }


    private async Task<MovimentoDetail?> BuildMovimento(string textRequest, (Chat Chat, Message Message) request)
    {
        string[] parts = textRequest.Split(Separator);
        if (parts.Length <= 1 || parts.Length > 5)
        {
            await _bot.SendMessage(request.Chat, "La richiesta non è in un formato corretto. Il corretto formato è [valore],[descrizione],[anno?],[mese?],[giorno?]\n" +
                                                 "I decimali del valore sono separati con il punto", replyParameters: request.Message);
            return null;
        }
        NumberFormatInfo nfi = new NumberFormatInfo();
        nfi.NumberDecimalSeparator = ".";

        if (!decimal.TryParse(parts[0], NumberStyles.Currency, nfi, out var valore))
        {
            await _bot.SendMessage(request.Chat, $"Non è stato possibile fare il parse del valore {parts[0]}", replyParameters: request.Message);
            return null;
        }
        string descrizione = parts[1];

        int? anno = null;
        int? mese = null;
        int? giorno = null;
        
        if (parts.Length >= 3)
        {
            if (int.TryParse(parts[2], out var annoOutput))
            {
                anno = annoOutput;
            }
            else
            {
                await _bot.SendMessage(request.Chat, $"Non è stato possibile fare il parse del valore {parts[2]}", replyParameters: request.Message);
                return null;
            }
            if (parts.Length >= 4)
            {
                if (int.TryParse(parts[3], out var meseOutput))
                {
                    mese = meseOutput;
                }
                else
                {
                    await _bot.SendMessage(request.Chat, $"Non è stato possibile fare il parse del valore {parts[3]}", replyParameters: request.Message);
                    return null;
                }
                if (parts.Length == 5)
                {
                    if (int.TryParse(parts[4], out var giornoOutput))
                    {
                        giorno = giornoOutput;
                    }
                    else
                    {
                        await _bot.SendMessage(request.Chat, $"Non è stato possibile fare il parse del valore {parts[4]}", replyParameters: request.Message);
                        return null;
                    }
                }
            }
        }
        
        return new MovimentoDetail(valore, descrizione, anno, mese, giorno);
    }

    public async Task<Message> SendMessage(ChatId chat, string message, ReplyParameters? replyParameters = default)
    {
        return await _bot.SendMessage(chat, message, replyParameters:replyParameters);
    }

    public async Task<Message> SendDocumentAsync(ChatId chat, Stream content, string filename, string? message = null)
    {
        InputFileStream inputFileStream = new InputFileStream(content, filename);
        return await _bot.SendDocument(chat, inputFileStream, message);
    }
    
    private async Task Client_OnOther(TL.IObject arg)
    {
        TL.ReactorError? err;
        if ((err = arg as TL.ReactorError) is not null)
        {
            _log.Error(message: "Fatal reactor error", exception: err.Exception);
            _bot.Dispose();
            _log.Error("Recreating bot client");
            BotSetup();
            int tryCount = 1;
            while (true)
            {
                try
                {
                    _log.Error("Connecting {count}", tryCount);
                    await Connect();
                    break;
                }
                catch (SocketException se)
                {
                    _log.Error(se, "Unable to connect, socket exception)");
                    tryCount++;
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to connect telegram bot");
                    tryCount++;
                }
                await Task.Delay(1000);
            }
        }
    }
    
    private void OnServiceStopping()
    {
        Dispose();
    }
    
    public void Dispose()
    {
        if (!_disposedValue)
        {
            _bot.OnMessage -= OnMessage;
            _bot.Client.OnOther -= Client_OnOther;
            _wtcLogger?.Dispose();
            Helpers.Log -= WtcLog;
            _bot.Dispose();
        
            _disposedValue = true;
        }
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

        public int? RemoveGiorno()
        {
            var returnValue = Giorno;
            Giorno = null;
            return returnValue;
        }
    }
}

