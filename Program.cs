using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace InlineTelegramBot;

internal class Program
{
    private static TelegramBotClient _botClient;
    private static TelegramBotHandler _botHandler;

    private static Dictionary<string, string> _lineEmojis = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Сокольническая", "🔴" },
        { "Замоскворецкая", "🟢" },
        { "Арбатско-Покровская", "🔵" },
        { "Филёвская", "🩵" },
        { "Кольцевая", "🟤" },
        { "Калужско-Рижская", "🟠" },
        { "Таганско-Краснопресненская", "🟣" },
        { "Калининская", "🟡" },
        { "Солнцевская", "🟡" },
        { "Серпуховско-Тимирязевская", "🔘" },
        { "Люблинско-Дмитровская", "🍏" },
        { "Большая кольцевая", "💠" },
        { "Каховская", "💠" },
        { "Бутовская", "🔹" }, 
        { "Московское центральное кольцо", "⭕" },
        { "Некрасовская", "🩷" }, 
        { "Троицкая", "🟩" }, 
        { "МЦД-1", "🟧" }, 
        { "МЦД-2", "🟪" },
        { "МЦД-3", "🟨" },
        { "МЦД-4", "🟩" }
    };

    // coords (latitude, longitude) of mirea
    private static readonly (double lat, double lon) _mireaLocation = (55.6683009, 37.4750282);

    // what kind of updates bot should get
    private static readonly UpdateType[] _updateTypes =
    [
        UpdateType.InlineQuery,
        UpdateType.ChosenInlineResult,
        UpdateType.CallbackQuery
    ];

    private async static Task Main(string[] args)
    {
        var ct = new CancellationToken();
        var httpClient = new HttpClient();


        await InitTelegramBot(httpClient, ct);
        await LoadDB(httpClient);

        Console.ReadKey();
    }

    /// <summary>
    /// initialize telegram bot with the token from token.txt file
    /// </summary>
    /// <param name="httpClient">httpClient which will be used by bot to send requests</param>
    /// <param name="ct">cancelation token</param>
    /// <returns></returns>
    private static async Task InitTelegramBot(HttpClient httpClient, CancellationToken ct)
    {
        if (!File.Exists("token.txt"))
        {
            File.Create("token.txt");
        }
        var tgApiKey = File.ReadAllText("token.txt");
        if (string.IsNullOrEmpty(tgApiKey))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Введите токен:");
            Console.ResetColor();
            tgApiKey = Console.ReadLine() ?? "";
            File.AppendAllText("token.txt", tgApiKey);
        }
        _botClient = new TelegramBotClient(token: tgApiKey, httpClient: httpClient, cancellationToken: ct);
        Console.Clear();
        try
        {
            Console.WriteLine($"@{(await _botClient.GetMe(cancellationToken: ct)).Username} работает!\nНажмите любую кнопку для закрытия бота");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Произошла ошибка {ex.Message}. Проверьте валидность токена в token.txt и доступ к серверам телеграмма с вашего IP");
            Environment.Exit(ex.HResult);
        }

        _botHandler = new TelegramBotHandler(_mireaLocation);
        _botClient.StartReceiving(updateHandler: _botHandler, cancellationToken: ct, receiverOptions: new Telegram.Bot.Polling.ReceiverOptions() { AllowedUpdates = _updateTypes } );
    }

    /// <summary>
    /// loads database of metro stations from metro.json or https://api.hh.ru/metro/1
    /// </summary>
    /// <param name="httpClient">httpClient to send requst to https://api.hh.ru/metro/1</param>
    /// <returns></returns>
    private static async Task LoadDB(HttpClient httpClient)
    {
        using MetroStationsDBContext db = new MetroStationsDBContext();

        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        // check if db was created
        if (db.Stations.Any())
        {
            return;
        }

        if (!File.Exists("metro.json"))
        {
            var jsonContent = await httpClient.GetStringAsync("https://api.hh.ru/metro/1");
            await File.WriteAllTextAsync("metro.json", jsonContent);
        }

        var metroJson = JsonSerializer.Deserialize<MetroJson>(File.ReadAllText("metro.json"), new JsonSerializerOptions(JsonSerializerDefaults.Web))!; // downloaded from https://api.hh.ru/metro/1

        var stations = metroJson.Lines.SelectMany(l => l.Stations);

        (double lat, double lon) targetDest = (55.663146, 37.482852);// coords of югозападной

        foreach (var station in stations)
        {
            station.LineName = station.Line.Name;
            station.LineColorEmoji = _lineEmojis.GetValueOrDefault(station.LineName, "⚪");

            var dist = Math.Sqrt(Math.Pow((station.Latitude - targetDest.lat) * 111.3, 2) + Math.Pow((station.Longitude - targetDest.lon) * 62.5, 2));
            var transferMinutes = station.LineName == "Сокольническая" ? 0 : 5;// add ~5 minutes for the transfer

            station.MinutesToMirea = Math.Max(1, (int)Math.Round(dist * 1.95 + transferMinutes + 9));// an approximate formula for calculating travel time. + 9-minute walk to MIREA from metro
            db.Stations.Add(station);
        }
        db.SaveChanges();
    }
}