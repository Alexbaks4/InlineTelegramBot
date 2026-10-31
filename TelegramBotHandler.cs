using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace InlineTelegramBot;

internal class TelegramBotHandler : IUpdateHandler
{

    private (double lat, double lon) _mireaLocation = (55.6683009, 37.4750282);

    public TelegramBotHandler((double lat, double lon) mireaLocation)
    {
        _mireaLocation = mireaLocation;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        switch (update.Type)
        {
            case UpdateType.InlineQuery:
                await HandleInlineQuery(botClient, update, ct);
                break;
            case UpdateType.ChosenInlineResult:
                await HandleChoosenInlineResult(botClient, update, ct);
                break;
            case UpdateType.CallbackQuery:
                await HandleCallbackQuery(botClient, update, ct);
                break;
            default:
                break;
        }
    }

    private async Task HandleCallbackQuery(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        if (update.CallbackQuery!.Data == "cancel")
        {
            await botClient.EditMessageText(update.CallbackQuery.InlineMessageId!, "Запрос был отменен пользователем");
        }
    }

    private async Task HandleInlineQuery(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        var cancelButton = InlineKeyboardButton.WithCallbackData("Отмена", "cancel");

        var results = new List<InlineQueryResult>()
        {
            new InlineQueryResultArticle()
            {
                Id = "0" + Guid.NewGuid().ToString(),
                Title = "Cколько ехать до шараги",
                Description = "Узнай сколько ехать до мирэа",
                
                InputMessageContent =  new InputTextMessageContent()
                {
                    MessageText = $"<i>Определяю сколько {update.InlineQuery!.From.Username ?? "пользователю"} ехать до шараги...</i>",
                    ParseMode = ParseMode.Html
                },
                ReplyMarkup = new InlineKeyboardMarkup(new[]
                {
                    cancelButton
                }),
                ThumbnailUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQVoQ3tMqqHoD1DluKy85bOVZgYdJ7RJpSjNDLmoHVsZBtsrdNyFMMmLMHS&s=10"
            }
        };

        await botClient.AnswerInlineQuery(
            inlineQueryId: update.InlineQuery!.Id,
            results: results,
            cacheTime: 0,
            isPersonal: true,
            cancellationToken: ct);
    }

    private async Task HandleChoosenInlineResult(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        switch (update.ChosenInlineResult!.ResultId[0])
        {
            case '0':
                await SendTimeToMirea(botClient, update, ct);
                break;
        }
    }

    private async Task SendTimeToMirea(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        if (update.ChosenInlineResult!.Location is null)
        {
            await botClient.EditMessageText(
                inlineMessageId: update.ChosenInlineResult.InlineMessageId!,
                text: "Не удалось получить доступ к местоположению",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
            return;
        }

        (double lat, double lon) currenLoc = (update.ChosenInlineResult.Location.Latitude, update.ChosenInlineResult.Location.Longitude);

        string message;
        if (GetSquaredDist(_mireaLocation, currenLoc) > 0.04)
        {
            Station currentStation;
            double distToStation;


            using (MetroStationsDBContext db = new MetroStationsDBContext())
            {
                if (!db.Stations.Any())
                {
                    return;
                }
                currentStation = db.Stations.ToList().MinBy(s => GetSquaredDist(s.Coords, currenLoc))!;
                distToStation = GetDist(currentStation.Coords, currenLoc);
            }


            if (distToStation > 2.5)
            {
                message = $"Ближайшая станция <tg-spoiler>{currentStation.Name.ToLower()}</tg-spoiler> в <tg-spoiler>{Math.Round(distToStation, 0)}</tg-spoiler> км, оттуда до шараги <tg-spoiler>{currentStation.MinutesToMirea}</tg-spoiler> мин";
            }
            else
            {
                message = $"Я на <tg-spoiler>{currentStation.Name.ToLower()}</tg-spoiler>, до шараги <tg-spoiler>{currentStation.MinutesToMirea}</tg-spoiler> мин";
            }
        }
        else
        {
            message = $"Я в шараге";
        }

        await botClient.EditMessageText(
            inlineMessageId: update.ChosenInlineResult.InlineMessageId!,
            text: message,
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }


    private double GetDist((double lat, double lon) stationLoc, (double lat, double lon) currentLoc)
    {
        return Math.Sqrt(GetSquaredDist(stationLoc, currentLoc));
    }

    private double GetSquaredDist((double lat, double lon) stationLoc, (double lat, double lon) currentLoc)
    {
        return Math.Pow((currentLoc.lat - stationLoc.lat) * 111.3, 2) + Math.Pow((currentLoc.lon - stationLoc.lon) * 62.5, 2);
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
