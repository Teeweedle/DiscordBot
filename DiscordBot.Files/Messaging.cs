using System.Text;
using DSharpPlus.Entities;

public class Messaging
{
    private static readonly int RespondDelay = 2;
    private readonly Dictionary<ulong, DateTime> _lastBotRespond = new();
    private readonly MessagingService _messagingService;
    private readonly ulong BotTestChannelID = 1445164447093227633;
    public Messaging(MessagingService aMessagingService)
    {
        _messagingService = aMessagingService;
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
        // if(_lastBotRespond.TryGetValue(aUser.Id, out var last) &&
        //     DateTime.UtcNow - last < TimeSpan.FromMinutes(RespondDelay))
        // {
        //     Console.WriteLine("Respond Delay");
        //     return false;
        // }
            
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
        DiscordGuild lGuild = await _messagingService.GetDiscordGuild(aChannel.GuildId.Value);
        CohereClient lCohere = _messagingService.GetCohereClient();

        string lPrompt = $"Respond to the following message as if you are another user in Discord but do it " 
            + $"in a playful manner. The reponse should be funny, helpful, and informative "
            + $"(maybe a little sarcastic and flirty): {aMessage.Content}";
        string lResponse = await lCohere.AskAsync(lPrompt);
        var lSultryFormat = await SultryResponse(lResponse, aUser, lGuild);
        _lastBotRespond[aUser.Id] = DateTime.UtcNow;

        await _messagingService.SendWebhookMessageAsync(null, aChannel.Id, lSultryFormat);
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

        List<MessageRecord> lMessages = _messagingService.GetTodaysMsgs();
        List<MessageRecord> lMergedMessages = MergeMultiPartMessages(lMessages);
        ulong lTargetChannelID;
        if (aChannelID == BotTestChannelID)
        {
            lTargetChannelID = aChannelID;
        }else{
            string? lMotdID = _messagingService.GetMoTDChannelID()!;
            if (string.IsNullOrEmpty(lMotdID))
            {
                await _messagingService.SendChannelMessageAsync("No MoTD channel set.", aChannelID);
                return;
            }
            lTargetChannelID = ulong.Parse(lMotdID); 
        } 
        
        if (lMergedMessages.Count == 0)
        {
            await _messagingService.SendChannelMessageAsync("Today is a slow day in history. No messages were found for today.", 
                        lTargetChannelID);
            return;
        }
        string? lWeightedChannelID = _messagingService.GetWeightedChannelID();
        var lBestMsg = MOTDService.GetMotD(lMergedMessages, lWeightedChannelID ?? string.Empty);
        if(lBestMsg == null)
        {
            await _messagingService.SendChannelMessageAsync("No message found for today.", lTargetChannelID);
            return;
        }
        DiscordChannel lSourceChannel = await _messagingService.GetSourceChannel(ulong.Parse(lBestMsg.ChannelID));
        var lOriginalMsg = await lSourceChannel.GetMessageAsync(ulong.Parse(lBestMsg.MessageID));
        var lMotDFormat = MotDFormatter(lOriginalMsg, lBestMsg);

        await _messagingService.SendWebhookMessageAsync(lOriginalMsg, lTargetChannelID, lMotDFormat);                         
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
        List<MessageRecord> lMessages =  _messagingService.GetLast24HoursMsgs();

        CohereClient lCohere = _messagingService.GetCohereClient();

        StringBuilder lSB = new StringBuilder();
        foreach (var m in lMessages)
        {
            lSB.Append(' ').Append(m.Content.Replace("\n", " "));
        }
        
        string lPrompt = $"Summarize the following text. Format the summary with bullets or list items so it is easy to read "
            + $"and don't include a title, just the summary: {lSB.ToString()}";
        string lResponse = await lCohere.AskAsync(lPrompt);
     
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
}