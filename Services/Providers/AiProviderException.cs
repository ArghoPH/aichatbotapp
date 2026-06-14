namespace AiChatbotApp.Services.Providers;

public class AiProviderException : Exception
{
    public bool IsQuotaError { get; }

    public TimeSpan? RetryAfter { get; }

    public AiProviderException(
        string message,
        bool isQuotaError = false,
        TimeSpan? retryAfter = null)
        : base(message)
    {
        IsQuotaError = isQuotaError;
        RetryAfter = retryAfter;
    }
}
