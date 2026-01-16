using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

public class Messaging
{
    private readonly DiscordClient _discord;
    private readonly HttpClient _httpClient;
    private readonly ILogger<Messaging> _logger;
    public Messaging(DiscordClient aDiscord, 
                    HttpClient aHttpClient,
                    ILogger<Messaging> aLogger)
    {
        _discord = aDiscord;
        _httpClient = aHttpClient;
        _logger = aLogger;
    }     
    public async Task SendReminderToUserAsync(ReminderRecord aReminder) 
    {
        DiscordGuild lGuild = await _discord.GetGuildAsync(aReminder.GuildID);
        
        DiscordMember lMember = await lGuild.GetMemberAsync(aReminder.UserID);

        await lMember.SendMessageAsync(aReminder.Message);
    }
    public async Task SendDMToOwnerAsync(string aMessage, ulong aGuildID) 
    {
        DiscordGuild lGuild = await _discord.GetGuildAsync(aGuildID);
        ulong lOwnerId =  lGuild.OwnerId;
        DiscordMember lOwner = await lGuild.GetMemberAsync(lOwnerId);
        await lOwner.SendMessageAsync(aMessage);  
    } 
    public (string, string, string?) MotDFormatter(DiscordMessage aMsg, MessageRecord aBestMsg)
    {   
        //TODO: Can't handle polls, upgrade DSharpPlus to 5.x
        string lUserName = $"\nOn this day in {aBestMsg.Timestamp.Year}";
        string lContent = $"------------------------------------------------\n" 
                + $"<@{aBestMsg.AuthorID}> said:\n"
                + $"{aBestMsg.Content}";
        string lFooter = $"------------------------------------------------\n" 
                + $"[view orignal message]({aMsg.JumpLink})";

        return (lUserName, lContent, lFooter);
    }
    public async Task<(string, string, string?)> SultryResponseFormat(string aMsg, DiscordUser aUser, DiscordGuild aGuild)
    {   
        DiscordMember lMember = await aGuild.GetMemberAsync(aUser.Id);
        string lUserNickName = lMember.Nickname ?? aUser.Username;
        string lUserName = $"\n{lUserNickName}' Sex Kitten:";
        string lContent = $"{aMsg}";
        string lFooter = string.Empty;

        return (lUserName, lContent, lFooter);
    }    
    /// <summary>
    /// Formats and sends a message using a webhook
    /// </summary>
    /// <param name="aMessage">The Original message</param>
    /// <param name="aBestMsg">Best message with grouped content</param>
    /// <param name="aChannelID"></param>
    /// <returns></returns>
    public async Task SendWebhookMessageAsync(
                        DiscordWebhook aWebhook,
                        (string userName, string content, string? footer) aFormat,
                        List<string> aMessageIDAttchmentList,
                        string aChannelID)    
    {        
        var lBot = _discord.CurrentUser;
        var lWebHookBuilder = new DiscordWebhookBuilder()
            .WithUsername(aFormat.userName)
            .WithContent(aFormat.content)
            .WithAvatarUrl(lBot.AvatarUrl); 
        int lFileIndex = 0;
        DiscordChannel lChannel = await _discord.GetChannelAsync(ulong.Parse(aChannelID));
        foreach (var id in aMessageIDAttchmentList)
        {
            DiscordMessage lMessage = await lChannel.GetMessageAsync(ulong.Parse(id));
            foreach (var attachment in lMessage.Attachments)
            {
                try
                {
                    var lStream = await _httpClient.GetStreamAsync(attachment.ProxyUrl);

                    var lExtension = Path.GetExtension(attachment.FileName);
                    var lUniqueFileName = $"{id}_{lFileIndex++}{lExtension}";
                    lWebHookBuilder.AddFile(lUniqueFileName, lStream);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to attach file {FileName} to channel {ChannelID}", attachment.FileName, lChannel.Id);
                }
            }
        }
        await aWebhook.ExecuteAsync(lWebHookBuilder);

        if (!string.IsNullOrEmpty(aFormat.footer))
        {
            var lFooterBuilder = new DiscordWebhookBuilder()
                .WithUsername(aFormat.userName)
                .WithContent(aFormat.footer)
                .WithAvatarUrl(lBot.AvatarUrl);
            await aWebhook.ExecuteAsync(lFooterBuilder);
        }
    }
    public async Task SendWebhookMessageAsync(
                        DiscordWebhook webhook,
                        (string userName, string content, string? footer) format)
    {
        var bot = _discord.CurrentUser;

        var builder = new DiscordWebhookBuilder()
            .WithUsername(format.userName)
            .WithContent(format.content)
            .WithAvatarUrl(bot.AvatarUrl);

        await webhook.ExecuteAsync(builder);

        if (!string.IsNullOrEmpty(format.footer))
        {
            var footerBuilder = new DiscordWebhookBuilder()
                .WithUsername(format.userName)
                .WithContent(format.footer)
                .WithAvatarUrl(bot.AvatarUrl);

            await webhook.ExecuteAsync(footerBuilder);
        }
    }

    public async Task SendChannelMessageAsync(string aMessage, ulong aChannelID)
    {
        var lmsgBuilder = new DiscordMessageBuilder();
        lmsgBuilder.WithContent(aMessage);
        var lChannel = await _discord.GetChannelAsync(aChannelID);
        await lChannel.SendMessageAsync(lmsgBuilder);
    }
}