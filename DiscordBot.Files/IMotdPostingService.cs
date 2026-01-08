public interface IMotdPostingService
{
    Task<MessageRecord?> GetMotdAsync(DateTime aDate, ulong aGuildID);
    Task<bool> HasMotdBeenPostedAsync(DateTime aDate, ulong aGuildID);
    Task<DateTime> GetLastMotDDate(ulong aGuildID);
    Task<ulong> GetMotdChannelID(ulong aGuildID); 
    Task SetLastMotDDate(DateTime aDate, ulong aGuildID); 
    Task<List<ulong>> GetGuildsDueForMotdPostingAsync(DateTime aToday);
}