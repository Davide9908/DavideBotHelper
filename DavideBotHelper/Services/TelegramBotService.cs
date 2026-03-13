using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using DavideBotHelper.Database;
using DavideBotHelper.Database.Context;
using DavideBotHelper.Services.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using WTelegram;

namespace DavideBotHelper.Services;

public class TelegramBotService : IDisposable
{
    private readonly ILogger<TelegramBotService> _log;
    private readonly BotConfig _botConfig = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly Logger? _wtcLogger;
    private bool _disposedValue;
    private Bot _bot = null!;
    private static bool _addSpesaRequested;
    private static bool _addEntrataRequested;
    private long _lastPong;
    private static readonly string[] ValidPrefixes = [WolRequestCallbackPrefix];
    private readonly SemaphoreSlim _reconnectionSemaphore = new SemaphoreSlim(1, 1);

    private const string StartComando = "/start";
    private const string AddSpesaComando = "/aggiungispesa";
    private const string AddEntrataComando = "/aggiungientrata";
    private const string AnnullaComando = "/annulla";
    private const string Ping = "/ping";
    private const string WakeOnLan = "/wol";
    private const char MovimentoSeparator = ',';
    private const string WolRequestCallbackPrefix = "WolRequest";
    private const char CallbackSeparator = '~';
    private const string DoneButtonData = "done";
    private const int WolPort = 9;


