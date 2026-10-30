using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;

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
            default:
                break;
        }
    }

    private async Task HandleInlineQuery(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        if (update.InlineQuery!.Location is null)
        {
            return;
        }

        (double lat, double lon) currenLoc = (update.InlineQuery.Location.Latitude, update.InlineQuery.Location.Longitude);

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
                message = $"Ближайшая станция <tg-spoiler>{currentStation.Name.ToLower()}</tg-spoiler> в <tg-spoiler>{Math.Round(distToStation, 0)}</tg-spoiler> км, оттуда до шараги <tg-spoiler>{currentStation.MinutesToMirea}</tg-spoiler> мин\nПоследнее обновление: {DateTime.Now:t}";
            }
            else
            {
                message = $"Я на <tg-spoiler>{currentStation.Name.ToLower()}</tg-spoiler>, до шараги <tg-spoiler>{currentStation.MinutesToMirea}</tg-spoiler> мин\nПоследнее обновление: {DateTime.Now:t}";
            }
        }
        else
        {
            message = $"Я в шараге";
        }
        var results = new List<InlineQueryResult>()
        {
            new InlineQueryResultArticle(id: Guid.NewGuid().ToString(), title: "сколько ехать до шараги", inputMessageContent: new InputTextMessageContent() { MessageText = message, ParseMode = ParseMode.Html })
        };

        await botClient.AnswerInlineQuery(
            inlineQueryId: update.InlineQuery.Id,
            results: results,
            cacheTime: 0,
            isPersonal: true,
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
