using System.Text;
using Cohere;
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
        DiscordGuild lGuild = await _discord.GetGuildAsync(aReminder.GuildID);
        
        DiscordMember lMember = await lGuild.GetMemberAsync(aReminder.UserID);

        await lMember.SendMessageAsync(aReminder.Message);
    }
    public async Task SendDMToOwnerAsync(string aMessage) 
    {
        DiscordGuild lGuild = await _discord.GetGuildAsync(ulong.Parse("1428047784245854310"));
    } 
    public (string, string, string?) MotDFormatter(DiscordMessage aMsg, MessageRecord aBestMsg)
    {
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
    public async Task SendWebhookMessageAsync(//Change to pass a list of DiscordMessages to have access to attachments
                        DiscordWebhook aWebhook,
                        (string userName, string content, string? footer) aFormat,
                        List<string> aMessageIDAttchmentList,
                        string aChannelID)    
    {        
        var lBot = await _discord.GetUserAsync(BotID);
        var lWebHookBuilder = new DiscordWebhookBuilder()
            .WithUsername(aFormat.userName)
            .WithContent(aFormat.content)//BuildContent(aFormat.content, aUrlList) 
            .WithAvatarUrl(lBot.AvatarUrl); 

        DiscordChannel lChannel = await _discord.GetChannelAsync(ulong.Parse(aChannelID));
        foreach (var id in aMessageIDAttchmentList)
        {
            DiscordMessage lMessage = await lChannel.GetMessageAsync(ulong.Parse(id));
            foreach (var attachment in lMessage.Attachments)
            {
                try
                {
                    var lStream = await _httpClient.GetStreamAsync(attachment.ProxyUrl);
                    lWebHookBuilder.AddFile(attachment.FileName, lStream);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to attach file {attachment.FileName}: {ex.Message}");
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
        var bot = await _discord.GetUserAsync(BotID);

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

    public string BuildContent(string aContent, List<string> aUrlList)
    {
        StringBuilder lContent = new StringBuilder(aContent);
        lContent.Append(" ");
        foreach (var url in aUrlList)
            lContent.Append(url);

        return lContent.ToString();
    }
    public async Task SendChannelMessageAsync(string aMessage, ulong aChannelID)
    {
        var lmsgBuilder = new DiscordMessageBuilder();
        lmsgBuilder.WithContent(aMessage);
        var lChannel = await _discord.GetChannelAsync(aChannelID);
        await lChannel.SendMessageAsync(lmsgBuilder);
    }
}