    public TelegramBotService(IConfiguration configuration, ILogger<TelegramBotService> log, IHostApplicationLifetime appLifetime, IServiceProvider serviceProvider)
    {
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
        configuration.GetRequiredSection("BotConfig").Bind(_botConfig);
        BotSetup();
    }
    private void BotSetup()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=WTelegramBotClient.db");
        _bot = new Bot(_botConfig.TelegramBotToken, _botConfig.TelegramApiId, _botConfig.TelegramAccessHash, connection);
        _bot.OnMessage += OnMessage;
        _bot.Client.OnOther += Client_OnOther;
        _bot.OnUpdate += OnUpdate;
        UpdateLastPong();
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
        UpdateLastPong();
        _ = await _bot.GetMe();
        await _bot.DropPendingUpdates();
    }

    private async Task OnUpdate(Update update)
    {
        if (update.CallbackQuery is not null && update.CallbackQuery.Message is not null)
        {
            if (update.CallbackQuery.Data is null)
            {
                _log.Error("Received callback without data");
                await _bot.AnswerCallbackQuery(update.CallbackQuery.Id);
                return;
            }
            var data = update.CallbackQuery.Data;
            //User pressed "Done" dummy button
            if (data == DoneButtonData)
            {
                await _bot.AnswerCallbackQuery(update.CallbackQuery.Id);
                return;
            }
            
            var callbackData = data.Split(CallbackSeparator);
            
            if (callbackData.Length != 2 && !ValidPrefixes.Contains(callbackData[0]))
            {
                _log.Error("Received invalid callback data: {data}", data);
                await _bot.AnswerCallbackQuery(update.CallbackQuery.Id);
                return;
            }

            switch (callbackData[0])
            {
                case WolRequestCallbackPrefix:
                    await HandleWolCallback(update.CallbackQuery, callbackData[1]);
                    break;
                default:
                    break;
            }
        }
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
            case WakeOnLan:
                await HandleWolRequest(msg);
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
    }

    private async Task HandleWolRequest(Message msg)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<DavideBotDbContext>();
        
        var devices = dbContext.WolDevices.Where(w => w.IsEnabled).ToList();
        var inlineKeyboard = new InlineKeyboardMarkup();

        if (!devices.Any())
        {
            await _bot.SendMessage(msg.Chat, "Nessun dispositivo configurato per il wake-on-lan", replyParameters: msg);
            return;
        }

        var first = devices[0];
        foreach (WolDevice device in devices)
        {
            if (device != first)
            {
                inlineKeyboard.AddNewRow();
            }
            inlineKeyboard.AddButton(device.Description ?? "Unknown Device: " + device.DeviceMacAddress, string.Join(CallbackSeparator, WolRequestCallbackPrefix, device.WolDeviceId.ToString()));
        }
        
        await _bot.SendMessage(msg.Chat, "Seleziona il dispositivo da accendere:", replyParameters: msg, replyMarkup: inlineKeyboard);
    }
    
    private async Task HandleWolCallback(CallbackQuery callbackQuery, string wolDevice)
    {
        if (!int.TryParse(wolDevice, out int wolDeviceId))
        {
            _log.Error("Received invalid callback data (id): {data}", callbackQuery.Data);
            await _bot.AnswerCallbackQuery(callbackQuery.Id);
            return;
        }

        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<DavideBotDbContext>();

        WolDevice? device = await dbContext.WolDevices
            .FirstOrDefaultAsync(wd => wd.WolDeviceId == wolDeviceId && wd.IsEnabled);
        if (device is null)
        {
            _log.Error("Received invalid callback data (id) - device not found: {data}", callbackQuery.Data);
            await _bot.AnswerCallbackQuery(callbackQuery.Id);
            return;
        }
        
        if (!PhysicalAddress.TryParse(device.DeviceMacAddress, out var address))
        {
            _log.Error("Configured mac address is not valid: {macAddress}", device.DeviceMacAddress);
            await _bot.AnswerCallbackQuery(callbackQuery.Id, "L'indirizzo mac configurato non è valido", true);
            return;
        }
        
        byte[] wolPacket = new byte[102];

        for (int i = 0; i < 6; i++)
        {
            wolPacket[i] = 0xFF;
        }
    
        for (int i = 6; i < 102;)
        {
            foreach (var addressByte in address.GetAddressBytes())
            {
                wolPacket[i] = addressByte;
                i++;
            }
        }
    
        using UdpClient client = new();
        try
        {
            _log.Info("Connecting to Broadcast on Port {port}...", WolPort);
            client.Connect(IPAddress.Broadcast, WolPort);
            _log.Info("Sending WOL packet...");
            await client.SendAsync(wolPacket, wolPacket.Length);
            _log.Info("WOL packet sent...");
        }
        catch (Exception e)
        {
            _log.Error(e, "Error sending WOL packet to {device}", device);
            await _bot.AnswerCallbackQuery(callbackQuery.Id);
            return;
        }
        
        await _bot.AnswerCallbackQuery(callbackQuery.Id);
        var inlineKeyboard = new InlineKeyboardMarkup();
        var button = new InlineKeyboardButton("Fatto ✅", DoneButtonData);
        button.Style = KeyboardButtonStyle.Success;
        inlineKeyboard.AddButton(button);
            
        await _bot.EditMessageReplyMarkup(callbackQuery.Message!.Chat, callbackQuery.Message.Id, inlineKeyboard);
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
        string[] parts = textRequest.Split(MovimentoSeparator);
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

        switch (arg)
        {
            case TL.ReactorError err:
                _log.Error(message: "Fatal reactor error", exception: err.Exception);
            
                int tryCount = 1;
                while (true)
                {
                    try
                    {
                        _ = await DisposeClientAndReconnect(tryCount);
                        break;
                    }
                    catch (SocketException se)
                    {
                        _log.Error(se, "Unable to connect, socket exception");
                        tryCount++;
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Unable to connect telegram bot");
                        tryCount++;
                    }
                    await Task.Delay(5000);
                }
                break;
            case TL.Pong _:
                UpdateLastPong();
                break;
        }
    }

    public async Task<bool> DisposeClientAndReconnect(int tryCount)
    {
        bool entered = _reconnectionSemaphore.Wait(0);
        if (!entered)
        {
            return false;
        }
        try
        {
            _addEntrataRequested = false;
            _addSpesaRequested = false;
            _bot.Client.OnOther -= Client_OnOther;
            _bot.OnMessage -= OnMessage;
            _bot.OnUpdate -= OnUpdate;
            _bot.Dispose();
            _log.Error("Recreating bot client");
            BotSetup();
            _log.Info("Connecting {count}", tryCount);
            await Connect();
            _log.Info("Connected");
        }
        finally
        {
            _reconnectionSemaphore.Release();
        }

        return true;
    }

    private void UpdateLastPong()
    {
        Interlocked.Exchange(ref _lastPong, Environment.TickCount64);
    }

    public long GetLastPong()
    {
        return Interlocked.Read(ref _lastPong);
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
            _bot.OnUpdate -= OnUpdate;
            _wtcLogger?.Dispose();
            Helpers.Log -= WtcLog;
            _bot.Dispose();
        
            _disposedValue = true;
        }
    }

    private record MovimentoDetail
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

