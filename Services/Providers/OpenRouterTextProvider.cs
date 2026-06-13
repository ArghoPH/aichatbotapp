using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiChatbotApp.Services.Providers;

public class OpenRouterTextProvider : IAiTextProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OpenRouterTextProvider(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public string Name => "OpenRouter";

    public int Priority => 3;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["OpenRouter:ApiKey"]);

    public async Task<string> GenerateTextAsync(string prompt)
    {
        var apiKey = _configuration["OpenRouter:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new AiProviderException("OpenRouter API key missing.");
        }

        var model = _configuration["OpenRouter:Model"] ?? "deepseek/deepseek-r1:free";

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://openrouter.ai/api/v1/chat/completions");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        request.Headers.Add("HTTP-Referer", "http://localhost:5173");
        request.Headers.Add("X-Title", "AI Smart Chatbot");

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
            }
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
                TimeSpan.FromMinutes(5));
        }

        var data = JObject.Parse(responseContent);

        return data["choices"]?[0]?["message"]?["content"]?.ToString()
               ?? "No response returned from OpenRouter.";
    }
}