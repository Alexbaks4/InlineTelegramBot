using System.Net.Http.Headers;
using System.Text.Json;

namespace InlineTelegramBot;

internal class DeepSeek
{
    private string _instructions;
    private HttpClient _httpClient;
    private string _apiKey;
    private const string _deepseekUrl = "https://api.deepseek.com/chat/completions";

    public DeepSeek(string instrucion, HttpClient httpClient, string apiKey)
    {
        _instructions = instrucion;
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    private string Convert2Json(string prompt, bool stream = false)
    {
        var jsonObj = new
        {
            model = "deepseek-v4-flash",
            messages = new[]
            {
                new { role = "system", content = _instructions },
                new { role = "user", content = prompt }
            },
            thinking = new { type = "disabled" },
            stream = stream
        };
        var json = JsonSerializer.Serialize(jsonObj);
        return json;
    }

    private async Task<string> Json2String(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var jsonRes = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(jsonRes);
        var aiResText = jsonDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").ToString();
        return aiResText;
    }

    public async Task<string> AskDeepSeek(string prompt)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var stringContent = new StringContent(prompt);
        var response = await _httpClient.PostAsync(_deepseekUrl, stringContent);
        var textAnswer = await Json2String(response);
        Console.WriteLine(textAnswer);
        return textAnswer;
    }
}
