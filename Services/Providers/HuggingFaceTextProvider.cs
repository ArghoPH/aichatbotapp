using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiChatbotApp.Services.Providers;

public class HuggingFaceTextProvider : IAiTextProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public HuggingFaceTextProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public string Name => "HuggingFace";

    public int Priority => 6;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["HuggingFace:ApiKey"]);

    public async Task<string> GenerateTextAsync(string prompt)
    {
        var apiKey = _configuration["HuggingFace:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new AiProviderException("Hugging Face API key missing.");
        }

        var model = _configuration["HuggingFace:Model"] ?? "mistralai/Mistral-7B-Instruct-v0.3";

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://router.huggingface.co/v1/chat/completions");

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
               ?? "No response returned from Hugging Face.";
    }

    private AiProviderException BuildException(string responseContent, System.Net.HttpStatusCode statusCode)
    {
        var isQuota =
            statusCode == System.Net.HttpStatusCode.TooManyRequests ||
            responseContent.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
            responseContent.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
            responseContent.Contains("credits", StringComparison.OrdinalIgnoreCase);

        return new AiProviderException(
            responseContent,
            isQuota,
            TimeSpan.FromMinutes(10));
    }
}
