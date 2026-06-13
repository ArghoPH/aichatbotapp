using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiChatbotApp.Services.Providers;

public class GeminiTextProvider : IAiTextProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GeminiTextProvider(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public string Name => "Gemini";

    public int Priority => 1;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["Gemini:ApiKey"]);

    public async Task<string> GenerateTextAsync(string prompt)
    {
        var apiKey = _configuration["Gemini:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new AiProviderException("Gemini API key missing.");
        }

        var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash-lite";

        var url =
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

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
            new StringContent(json, Encoding.UTF8, "application/json"));

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw BuildException(responseContent);
        }

        var data = JObject.Parse(responseContent);

        return data["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()
               ?? "No response returned from Gemini.";
    }

    private AiProviderException BuildException(string responseContent)
    {
        var isQuota =
            responseContent.Contains("RESOURCE_EXHAUSTED") ||
            responseContent.Contains("quota") ||
            responseContent.Contains("429");

        var retryAfter = ExtractRetryDelay(responseContent);

        return new AiProviderException(
            responseContent,
            isQuota,
            retryAfter);
    }

    private TimeSpan? ExtractRetryDelay(string responseContent)
    {
        try
        {
            var data = JObject.Parse(responseContent);

            var retryDelay = data["error"]?["details"]?
                .FirstOrDefault(x => x["@type"]?.ToString().Contains("RetryInfo") == true)?
                ["retryDelay"]?.ToString();

            if (string.IsNullOrWhiteSpace(retryDelay))
            {
                return null;
            }

            retryDelay = retryDelay.Replace("s", "");

            if (int.TryParse(retryDelay, out var seconds))
            {
                return TimeSpan.FromSeconds(seconds);
            }
        }
        catch
        {
            return null;
        }

        return null;
    }
}