using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace AiChatbotApp.Services.Providers;

public class GitHubModelsVisionProvider : IAiVisionProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GitHubModelsVisionProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient();
        _configuration = configuration;
    }

    public string Name => "GitHub Models Vision";

    public int Priority => 2;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["GitHubModels:ApiKey"]);

    public async Task<string> AnalyzeImageAsync(string prompt, string imagePath)
    {
        var apiKey = _configuration["GitHubModels:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new AiProviderException(
                "GitHub Models API key is not configured.",
                isQuotaError: false);
        }

        if (!File.Exists(imagePath))
        {
            throw new AiProviderException(
                "Image file was not found.",
                isQuotaError: false);
        }

        var model =
            _configuration["GitHubModels:VisionModel"] ??
            "meta/llama-3.2-11b-vision-instruct";

        var mimeType = GetMimeType(imagePath);

        var imageBytes = await File.ReadAllBytesAsync(imagePath);
        var base64Image = Convert.ToBase64String(imageBytes);

        var imageDataUrl = $"data:{mimeType};base64,{base64Image}";

        var requestBody = new
        {
            model,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "text",
                            text = prompt
                        },
                        new
                        {
                            type = "image_url",
                            image_url = new
                            {
                                url = imageDataUrl
                            }
                        }
                    }
                }
            },
            temperature = 0.2,
            max_tokens = 1024,
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://models.github.ai/inference/chat/completions");

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

        request.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request);

        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.TooManyRequests ||
                responseText.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
                responseText.Contains("quota", StringComparison.OrdinalIgnoreCase))
            {
                throw new AiProviderException(
                    $"GitHub Models Vision quota or rate limit reached: {responseText}",
                    isQuotaError: true,
                    retryAfter: TimeSpan.FromMinutes(10));
            }

            throw new AiProviderException(
                $"GitHub Models Vision request failed: {responseText}",
                isQuotaError: false,
                retryAfter: TimeSpan.FromMinutes(2));
        }

        using var document = JsonDocument.Parse(responseText);

        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new AiProviderException(
                "GitHub Models Vision returned an empty response.",
                isQuotaError: false,
                retryAfter: TimeSpan.FromMinutes(2));
        }

        return content;
    }

    private string GetMimeType(string imagePath)
    {
        var extension = Path.GetExtension(imagePath).ToLower();

        return extension switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}