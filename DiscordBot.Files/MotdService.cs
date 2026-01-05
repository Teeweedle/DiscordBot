public sealed class MotdService //: IMotdPostingService
{
    private readonly DatabaseHelper _dbh;
    private readonly DiscordLookupService _lookup;
    public MotdService(DatabaseHelper aDb, DiscordLookupService aLookup)
    {
        _dbh = aDb;
        _lookup = aLookup;
    }
    /// <summary>
    /// Returns the MessageRecord with the highest interestingness
    /// </summary>
    /// <param name="aMessages">A list of messages from this day any year</param>
    /// <returns></returns>
    public MessageRecord GetMotD(List<MessageRecord> aMessages, string aWeightedChannelID) { 
        
        var lMOTD = new OnThisDay(aMessages);
        lMOTD.GenerateInterestingness(aWeightedChannelID);
        Console.WriteLine($"Posting MOTD... ");   

        var lBestMsg = aMessages
            .OrderByDescending(m => m.Interestingness)
            .FirstOrDefault();
        Console.WriteLine($"Best Interestingness message - {lBestMsg!.Interestingness} \n" +
                            $"The message is - {lBestMsg.Content}\n" +
                            $"The attachment count is - {lBestMsg.AttachmentCount}");
        return lBestMsg;
    }
    public async Task<MessageRecord?> GetMotdAsync(DateTime aDateUTC, ulong aGuildID)
    {
        List<MessageRecord> lMessages = _dbh.GetTodaysMsgs(DateTime.UtcNow.Date, aGuildID.ToString());
        List<MessageRecord> lMergedMessages = MergeMultiPartMessages(lMessages);

        if(lMergedMessages.Count == 0) return null;

        string? lWeightedChannelID = _dbh.GetWeightedChannelID(aGuildID.ToString())!;
        var lBestMsg = GetMotD(lMergedMessages, lWeightedChannelID ?? string.Empty);

        if(lBestMsg == null) return null;

        return lBestMsg;
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
                //current message is within 7 minutes of the first message (discord limit)
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
            AttachmentUrlList = new List<string>(lFirstMessage.AttachmentUrlList),
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

            foreach(string url in aMessages[i].AttachmentUrlList) 
                lNewMessage.AttachmentUrlList.Add(url);
        }        
        return lNewMessage;
    }
    /// <summary>
    /// Checks if a MOTD has been posted within the last 24 hours for the specified motd channel.
    /// </summary>
    /// <param name="aDateUTC">The date to check for the MOTD.</param>
    /// <returns>True if a MOTD has been posted within the last 24 hours, false otherwise.</returns>
    public async Task<bool> HasMotdBeenPostedAsync(DateTime aDateUTC, string aGuildID)
    {
        string? lMotdChannelIDString = _dbh.GetMotdChannelID(aGuildID);
        if(string.IsNullOrEmpty(lMotdChannelIDString)) 
            return false;

        ulong lMotdChannelID = ulong.Parse(lMotdChannelIDString);
        DateTime lLastMotdDate = await _lookup.GetLastMOTDDateAsync(lMotdChannelID); 

        return DateTime.UtcNow - lLastMotdDate <= TimeSpan.FromDays(1);
    }
    // public async Task<DateTime> GetLastMotDDate() => await _lookup.GetLastMOTDDateAsync(ulong.Parse(_dbh.GetMotdChannelID() ?? string.Empty));
    // public async Task<ulong> GetMotdChannelID() => ulong.Parse(_dbh.GetMotdChannelID() ?? string.Empty);
}