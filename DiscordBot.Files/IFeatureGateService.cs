using DSharpPlus.SlashCommands;

public interface IFeatureGateService
{
    Task<bool> IsFeatureEnabledAsync(ulong aGuildId, string aFeatureName);
    Task SetFeatureEnabledAsync(ulong aGuildId, string aFeatureName, bool aEnabled);
    Task<bool> EnsureFeatureEnabledAsync(InteractionContext aContext, string aFeatureName);
    Task PurgeGuildFeaturesAsync(ulong aGuildID);
}