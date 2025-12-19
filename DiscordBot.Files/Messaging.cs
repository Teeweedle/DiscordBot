using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;

public class Messaging
{
    private static readonly int RespondDelay = 2;
    private readonly Dictionary<ulong, DateTime> _lastBotRespond = new();
    private readonly DatabaseHelper _dbh;
    private readonly DiscordClient _discord;
    private readonly CohereClient _cohereClient;
    private readonly HttpClient _httpClient;
    // private readonly MessagingService _messagingService;
    private readonly ulong BotID = 1428047784245854310;

    private readonly ulong BotTestChannelID = 1445164447093227633;
    public Messaging(DatabaseHelper aDb, 
                    DiscordClient aDiscord, 
                    CohereClient aCohereClient, 
                    HttpClient aHttpClient)
    {
        _dbh = aDb;
        _discord = aDiscord;
        _cohereClient = aCohereClient;
        _httpClient = aHttpClient;
    }    
    /// <summary>
    /// Checks if a bot can respond to a user in a message.
    /// </summary>
    /// <param name="aUser">The user to respond to.</param>
    /// <param name="aMessage">The message to respond to.</param>
    /// <returns>True if the bot can respond, false otherwise.</returns>
    /// <remarks>
    /// This function checks if the bot has responded to the user in the last <see cref="RespondDelay"/> minutes,
    /// if the message is empty, if the message contains a URL, or if the message starts with "http://".
    /// </remarks>
    private bool CanRespond(DiscordUser aUser, DiscordMessage aMessage)
    {            
        //No empty messages
        if (string.IsNullOrWhiteSpace(aMessage.Content))
        {
            Console.WriteLine("Empty message");
            return false;
        }
            
        //No URLs
        if(Uri.IsWellFormedUriString(aMessage.Content.Trim(), UriKind.Absolute))
        {
            Console.WriteLine("No URL");
            return false;
        }
            
        //No http links
        if(aMessage.Content.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("No http link");
            return false;
        }
            
        return true;
    }
    public async Task RespondToUser(DiscordMessage aMessage, DiscordChannel aChannel, DiscordUser aUser)
    {
        if(!CanRespond(aUser, aMessage))
            return;
        
        if(!aChannel.GuildId.HasValue) 
            return;
        Console.WriteLine("Responding...");
        DiscordGuild lGuild = await _discord.GetGuildAsync(aChannel.GuildId.Value);

        string lPrompt = $"Respond to the following message as if you are another user in Discord but do it " 
            + $"in a playful manner. The reponse should be funny, helpful, and informative "
            + $"(maybe a little sarcastic and flirty): {aMessage.Content}";
        string lResponse = await _cohereClient.AskAsync(lPrompt);
        var lSultryFormat = await SultryResponse(lResponse, aUser, lGuild);
        _lastBotRespond[aUser.Id] = DateTime.UtcNow;

        await SendWebhookMessageAsync(null, aChannel.Id, lSultryFormat);
    }
    /// <summary>
    /// Posts the most interesting message of the day to the testing channel. For testing purposes only
    /// If <paramref name="testMode"/> is true, the message will be posted to the <see cref="BotTestChannelID"/> channel.
    /// </summary>
    /// <param name="testMode">If true, the message will be posted to the <see cref="BotTestChannelID"/> channel.</param>
    public async Task PostMotDAsync(bool testMode)
    {
        Console.WriteLine($"(Test Mode: {testMode})");

        if (!testMode)
            throw new Exception("PostMotDAsync(bool) must be called with testMode set to true.");

        await PostMotDAsync(BotTestChannelID);
    }
    /// <summary>
    /// Posts the most interesting message of the day to the specified channel.
    /// If testMode is true, the message will be posted to the <see cref="BotTestChannelID"/> channel.
    /// </summary>
    /// <param name="testMode">If true, the message will be posted to the <see cref="BotTestChannelID"/> channel.</param>
    public async Task PostMotDAsync(ulong aChannelID)
    {
        Console.WriteLine($"Posting MOTD... ");
        var MOTDService = new OnThisDayService();

        List<MessageRecord> lMessages = _dbh.GetTodaysMsgs(DateTime.UtcNow.Date);
        List<MessageRecord> lMergedMessages = MergeMultiPartMessages(lMessages);
        ulong lTargetChannelID;
        if (aChannelID == BotTestChannelID)
        {
            lTargetChannelID = aChannelID;
        }else{
            string? lMotdID = _dbh.GetMoTDChannelID()!;
            if (string.IsNullOrEmpty(lMotdID))
            {
                await SendChannelMessageAsync("No MoTD channel set.", aChannelID);
                return;
            }
            lTargetChannelID = ulong.Parse(lMotdID); 
        } 
        
        if (lMergedMessages.Count == 0)
        {
            await SendChannelMessageAsync("Today is a slow day in history. No messages were found for today.", 
                        lTargetChannelID);
            return;
        }
        string? lWeightedChannelID = _dbh.GetWeightedChannelID();
        var lBestMsg = MOTDService.GetMotD(lMergedMessages, lWeightedChannelID ?? string.Empty);
        if(lBestMsg == null)
        {
            await SendChannelMessageAsync("No message found for today.", lTargetChannelID);
            return;
        }
        DiscordChannel lSourceChannel = await _discord.GetChannelAsync(ulong.Parse(lBestMsg.ChannelID));
        var lOriginalMsg = await lSourceChannel.GetMessageAsync(ulong.Parse(lBestMsg.MessageID));
        var lMotDFormat = MotDFormatter(lOriginalMsg, lBestMsg);

        await SendWebhookMessageAsync(lOriginalMsg, lTargetChannelID, lMotDFormat);                         
    }
    /// <summary>
    /// Groups messages from the same user within 7 minutes
    /// </summary>
    /// <param name="aMessages">A list of messages to be checked for similarity</param>
    /// <returns>A list of messages that have been merged based on timestamp and author</returns>
    private List<MessageRecord> MergeMultiPartMessages(List<MessageRecord> aMessages)
    {
        if (aMessages.Count == 0)
            return aMessages;
        List<MessageRecord> lSortedMessages = aMessages
            .OrderBy(m => m.Timestamp)
            .ToList();

        List<MessageRecord> lMergedMessages = new List<MessageRecord>();

        int i = 0;
        while(i < lSortedMessages.Count)
        {
            MessageRecord lStartNewMessage = lSortedMessages[i];
            List<MessageRecord> lGroup = new List<MessageRecord>();
            lGroup.Add(lStartNewMessage);

            int j = i + 1;
            while(j < lSortedMessages.Count)
            {
                MessageRecord lNextMessage = lSortedMessages[j];
                
                bool lSameAuthor = lStartNewMessage.AuthorID == lNextMessage.AuthorID;
                //current message is within 7 minutes of the first message
                bool lWithin7Minutes = (lNextMessage.Timestamp - lStartNewMessage.Timestamp).TotalMinutes <= 7; 
                
                if(!lSameAuthor || !lWithin7Minutes)
                    break;

                lGroup.Add(lNextMessage);
                j++;
            }

            lMergedMessages.Add(CollapseMessageContent(lGroup));
            i = j;            
        }

        return lMergedMessages;
    }
    /// <summary>
    /// Combines multiple messages into one
    /// </summary>
    /// <param name="aMessages">A list of messages from the same user within 7 minutes</param>
    /// <returns>A combined message</returns>
    private MessageRecord CollapseMessageContent(List<MessageRecord> aMessages)
    {
        MessageRecord lFirstMessage = aMessages[0];

        MessageRecord lNewMessage = new MessageRecord
        {
            Content = lFirstMessage.Content,
            AttachmentCount = lFirstMessage.AttachmentCount,
            ReactionCount = lFirstMessage.ReactionCount,
            Timestamp = lFirstMessage.Timestamp,
            MessageID = lFirstMessage.MessageID,
            GuildID = lFirstMessage.GuildID,
            ChannelID = lFirstMessage.ChannelID,
            AuthorID = lFirstMessage.AuthorID,
            Interestingness = lFirstMessage.Interestingness
        };
        for(int i = 1; i < aMessages.Count; i++)
        {
            lNewMessage.Content += "\n" + aMessages[i].Content;
            lNewMessage.AttachmentCount += aMessages[i].AttachmentCount;
            lNewMessage.ReactionCount += aMessages[i].ReactionCount;
        }        
        return lNewMessage;
    }
    public async Task<string> GetChannelSummaryAsync(DiscordChannel aChannel)
    {
        List<MessageRecord> lMessages =  _dbh.GetLast24HoursMsgs(DateTime.Now, aChannel.Id.ToString());

        StringBuilder lSB = new StringBuilder();
        foreach (var m in lMessages)
        {
            lSB.Append(' ').Append(m.Content.Replace("\n", " "));
        }
        
        string lPrompt = $"Summarize the following text. Format the summary with bullets or list items so it is easy to read "
            + $"and don't include a title, just the summary: {lSB.ToString()}";
        string lResponse = await _cohereClient.AskAsync(lPrompt);
     
        return lResponse;
    }
   
