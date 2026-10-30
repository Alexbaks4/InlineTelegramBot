using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace InlineTelegramBot;

internal class Program
{
    private static string _tgApiKey;
    //private static readonly string _apiDeepSeekKey;


    private static readonly (double lat, double lon) _mireaLocation = (55.6683009, 37.4750282);
    private static readonly List<UpdateType> _updateTypes = new List<UpdateType>()
    {
        
    };

    private async static Task Main(string[] args)
    {
        if (!File.Exists("token.txt"))
        {
            File.Create("token.txt");
        }
        _tgApiKey = File.ReadAllText("token.txt");
        if (string.IsNullOrEmpty(_tgApiKey))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Вставьте токен бота в token.txt");
            Console.ReadKey();
            return;
        }

        var cts = new CancellationToken();
        var httpClient = new HttpClient();

        await LoadDB(httpClient);


        //var deepSeekAi = new DeepSeek("", httpClient, _apiDeepSeekKey);
        var tgBot = new TelegramBotClient(_tgApiKey, httpClient, cts);
        var tgBotUpdateHandler = new TelegramBotHandler(_mireaLocation);

        tgBot.StartReceiving(tgBotUpdateHandler, cancellationToken: cts);

        Console.WriteLine($"{(await tgBot.GetMe()).Username} работает!\nНажмите любую кнопку для закрытия бота");
        Console.ReadKey();
    }

    private static async Task LoadDB(HttpClient httpClient)
    {
        using MetroStationsDBContext db = new MetroStationsDBContext();

        //db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        // проверяем загружена ли БД
        if (db.Stations.Any())
        {
            return;
        }

        if (!File.Exists("metro.json"))
        {
            var jsonContent = await httpClient.GetStringAsync("https://api.hh.ru/metro/1");
            await File.WriteAllTextAsync("metro.json", jsonContent);
        }

        var metroJson = JsonSerializer.Deserialize<MetroJson>(File.ReadAllText("metro.json"), new JsonSerializerOptions(JsonSerializerDefaults.Web))!; // скачано с https://api.hh.ru/metro/1

        var stations = new List<Station>();

        (double lat, double lon) targetDest = (55.663146, 37.482852);//координаты югозападной

        metroJson.Lines.ForEach(l => l.Stations.ForEach(s => stations.Add(s)));

        db.Database.EnsureCreated();
        foreach (var station in stations)
        {
            station.LineName = station.Line.Name;
            station.LineHexColor = station.Line.HexColor;
            var dist = Math.Sqrt(Math.Pow((station.Latitude - targetDest.lat) * 111.3, 2) + Math.Pow((station.Longitude - targetDest.lon) * 62.5, 2));
            var transferMinutes = station.LineName == "Сокольническая" ? 0 : 5;

            station.MinutesToMirea = Math.Max(1, (int)Math.Round(dist * 1.95 + transferMinutes + 9));
            db.Stations.Add(station);
        }
        db.SaveChanges();
    }
}