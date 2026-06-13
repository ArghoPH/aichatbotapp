using Microsoft.Extensions.Configuration;

namespace AiChatbotApp.Services.Providers;

public class GeminiVisionProvider : IAiVisionProvider
{
    private readonly GeminiService _geminiService;
    private readonly IConfiguration _configuration;

    public GeminiVisionProvider(
        GeminiService geminiService,
        IConfiguration configuration)
    {
        _geminiService = geminiService;
        _configuration = configuration;
    }

    public string Name => "Gemini Vision";

    public int Priority => 1;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["Gemini:ApiKey"]);

    public async Task<string> AnalyzeImageAsync(string prompt, string imagePath)
    {
        return await _geminiService.AnalyzeImageAsync(prompt, imagePath);
    }
}