    public static (string, string, string?) MotDFormatter(DiscordMessage aMsg, MessageRecord aBestMsg)
    {
        string lUserName = $"\nOn this day in {aBestMsg.Timestamp.Year}";
        string lContent = $"------------------------------------------------\n" 
                + $"<@{aBestMsg.AuthorID}> said: \n"
                + $"{aBestMsg.Content} \n\n";
        string lFooter = $"------------------------------------------------\n" 
                + $"[view orignal message]({aMsg.JumpLink})";

        return (lUserName, lContent, lFooter);
    }
    public static async Task<(string, string, string?)> SultryResponse(string aMsg, DiscordUser aUser, DiscordGuild aGuild)
    {   
        DiscordMember lMember = await aGuild.GetMemberAsync(aUser.Id);
        string lUserNickName = lMember.Nickname ?? aUser.Username;
        string lUserName = $"\n{lUserNickName}' Sex Kitten:";
        string lContent = $"{aMsg}";
        string lFooter = string.Empty;

        return (lUserName, lContent, lFooter);
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
    /// <summary>
    /// Formats and sends a message using a webhook
    /// </summary>
    /// <param name="aMessage">The Original message</param>
    /// <param name="aBestMsg">Best message with grouped content</param>
    /// <param name="aChannelID"></param>
    /// <returns></returns>
    public async Task SendWebhookMessageAsync(DiscordMessage? aMessage, 
                        ulong aChannelID,
                        (string userName, string content, string? footer) aFormat)    {

        var lWebHook = await EnsureAvailableWebhookAsync(aChannelID);  
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
        
        await lWebHook.ExecuteAsync(lWebHookBuilder);

        if (!string.IsNullOrEmpty(aFormat.footer))
        {
            var lFooterBuilder = new DiscordWebhookBuilder()
                .WithUsername(aFormat.userName)
                .WithContent(aFormat.footer)
                .WithAvatarUrl(lBot.AvatarUrl);
            await lWebHook.ExecuteAsync(lFooterBuilder);
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