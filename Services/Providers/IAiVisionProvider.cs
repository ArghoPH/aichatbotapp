namespace AiChatbotApp.Services.Providers;

public interface IAiVisionProvider
{
    string Name { get; }

    int Priority { get; }

    bool IsConfigured { get; }

    Task<string> AnalyzeImageAsync(string prompt, string imagePath);
}