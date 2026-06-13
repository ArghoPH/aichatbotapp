namespace AiChatbotApp.Services;

public class AiProviderStatus
{
    public string Name { get; set; } = string.Empty;

    public int Priority { get; set; }

    public bool IsConfigured { get; set; }

    public bool IsInCooldown { get; set; }

    public DateTime? CooldownUntil { get; set; }

    public bool IsLastUsed { get; set; }

    public string Status
    {
        get
        {
            if (!IsConfigured)
            {
                return "Not Configured";
            }

            if (IsInCooldown)
            {
                return "Cooldown";
            }

            if (IsLastUsed)
            {
                return "Active";
            }

            return "Ready to Try";
        }
    }
}