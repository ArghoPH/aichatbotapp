using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiChatbotApp.Services.Providers;

public class CerebrasTextProvider : IAiTextProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public CerebrasTextProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public string Name => "Cerebras";

    public int Priority => 7;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["Cerebras:ApiKey"]);

    public async Task<string> GenerateTextAsync(string prompt)
    {
        var apiKey = _configuration["Cerebras:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new AiProviderException("Cerebras API key missing.");
        }

        var model = _configuration["Cerebras:Model"] ?? "llama-3.3-70b";

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.cerebras.ai/v1/chat/completions");

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

        return data["choices"]?[0]?["message"]?["content"]?.ToString()
               ?? "No response returned from Cerebras.";
    }

    private AiProviderException BuildException(string responseContent, System.Net.HttpStatusCode statusCode)
    {
        var isQuota =
            statusCode == System.Net.HttpStatusCode.TooManyRequests ||
            responseContent.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
            responseContent.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
            responseContent.Contains("billing", StringComparison.OrdinalIgnoreCase);

        return new AiProviderException(
            responseContent,
            isQuota,
            TimeSpan.FromMinutes(10));
    }
}
