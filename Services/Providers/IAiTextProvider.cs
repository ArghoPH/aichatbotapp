namespace AiChatbotApp.Services.Providers;

public interface IAiTextProvider
{
    string Name { get; }

    int Priority { get; }

    bool IsConfigured { get; }

    Task<string> GenerateTextAsync(string prompt);
}