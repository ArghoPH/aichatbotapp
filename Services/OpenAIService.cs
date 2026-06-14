using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiChatbotApp.Services;

public class OpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OpenAIService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    private string GetApiKey()
    {
        var apiKey = _configuration["OpenAI:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new Exception("OpenAI API key is missing. Please set it using dotnet user-secrets.");
        }

        return apiKey;
    }

    public async Task<string> GetChatResponseAsync(string userMessage)
    {
        var apiKey = GetApiKey();

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new
        {
            model = "gpt-5.4-mini",
            input = userMessage
        };

        var json = JsonConvert.SerializeObject(requestBody);

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/responses",
            content);

        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"OpenAI API Error: {responseText}");
        }

        var data = JObject.Parse(responseText);

        var outputText = data["output"]?[0]?["content"]?[0]?["text"]?.ToString();

        return outputText ?? "No response received from AI.";
    }

    public async Task<string> AnalyzeImageAsync(string userMessage, string imagePath)
    {
        var apiKey = GetApiKey();

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var imageBytes = await File.ReadAllBytesAsync(imagePath);
        var base64Image = Convert.ToBase64String(imageBytes);

        var requestBody = new
        {
            model = "gpt-5.4-mini",
            input = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text = userMessage
                        },
                        new
                        {
                            type = "input_image",
                            image_url = $"data:image/jpeg;base64,{base64Image}"
                        }
                    }
                }
            }
        };

        var json = JsonConvert.SerializeObject(requestBody);

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/responses",
            content);

        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"OpenAI Image Analysis Error: {responseText}");
        }

        var data = JObject.Parse(responseText);

        var outputText = data["output"]?[0]?["content"]?[0]?["text"]?.ToString();

        return outputText ?? "No image analysis response received.";
    }

    public async Task<string> GenerateImageAsync(string prompt)
    {
        var apiKey = GetApiKey();

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new
        {
            model = "gpt-image-2",
            prompt = prompt,
            size = "1024x1024"
        };

        var json = JsonConvert.SerializeObject(requestBody);

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/images/generations",
            content);

        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"OpenAI Image Generation Error: {responseText}");
        }

        var data = JObject.Parse(responseText);

        var base64Image = data["data"]?[0]?["b64_json"]?.ToString();

        if (string.IsNullOrWhiteSpace(base64Image))
        {
            throw new Exception("No generated image received from OpenAI.");
        }

        return base64Image;
    }
}
