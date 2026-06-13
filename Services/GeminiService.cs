using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiChatbotApp.Services;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GeminiService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> GenerateResponseAsync(string prompt)
    {
        var apiKey = _configuration["Gemini:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new Exception("Gemini API Key not found.");
        }

        var url =
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={apiKey}";

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = prompt
                        }
                    }
                }
            }
        };

        var json = JsonConvert.SerializeObject(body);

        var response = await _httpClient.PostAsync(
            url,
            new StringContent(
                json,
                Encoding.UTF8,
                "application/json"));

        var responseContent =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(responseContent);
        }

        var data = JObject.Parse(responseContent);

        return data["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()
               ?? "No response returned.";
    }
}