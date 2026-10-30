using MetroStations;
using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;

namespace InlineTelegramBot;

internal class TelegramBotHandler : IUpdateHandler
{

    public TelegramBotHandler()
    {
        
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        switch (update.Type)
        {
            case UpdateType.InlineQuery:
                (double lat, double lon) currenLoc = (update.InlineQuery.Location.Latitude, update.InlineQuery.Location.Longitude);
                Station station;
                double distToStation;
                using (MetroStationsDBContext db = new MetroStationsDBContext())
                {
                    station = db.Stations.ToList().MinBy(s => Math.Pow((currenLoc.lat - s.Latitude) * 111.3, 2) + Math.Pow((currenLoc.lon - s.Longitude) * 62.5, 2));
                    distToStation = Math.Sqrt(Math.Pow((currenLoc.lat - station.Latitude) * 111.3, 2) + Math.Pow((currenLoc.lon - station.Longitude) * 62.5, 2));
                }
                var message = distToStation > 2.5 ? $"Ближайшая станция <tg-spoiler>{station.Name.ToLower()}</tg-spoiler> в <tg-spoiler>{Math.Round(distToStation, 0)}</tg-spoiler> км, оттуда до шараги <tg-spoiler>{station.MinutesToYougoZapadnaya + 9}</tg-spoiler> мин" : $"я на <tg-spoiler>{station.Name.ToLower()}</tg-spoiler>, до шараги <tg-spoiler>{station.MinutesToYougoZapadnaya + 9}</tg-spoiler> мин";
                var results = new List<InlineQueryResult>();
                results.Add(new InlineQueryResultArticle("0", "сколько ехать до шараги", new InputTextMessageContent() { MessageText = message, ParseMode = ParseMode.Html }));
                await botClient.AnswerInlineQuery(update.InlineQuery.Id, results, cacheTime: 0);
                break;
            default:
                break;
        }
    }
    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
