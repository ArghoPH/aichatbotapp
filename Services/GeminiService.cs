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

    public async Task<string> AnalyzeImageAsync(string prompt, string imagePath)
    {
        var apiKey = _configuration["Gemini:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new Exception("Gemini API Key not found.");
        }

        var imageBytes = await File.ReadAllBytesAsync(imagePath);
        var base64Image = Convert.ToBase64String(imageBytes);
        var mimeType = GetMimeType(imagePath);

        var url =
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={apiKey}";

        var body = new
        {
            contents = new[]
            {
            new
            {
                parts = new object[]
                {
                    new
                    {
                        inline_data = new
                        {
                            mime_type = mimeType,
                            data = base64Image
                        }
                    },
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
            throw new Exception(responseContent);
        }

        var data = JObject.Parse(responseContent);

        return data["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()
               ?? "No image analysis response returned.";
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
            _ => "image/jpeg"
        };
    }
}
