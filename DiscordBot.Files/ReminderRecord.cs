public class ReminderRecord
{
    public ulong UserID { get; set; }
    public ulong GuildID { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ExpirationDate { get; set; }
    public ulong InteractionID { get; set; }
}