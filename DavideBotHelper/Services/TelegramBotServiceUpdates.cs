using System.Net.Sockets;
using DavideBotHelper.Services.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DavideBotHelper.Services;

public partial class TelegramBotService
{
    private async Task OnUpdate(Update update)
    {
        UpdateLastPong();
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
        UpdateLastPong();
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
                //_log.Info("ping-pong");
                UpdateLastPong();
                break;
        }
    }
}