using System.Text.RegularExpressions;

public class OnThisDay
{
    private const float AttachmentMultiplier = 2.5f;
    private const float WordCountMultiplier = 0.35f;
    private const float MentionsUserMultiplier = 0.3f;
    private const float ReactionCountMultiplier = 0.25f;
    private const float weightedChannelMultiplier = 3.5f;    
    private static readonly Regex MediaLinkRegex = new Regex(
        @"https?:\/\/(?:[^\s]+?\.(?:gif|mp3|mp4|png|jpg|jpeg|webm)|(?:www\.)?(?:reddit\.com|v\.redd\.it|imgur\.com|gfycat\.com|tenor\.com|youtube\.com|youtu\.be)[^\s]*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex MentionsUserRegex = new Regex(
        @"<@!?(\d+)>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );
    public static readonly Regex WordCountRegex = new Regex(
        @"(?<!\S)(?!https?://|www\.|<@\d+>)[a-zA-Z]+(?:'[a-zA-Z]+)?(?!\.\w{2,6})(?!\S)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );
   
    private List<MessageRecord> _messages = new List<MessageRecord>();
    /// <summary>
    /// Constructor, accepts a list of MessageRecords to process
    /// </summary>
    /// <param name="aMessages">A list of MessageRecords</param>
    public OnThisDay(List<MessageRecord> aMessages) => _messages = aMessages;
    public void GenerateInterestingness(string? aWeightedChannelID)
    {
        foreach (var m in _messages)
        {
            m.Interestingness =
                ContentWordCount(m.Content) +
                HasAttachment(m.AttachmentCount, m.Content) +
                ReactionCount(m.ReactionCount) +
                MentionsUser(m.Content);

            if(aWeightedChannelID != null && m.ChannelID == aWeightedChannelID) 
                m.Interestingness *= weightedChannelMultiplier;
        }
    }
    public float ContentWordCount(string aContent)
    {
        int lWordCount = WordCountRegex.Matches(aContent ?? string.Empty).Count;
        return lWordCount * WordCountMultiplier;
    }
    public float HasAttachment(int aAttachmentCount, string aContent)
    {
        int lLinkCount = MediaLinkRegex.Matches(aContent ?? string.Empty).Count;
        return (aAttachmentCount + lLinkCount) * AttachmentMultiplier;
    }
    public float MentionsUser(string aContent)
    {
        int lMentions = MentionsUserRegex.Matches(aContent ?? string.Empty).Count;
        return lMentions * MentionsUserMultiplier;
    }
    public float ReactionCount(int aReactionCount) => aReactionCount * ReactionCountMultiplier;
}