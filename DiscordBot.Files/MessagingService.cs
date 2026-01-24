using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

public class MessagingService : IReminderNotifier, IMessagingService
{
    private readonly DiscordClient _discord;
    private readonly DatabaseHelper _dbh;
    private readonly Messaging _messaging;
    private readonly ConversationResponse _conversation;
    private readonly ILogger<MessagingService> _logger;
    
    public MessagingService(DiscordClient aDiscord, 
                            DatabaseHelper aDatabaseHelper, 
                            Messaging aMessaging,
                            ConversationResponse aConversation,
                            ILogger<MessagingService> aLogger)                            
    {
        _discord = aDiscord;
        _dbh = aDatabaseHelper;
        _messaging = aMessaging;
        _conversation = aConversation;
        _logger = aLogger;

        _discord.MessageCreated += OnMessageCreatedAsync;
    }
    public async Task OnMessageCreatedAsync(DiscordClient sender, MessageCreateEventArgs e) 
    {
        if (!e.Author.IsBot)
            _dbh.SaveMessage(e.Message);
        if(string.Equals(e.Author.Id.ToString(), _dbh.GetTargetUserID(e.Guild.Id.ToString()), StringComparison.Ordinal) &&
            string.Equals(e.Channel.Id.ToString(), _dbh.GetTargetChannelID(e.Guild.Id.ToString()), StringComparison.Ordinal))
        {
            await RespondToUserAsync(e.Message, e.Channel, e.Author);
        }                
    }
    public async Task RespondToUserAsync(DiscordMessage aMessage, DiscordChannel aChannel, DiscordUser aUser)
    {
        if(!aChannel.GuildId.HasValue)
            return;

        var lGuild = await _discord.GetGuildAsync(aChannel.GuildId.Value);

        string lReponse = await _conversation.TryBuildResponse(aMessage);
        if (lReponse == null)
            return; // No response was built
        var lFormat = await _messaging.SultryResponseFormat(lReponse, aUser, lGuild);
        WebhookResult lWebhookResult = await EnsureAvailableWebhookAsync(aChannel.Id);
        if (!lWebhookResult.IsSuccess)
        {
            _logger.LogError(lWebhookResult.Error);
            return;
        }

        await _messaging.SendWebhookMessageAsync(lWebhookResult.Webhook!, lFormat);
    }
    /// <summary>
    /// Posts the message of the day to the specified channel. Uses a webhook to post the message.
    /// </summary>
    /// <param name="aBestMsg">The message to post</param>
    /// <param name="aChannelID">The channel to post the message to</param>
    public async Task PostMotdAsync(MessageRecord aBestMsg, ulong aChannelID)
    {
        DiscordChannel lSourceChannel = await _discord.GetChannelAsync(ulong.Parse(aBestMsg.ChannelID));
        var lOriginalMsg = await lSourceChannel.GetMessageAsync(ulong.Parse(aBestMsg.MessageID));
        var lMotDFormat = _messaging.MotDFormatter(lOriginalMsg, aBestMsg);

        // DiscordWebhook? lWebHook = await EnsureAvailableWebhookAsync(aChannelID);
        WebhookResult lWebhookResult = await EnsureAvailableWebhookAsync(aChannelID);
        if (!lWebhookResult.IsSuccess)
        {
            _logger.LogError(lWebhookResult.Error);
            return;            
        }
        await _messaging.SendWebhookMessageAsync(lWebhookResult.Webhook!, 
                                                lMotDFormat, 
                                                aBestMsg.MessageIDAttachmentList, 
                                                aBestMsg.ChannelID);
    }
    public async Task<WebhookResult> EnsureAvailableWebhookAsync(ulong aChannelID)
    {
        DiscordChannel lChannel = await _discord.GetChannelAsync(aChannelID);
        string lGuildID = lChannel.GuildId!.Value.ToString();

        string? lWebHookID = _dbh.GetWebHookID(lGuildID, aChannelID.ToString());
        string? lWebHookToken = _dbh.GetWebHookToken(lGuildID, aChannelID.ToString());
        DiscordWebhook? lWebHook;
        if (!string.IsNullOrEmpty(lWebHookID) && !string.IsNullOrEmpty(lWebHookToken))
        {
            try
            {
                lWebHook = await _discord.GetWebhookWithTokenAsync(ulong.Parse(lWebHookID!), lWebHookToken!);
                return new WebhookResult(true, lWebHook, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stored webhook for guild {GuildID} is invalid for channel {ChannelID}.", 
                                lGuildID, aChannelID);                
            }
        }

        var lExistingWebhooks = await lChannel.GetWebhooksAsync();
        lWebHook = lExistingWebhooks.FirstOrDefault(x => x.Name == "OnThisDayWebhook");
        
        if (lWebHook == null)
        {
            try
            {
                lWebHook = await lChannel.CreateWebhookAsync("OnThisDayWebhook");                    
            }
            catch (System.Exception ex)
            {
                return new WebhookResult(false, null, $"Failed to create webhook for channel {aChannelID}. Likely a permission issue.");
            }
        }
        
        _dbh.SaveWebHook(
            lChannel.GuildId!.Value.ToString(), 
            aChannelID.ToString(), 
            lWebHook.Id.ToString(), 
            lWebHook.Token!);

        return new WebhookResult(true, lWebHook, null);
    }
    public async Task SendMissingMotdChannelAsync(ulong aGuildID)
    {
        await _messaging.SendDMToOwnerAsync("The MOTD channel has not been set. Please use `/setmotdchannel` to set it.", aGuildID);
    }
    public Task SendNoMotdFoundAsync(ulong aChannelID)
    {
        return _messaging.SendChannelMessageAsync("Today is a slow day in history. No messages were found for today.",
                                                 aChannelID);
    }
    public Task SendReminderAsync(ReminderRecord aReminderRecord)
    {
        throw new NotImplementedException();
    }

    public async Task PurgeGuildMessagesAsync(ulong aGuildID)
    {
        int lMessagesPurged = await _dbh.PurgeGuildMessages(aGuildID);
        _logger.LogInformation($"Purged {lMessagesPurged} messages from guild {aGuildID}");
    }
    public async Task PurgeGuildWebHooksAsync(ulong aGuildID)
    {
        int lWebHooksPurged = await _dbh.PurgeGuildWebHookInfo(aGuildID);
        _logger.LogInformation($"Purged {lWebHooksPurged} webhooks from guild {aGuildID}");
    }
    public async Task PurgeGuildTargetUserAndChannelsAsync(ulong aGuildID)
    {
        int lTargetUserAndChannelsPurged = await _dbh.PurgeGuildTargetUserAndChannel(aGuildID);
        _logger.LogInformation($"Purged {lTargetUserAndChannelsPurged} target user and channels from guild {aGuildID}");
    }
}