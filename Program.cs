using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace InlineTelegramBot;

internal class Program
{
    private static string _tgApiKey = "";

    private static TelegramBotClient _botClient;
    private static TelegramBotHandler _botHandler;

    //private static readonly string _apiDeepSeekKey;


    private static readonly (double lat, double lon) _mireaLocation = (55.6683009, 37.4750282);
    private static readonly List<UpdateType> _updateTypes = new List<UpdateType>()
    {
        
    };

    private async static Task Main(string[] args)
    {
        var ct = new CancellationToken();
        var httpClient = new HttpClient();


        InitTelegramBot(httpClient, ct);
        await LoadDB(httpClient);


        //var deepSeekAi = new DeepSeek("", httpClient, _apiDeepSeekKey);

        Console.ReadKey();
    }

    private static async void InitTelegramBot(HttpClient httpClient, CancellationToken ct)
    {
        if (!File.Exists("token.txt"))
        {
            File.Create("token.txt");
        }
        _tgApiKey = File.ReadAllText("token.txt");
        if (string.IsNullOrEmpty(_tgApiKey))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Введите токен:");
            Console.ResetColor();
            _tgApiKey = Console.ReadLine();
            File.AppendAllText("token.txt", _tgApiKey);
        }
        _botClient = new TelegramBotClient(token: _tgApiKey, httpClient: httpClient, cancellationToken: ct);
        Console.Clear();
        try
        {
            Console.WriteLine($"{(await _botClient.GetMe()).Username} работает!\nНажмите любую кнопку для закрытия бота");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Произошла ошибка {ex.Message}. Проверьте валидность токена в token.txt и доступ к серверам телеграмма с вашего IP");
            Environment.Exit(ex.HResult);
        }

        _botHandler = new TelegramBotHandler(_mireaLocation);
        _botClient.StartReceiving(_botHandler, cancellationToken: ct);
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