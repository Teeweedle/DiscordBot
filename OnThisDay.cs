
using System.Text.RegularExpressions;

public class OnThisDay
{
    private const float AttachmentMultiplier = 3.0f;
    private const float WordCountMultiplier = 0.5f;
    private const float MentionsUserMultiplier = 0.5f;
    private static readonly Regex MediaLinkRegex = new Regex(
    @"https?:\/\/[^\s]+?\.(?:gif|mp3|mp4|png|jpg|jpeg|webm)",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
    );
    private static readonly Regex MentionsUserRegex = new Regex(
    @"<@!?(\d+)>",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
    );
    private List<MessageRecord> _messages = new List<MessageRecord>();
    /// <summary>
    /// Constructor, accepts a list of MessageRecords to process
    /// </summary>
    /// <param name="aMessages">A list of MessageRecords</param>
    public OnThisDay(List<MessageRecord> aMessages) => _messages = aMessages;
    public void GenerateInterestingness()
    {
        float lInterestingness;
        foreach (var message in _messages)
        {
            lInterestingness = 0.0f;
            lInterestingness += ContentWordCount(message.Content);
            lInterestingness += HasAttachment(message.AttachmentCount, message.Content);
            lInterestingness += MentionsUser(message.Content);
            message.Interestingness = lInterestingness;
        }
    }
    public float ContentWordCount(string aContent)
    {
        int lWordCount = Regex.Matches(aContent, @"\b[\w'-]+\b").Count;
        return lWordCount * WordCountMultiplier;
    }
    public float HasAttachment(int aAttachmentCount, string aContent)
    {
        int lAttachmentCount = MediaLinkRegex.Matches(aContent ?? string.Empty).Count;
        return (aAttachmentCount + lAttachmentCount) * AttachmentMultiplier;
    }
    public float MentionsUser(string aContent){
        int lMentions = MentionsUserRegex.Matches(aContent ?? string.Empty).Count;
        return lMentions * MentionsUserMultiplier;
    }
}