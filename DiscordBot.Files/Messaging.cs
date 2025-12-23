using DSharpPlus;
using DSharpPlus.Entities;

public class Messaging
{
    private readonly DiscordClient _discord;
    private readonly HttpClient _httpClient;
    private readonly ulong BotID = 1428047784245854310;

    public Messaging(DiscordClient aDiscord, 
                    HttpClient aHttpClient)
    {
        _discord = aDiscord;
        _httpClient = aHttpClient;
    }     
    public async Task SendDMToUserAsync(ReminderRecord aReminder) 
    {
        DiscordGuild guild = await _discord.GetGuildAsync(aReminder.GuildID);
        
        DiscordMember member = await guild.GetMemberAsync(aReminder.UserID);

        await member.SendMessageAsync(aReminder.Message);
    }    

    public (string, string, string?) MotDFormatter(DiscordMessage aMsg, MessageRecord aBestMsg)
    {
        string lUserName = $"\nOn this day in {aBestMsg.Timestamp.Year}";
        string lContent = $"------------------------------------------------\n" 
                + $"<@{aBestMsg.AuthorID}> said: \n"
                + $"{aBestMsg.Content} \n\n";
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
                        DiscordMessage? aMessage, 
                        (string userName, string content, string? footer) aFormat)    
    {

        var lBot = await _discord.GetUserAsync(BotID);
        
        var lWebHookBuilder = new DiscordWebhookBuilder()
            .WithUsername(aFormat.userName)
            .WithContent(aFormat.content)
            .WithAvatarUrl(lBot.AvatarUrl);

        if(aMessage != null)
        {
           foreach (var attachment in aMessage.Attachments)
            {
                var lStream = await _httpClient.GetStreamAsync(attachment.Url);
                lWebHookBuilder.AddFile(attachment.FileName, lStream);
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
    public async Task SendChannelMessageAsync(string aMessage, ulong aChannelID)
    {
        var lmsgBuilder = new DiscordMessageBuilder();
        lmsgBuilder.WithContent(aMessage);
        var lChannel = await _discord.GetChannelAsync(aChannelID);
        await lChannel.SendMessageAsync(lmsgBuilder);
    }
}