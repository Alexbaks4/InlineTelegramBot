using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace InlineTelegramBot;

internal class TelegramBotHandler : IUpdateHandler
{
    // mirea coordinates
    private (double lat, double lon) _mireaLocation;

    // constructor
    public TelegramBotHandler((double lat, double lon) mireaLocation)
    {
        _mireaLocation = mireaLocation;
    }

    // main update router
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

    // handles inline keyboard clicks
    private async Task HandleCallbackQuery(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        // cancel button action
        if (update.CallbackQuery!.Data == "cancel")
        {
            await botClient.EditMessageText(update.CallbackQuery.InlineMessageId!, "Запрос был отменен пользователем");
        }
    }

    // generates inline search results
    private async Task HandleInlineQuery(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        // inline cancel button setup
        var cancelButton = InlineKeyboardButton.WithCallbackData("Отмена", "cancel");

        // inline result list
        var results = new List<InlineQueryResult>()
        {
            new InlineQueryResultArticle()
            {
                // unique result id
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

        // send inline response
        await botClient.AnswerInlineQuery(
            inlineQueryId: update.InlineQuery!.Id,
            results: results,
            cacheTime: 0,
            isPersonal: true,
            cancellationToken: ct);
    }

    // handles selected inline result
    private async Task HandleChoosenInlineResult(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        switch (update.ChosenInlineResult!.ResultId[0])
        {
            case '0':
                await SendTimeToMirea(botClient, update, ct);
                break;
        }
    }

    // calculates and sends travel time
    private async Task SendTimeToMirea(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        // check location access
        if (update.ChosenInlineResult!.Location is null)
        {
            await botClient.EditMessageText(
                inlineMessageId: update.ChosenInlineResult.InlineMessageId!,
                text: "Не удалось получить доступ к местоположению",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
            return;
        }

        // current location tuple
        (double lat, double lon) currenLoc = (update.ChosenInlineResult.Location.Latitude, update.ChosenInlineResult.Location.Longitude);

        string message;
        // check distance to mirea
        if (GetSquaredDist(_mireaLocation, currenLoc) > 0.36)
        {
            Station currentStation;
            double distToStation;

            // database connection
            using (MetroStationsDBContext db = new MetroStationsDBContext())
            {
                // empty database check
                if (!db.Stations.Any())
                {
                    return;
                }
                // find nearest station
                currentStation = db.Stations.ToList().MinBy(s => GetSquaredDist(s.Coords, currenLoc))!;
                distToStation = GetDist(currentStation.Coords, currenLoc);
            }

            // check distance to metro
            if (distToStation > 2.5)
            {
                message = $"Ближайшая станция <tg-spoiler>{currentStation.Name.ToLower()}{currentStation.LineColorEmoji}</tg-spoiler> в <tg-spoiler>{Math.Round(distToStation, 0)}</tg-spoiler> км, оттуда до шараги <tg-spoiler>{currentStation.MinutesToMirea}</tg-spoiler> мин";
            }
            else
            {
                message = $"Я на <tg-spoiler>{currentStation.Name.ToLower()}{currentStation.LineColorEmoji}</tg-spoiler>, до шараги <tg-spoiler>{currentStation.MinutesToMirea}</tg-spoiler> мин";
            }
        }
        else
        {
            message = $"Я в шараге";
        }

        // update message with final result
        await botClient.EditMessageText(
            inlineMessageId: update.ChosenInlineResult.InlineMessageId!,
            text: message,
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    // calculate actual distance
    private double GetDist((double lat, double lon) stationLoc, (double lat, double lon) currentLoc)
    {
        return Math.Sqrt(GetSquaredDist(stationLoc, currentLoc));
    }

    // calculate squared distance for optimization
    private double GetSquaredDist((double lat, double lon) stationLoc, (double lat, double lon) currentLoc)
    {
        return Math.Pow((currentLoc.lat - stationLoc.lat) * 111.3, 2) + Math.Pow((currentLoc.lon - stationLoc.lon) * 62.5, 2);
    }

    // error handler
    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}