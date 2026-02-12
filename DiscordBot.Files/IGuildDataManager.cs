public interface IGuildDataManager
{
    Task PurgeGuildDataAsync(ulong aGuildID);
    Task PurgeUserDataAsync(ulong aUserID, ulong aGuildID);
}