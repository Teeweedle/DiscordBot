
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;

public class FeatureGateService : IFeatureGateService
{
    private readonly DatabaseHelper _dbh;
    private readonly ILogger<FeatureGateService> _logger;
    private static readonly Dictionary<string, FeatureDefaults> _defaults = new()
    {
        ["AI"] = new FeatureDefaults
        {
            EnabledByDefault = false
        },
        ["MOTD"] = new FeatureDefaults
        {
            EnabledByDefault = true
        }
    };
    public FeatureGateService(DatabaseHelper adbh,
                                ILogger<FeatureGateService> aLogger)
    {
        _dbh = adbh;
        _logger = aLogger;
    }

    /// <summary>
    /// Checks if a feature is enabled for a guild.
    /// First, it checks if the feature is enabled in the database.
    /// If the feature is not found in the database, it returns the default value for the feature.
    /// </summary>
    /// <param name="aGuildId">The ID of the guild to check</param>
    /// <param name="aFeatureName">The name of the feature to check</param>
    /// <returns>True if the feature is enabled, false otherwise</returns>
    public async Task<bool> IsFeatureEnabledAsync(ulong aGuildId, string aFeatureName)
    {
        var lEnabled =  _dbh.IsFeatureEnabled(aGuildId, aFeatureName);
        
        if(lEnabled != null) 
            return lEnabled.Value;

        return _defaults[aFeatureName].EnabledByDefault;
    }

    public async Task SetFeatureEnabledAsync(ulong aGuildId, string aFeatureName, bool aEnabled)
    {
        _dbh.SetFeatureEnabled(aGuildId, aFeatureName, aEnabled);
    } 
    public async Task<bool> EnsureFeatureEnabledAsync(InteractionContext aContext, 
                                                        string aFeatureName)
    {
        if(await IsFeatureEnabledAsync(aContext.Guild.Id, aFeatureName))
            return true;
        await aContext.EditResponseAsync(new DiscordWebhookBuilder()
        .AddEmbed(new DiscordEmbedBuilder()
            .WithTitle($"Feature `{aFeatureName}` is locked.")
            .WithDescription("This feature costs 💵 💵 💵 to run, so it is disabled by default.")
            .WithColor(DiscordColor.Red)));
        return false;
    }

    public async Task PurgeGuildFeaturesAsync(ulong aGuildID)
    {
        int lFeaturesPurged = await _dbh.PurgeGuildFeatures(aGuildID);
        _logger.LogInformation($"Purged {lFeaturesPurged} features from guild {aGuildID}");
    }
}