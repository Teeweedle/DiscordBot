using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

public class MessagingService : IReminderNotifier
{
    private readonly DiscordClient _discord;
    private readonly DatabaseHelper _dbh;
    private readonly Messaging _messaging;
    private readonly ConversationResponse _conversation;
    
    public MessagingService(DiscordClient aDiscord, 
                            DatabaseHelper aDatabaseHelper, 
                            Messaging aMessaging,
                            ConversationResponse aConversation)                            
    {
        _discord = aDiscord;
        _dbh = aDatabaseHelper;
        _messaging = aMessaging;
        _conversation = aConversation;

        _discord.MessageCreated += OnMessageCreatedAsync;
    }
    public async Task OnMessageCreatedAsync(DiscordClient sender, MessageCreateEventArgs e) 
    {
        if (!e.Author.IsBot)
            _dbh.SaveMessage(e.Message);
        if(string.Equals(e.Author.Id.ToString(), _dbh.GetTargetUserID(), StringComparison.Ordinal) &&
            string.Equals(e.Channel.Id.ToString(), _dbh.GetTargetChannelID(), StringComparison.Ordinal))
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
        DiscordWebhook lWebHook = await EnsureAvailableWebhookAsync(aChannel.Id);

        await _messaging.SendWebhookMessageAsync(lWebHook, aMessage, lFormat);
    }
    /// <summary>
    /// Posts the most interesting message of the day to the specified channel.
    /// If testMode is true, the message will be posted to the <see cref="BotTestChannelID"/> channel.
    /// </summary>
    /// <param name="testMode">If true, the message will be posted to the <see cref="BotTestChannelID"/> channel.</param>
    public async Task PostMotDAsync(MessageRecord aBestMsg, ulong aChannelID)
    {
        DiscordChannel lSourceChannel = await _discord.GetChannelAsync(ulong.Parse(aBestMsg.ChannelID));
        var lOriginalMsg = await lSourceChannel.GetMessageAsync(ulong.Parse(aBestMsg.MessageID));
        var lMotDFormat = _messaging.MotDFormatter(lOriginalMsg, aBestMsg);

        DiscordWebhook lWebHook = await EnsureAvailableWebhookAsync(aChannelID);

        await _messaging.SendWebhookMessageAsync(lWebHook, lOriginalMsg, lMotDFormat);                         
    }
    public async Task<DiscordWebhook> EnsureAvailableWebhookAsync(ulong aChannelID)
    {
        DiscordChannel lChannel = await _discord.GetChannelAsync(aChannelID);

        string? lWebHookID = _dbh.GetWebHookID(lChannel.GuildId!.Value.ToString(), aChannelID.ToString());
        string? lWebHookToken = _dbh.GetWebHookToken(lChannel.GuildId!.Value.ToString(), aChannelID.ToString());

        if (!string.IsNullOrEmpty(lWebHookID) && !string.IsNullOrEmpty(lWebHookToken))
        {
            return await _discord.GetWebhookWithTokenAsync(ulong.Parse(lWebHookID!), lWebHookToken!);
        }

        var lExistingWebhooks = await lChannel.GetWebhooksAsync();
        var lWebHook = lExistingWebhooks.FirstOrDefault(x => x.Name == "OnThisDayWebhook");
        
        if (lWebHook == null)
        {
            lWebHook = await lChannel.CreateWebhookAsync("OnThisDayWebhook");

            if (lWebHook == null)
            {
                throw new Exception("Failed to create webhook., bot does not have permissions.");
            }
        }
        
        _dbh.SaveWebHook(
            lChannel.GuildId!.Value.ToString(), 
            aChannelID.ToString(), 
            lWebHook.Id.ToString(), 
            lWebHook.Token!);
        return await _discord.GetWebhookWithTokenAsync(lWebHook.Id, lWebHook.Token!);
    }
    
    public Task SendReminderAsync(ReminderRecord aReminderRecord)
    {
        throw new NotImplementedException();
    }
}