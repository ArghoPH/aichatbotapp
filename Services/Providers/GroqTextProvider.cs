using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiChatbotApp.Services.Providers;

public class GroqTextProvider : IAiTextProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GroqTextProvider(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public string Name => "Groq";

    public int Priority => 2;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["Groq:ApiKey"]);

    public async Task<string> GenerateTextAsync(string prompt)
    {
        var apiKey = _configuration["Groq:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new AiProviderException("Groq API key missing.");
        }

        var model = _configuration["Groq:Model"] ?? "llama-3.3-70b-versatile";

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.groq.com/openai/v1/chat/completions");

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
            var isQuota =
                response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                responseContent.Contains("rate limit") ||
                responseContent.Contains("quota") ||
                responseContent.Contains("429");

            throw new AiProviderException(
                responseContent,
                isQuota,
                TimeSpan.FromMinutes(2));
        }

        var data = JObject.Parse(responseContent);

        return data["choices"]?[0]?["message"]?["content"]?.ToString()
               ?? "No response returned from Groq.";
    }
}