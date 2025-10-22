public class MessageRecord
{
    public string MessageID { get; set; }
    public string ChannelID { get; set; }
    public string AuthorID { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
    public float Interestingness { get; set; }
}