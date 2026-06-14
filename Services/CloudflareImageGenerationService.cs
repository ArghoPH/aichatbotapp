using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AiChatbotApp.Services;

public class CloudflareImageGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public CloudflareImageGenerationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _httpClient = httpClientFactory.CreateClient();
        _configuration = configuration;
        _environment = environment;
    }

    public async Task<CloudflareGeneratedImageResult> GenerateImageAsync(string prompt)
    {
        var accountId = _configuration["Cloudflare:AccountId"];
        var apiToken = _configuration["Cloudflare:ApiToken"];

        var model =
            _configuration["Cloudflare:ImageModel"] ??
            "@cf/stabilityai/stable-diffusion-xl-base-1.0";

        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new Exception("Cloudflare Account ID is not configured.");
        }

        if (string.IsNullOrWhiteSpace(apiToken))
        {
            throw new Exception("Cloudflare API token is not configured.");
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new Exception("Image prompt cannot be empty.");
        }

        var requestBody = new
        {
            prompt
        };

        var json = JsonSerializer.Serialize(requestBody);

        var url =
            $"https://api.cloudflare.com/client/v4/accounts/{accountId}/ai/run/{model}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", apiToken);

        request.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request);

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Cloudflare image generation failed: {errorText}");
        }

        var imageBytes = await response.Content.ReadAsByteArrayAsync();

        if (imageBytes.Length == 0)
        {
            throw new Exception("Cloudflare returned an empty image response.");
        }

        var extension = contentType.Contains("jpeg")
            ? ".jpg"
            : ".png";

        var fileName = $"cf_generated_{Guid.NewGuid()}{extension}";

        var folderPath = Path.Combine(
            _environment.WebRootPath,
            "generated");

        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);

        await File.WriteAllBytesAsync(filePath, imageBytes);

        return new CloudflareGeneratedImageResult
        {
            Prompt = prompt,
            Model = model,
            ImagePath = $"/generated/{fileName}"
        };
    }
}

public class CloudflareGeneratedImageResult
{
    public string Prompt { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string ImagePath { get; set; } = string.Empty;
}
