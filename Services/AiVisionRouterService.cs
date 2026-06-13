using AiChatbotApp.Services.Providers;
using Microsoft.AspNetCore.Hosting;

namespace AiChatbotApp.Services;

public class AiVisionRouterService
{
    private readonly IEnumerable<IAiVisionProvider> _visionProviders;
    private readonly IWebHostEnvironment _environment;

    public AiVisionRouterService(
        IEnumerable<IAiVisionProvider> visionProviders,
        IWebHostEnvironment environment)
    {
        _visionProviders = visionProviders;
        _environment = environment;
    }

    public async Task<AiRouterResult> AnalyzeImageAsync(string prompt, string imagePath)
    {
        var providers = _visionProviders
            .Where(x => x.IsConfigured)
            .OrderBy(x => x.Priority)
            .ToList();

        if (!providers.Any())
        {
            return new AiRouterResult
            {
                ProviderName = "No Vision Provider",
                Text = "No image analysis provider is configured. Please configure Gemini:ApiKey or GitHubModels:ApiKey."
            };
        }

        var errors = new List<string>();

        foreach (var provider in providers)
        {
            try
            {
                var result = await provider.AnalyzeImageAsync(prompt, imagePath);

                return new AiRouterResult
                {
                    ProviderName = provider.Name,
                    Text = result
                };
            }
            catch (Exception ex)
            {
                var errorMessage = $"{provider.Name} failed: {ex.Message}";

                errors.Add(errorMessage);

                Console.WriteLine("======================================");
                Console.WriteLine("VISION PROVIDER ERROR");
                Console.WriteLine(errorMessage);
                Console.WriteLine("======================================");
            }
        }

        var finalMessage = "All image analysis providers are temporarily unavailable. Please try again later.";

        if (_environment.IsDevelopment())
        {
            finalMessage += Environment.NewLine;
            finalMessage += Environment.NewLine;
            finalMessage += "Debug details:";
            finalMessage += Environment.NewLine;
            finalMessage += string.Join(Environment.NewLine, errors);
        }

        return new AiRouterResult
        {
            ProviderName = "Vision Router",
            Text = finalMessage
        };
    }
}