public interface IMotdPostingService
{
    Task<MessageRecord?> GetMotdAsync(DateTime aDate);
    Task<bool> HasMotdBeenPostedAsync(DateTime aDate);
    Task<DateTime> GetLastMotDDate();
    Task<ulong> GetMotdChannelID();  
}