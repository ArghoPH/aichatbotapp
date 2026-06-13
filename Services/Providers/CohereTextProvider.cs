using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiChatbotApp.Services.Providers;

public class CohereTextProvider : IAiTextProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public CohereTextProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public string Name => "Cohere";

    public int Priority => 5;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["Cohere:ApiKey"]);

    public async Task<string> GenerateTextAsync(string prompt)
    {
        var apiKey = _configuration["Cohere:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new AiProviderException("Cohere API key missing.");
        }

        var model = _configuration["Cohere:Model"] ?? "command-r7b-12-2024";

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.cohere.com/v2/chat");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var body = new
        {
            model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            temperature = 0.7
        };

        request.Content = new StringContent(
            JsonConvert.SerializeObject(body),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw BuildException(responseContent, response.StatusCode);
        }

        var data = JObject.Parse(responseContent);

        var content = data["message"]?["content"];

        if (content is JArray contentArray)
        {
            return contentArray[0]?["text"]?.ToString()
                   ?? "No response returned from Cohere.";
        }

        return content?.ToString()
               ?? "No response returned from Cohere.";
    }

    private AiProviderException BuildException(string responseContent, System.Net.HttpStatusCode statusCode)
    {
        var isQuota =
            statusCode == System.Net.HttpStatusCode.TooManyRequests ||
            responseContent.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
            responseContent.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
            responseContent.Contains("trial", StringComparison.OrdinalIgnoreCase);

        return new AiProviderException(
            responseContent,
            isQuota,
            TimeSpan.FromMinutes(10));
    }
}