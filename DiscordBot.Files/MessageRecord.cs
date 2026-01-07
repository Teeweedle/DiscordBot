public class MessageRecord
{
    public string MessageID { get; set; } = string.Empty;
    public string GuildID { get; set; } = string.Empty;
    public string ChannelID { get; set; } = string.Empty;
    public string AuthorID { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> MessageIDAttachmentList { get; set; } = new List<string>();
    public int AttachmentCount { get; set; }
    public int ReactionCount { get; set; }
    public DateTime Timestamp { get; set; }
    public float Interestingness { get; set; }
}