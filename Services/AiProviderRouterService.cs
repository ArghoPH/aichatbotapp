using System.Collections.Concurrent;
using AiChatbotApp.Services.Providers;

namespace AiChatbotApp.Services;

public class AiProviderRouterService
{
    private readonly IEnumerable<IAiTextProvider> _providers;

    private static readonly ConcurrentDictionary<string, DateTime> Cooldowns = new();

    private static string? LastSuccessfulProviderName;

    public AiProviderRouterService(IEnumerable<IAiTextProvider> providers)
    {
        _providers = providers;
    }

    public async Task<AiRouterResult> GenerateTextAsync(string prompt)
    {
        var availableProviders = _providers
            .Where(x => x.IsConfigured)
            .OrderBy(x => x.Priority)
            .ToList();

        if (!availableProviders.Any())
        {
            return new AiRouterResult
            {
                Text = "No AI provider is configured.",
                ProviderName = "None"
            };
        }

        foreach (var provider in availableProviders)
        {
            if (IsProviderInCooldown(provider.Name))
            {
                continue;
            }

            try
            {
                var response = await provider.GenerateTextAsync(prompt);

                LastSuccessfulProviderName = provider.Name;

                return new AiRouterResult
                {
                    Text = response,
                    ProviderName = provider.Name
                };
            }
            catch (AiProviderException ex)
            {
                if (ex.IsQuotaError)
                {
                    var cooldown = ex.RetryAfter ?? TimeSpan.FromMinutes(15);

                    SetCooldown(provider.Name, cooldown);

                    continue;
                }

                SetCooldown(provider.Name, TimeSpan.FromMinutes(2));

                continue;
            }
            catch
            {
                SetCooldown(provider.Name, TimeSpan.FromMinutes(2));

                continue;
            }
        }

        return new AiRouterResult
        {
            Text = "All free AI providers are temporarily unavailable or quota-limited. Please try again later.",
            ProviderName = "Fallback"
        };
    }

    public List<AiProviderStatus> GetProviderStatuses()
    {
        return _providers
            .OrderBy(x => x.Priority)
            .Select(provider =>
            {
                var isInCooldown = IsProviderInCooldown(provider.Name);

                DateTime? cooldownUntil = null;

                if (Cooldowns.TryGetValue(provider.Name, out var storedCooldownUntil))
                {
                    if (DateTime.Now < storedCooldownUntil)
                    {
                        cooldownUntil = storedCooldownUntil;
                    }
                }

                return new AiProviderStatus
                {
                    Name = provider.Name,
                    Priority = provider.Priority,
                    IsConfigured = provider.IsConfigured,
                    IsInCooldown = isInCooldown,
                    CooldownUntil = cooldownUntil,
                    IsLastUsed = LastSuccessfulProviderName == provider.Name
                };
            })
            .ToList();
    }

    private bool IsProviderInCooldown(string providerName)
    {
        if (!Cooldowns.TryGetValue(providerName, out var cooldownUntil))
        {
            return false;
        }

        if (DateTime.Now >= cooldownUntil)
        {
            Cooldowns.TryRemove(providerName, out _);
            return false;
        }

        return true;
    }

    private void SetCooldown(string providerName, TimeSpan duration)
    {
        Cooldowns[providerName] = DateTime.Now.Add(duration);
    }
}