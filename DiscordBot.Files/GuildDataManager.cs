
using Microsoft.Extensions.Logging;

public class GuildDataManager : IGuildDataManager
{
    private readonly IMessagingService _messagingService;
    private readonly IMotdService _motdService;
    private readonly IReminderService _reminderService;
    private readonly IFeatureGateService _featureGateService;
    private readonly ILogger<GuildDataManager> _logger;
    public GuildDataManager(IMessagingService aMessagingService, 
                            IMotdService aMotdService,
                            IReminderService aReminderService,
                            IFeatureGateService aFeatureGateService,
                            ILogger<GuildDataManager> aLogger)
    {
        _messagingService = aMessagingService;
        _motdService = aMotdService;
        _reminderService = aReminderService;
        _featureGateService = aFeatureGateService;
        _logger = aLogger;
    }

    public async Task PurgeGuildDataAsync(ulong aGuildID)
    {
        _logger.LogInformation($"Purging guild {aGuildID} data...", aGuildID);
        await Task.WhenAll(
            SafeExecuteAsync(() => _messagingService.PurgeGuildMessagesAsync(aGuildID), $"messages in guild {aGuildID}"),
            SafeExecuteAsync(() => _messagingService.PurgeGuildWebHooksAsync(aGuildID), $"webhooks in guild {aGuildID}"),
            SafeExecuteAsync(() => _messagingService.PurgeGuildTargetUserAndChannelsAsync(aGuildID), $"target user/channels in guild {aGuildID}"),
            SafeExecuteAsync(() => _motdService.PurgeGuildMotdSettingsAsync(aGuildID), $"motd settings in guild {aGuildID}"),
            SafeExecuteAsync(() => _reminderService.PurgeGuildRemindersAsync(aGuildID), $"reminders in guild {aGuildID}"),
            SafeExecuteAsync(() => _featureGateService.PurgeGuildFeaturesAsync(aGuildID), $"features in guild {aGuildID}")
        );
        _logger.LogInformation($"Purged of guild {aGuildID} data complete.", aGuildID);        
    }
    public async Task PurgeUserDataAsync(ulong aUserID, ulong aGuildID)
    {
        _logger.LogInformation($"Purging user {aUserID} data...", aUserID);
        await Task.WhenAll(
            SafeExecuteAsync(() => _reminderService.PurgeUserRemindersAsync(aUserID, aGuildID), $"reminders for User {aUserID} in guild {aGuildID}")
        );
        _logger.LogInformation($"Purge of user {aUserID} data complete.", aUserID);
    }
    private async Task SafeExecuteAsync(Func<Task> aAction,  string aActionName)
    {
        try
        {
            await aAction();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to purge {ActionName}", aActionName);
        }
    }
}