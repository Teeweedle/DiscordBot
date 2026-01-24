public interface IMotdService
{
    Task PurgeGuildMotdSettingsAsync(ulong aGuildID);
    Task<MessageRecord?> GetMotdAsync(DateTime aDateUTC, ulong aGuildID);
    Task SendMotdAsync(MessageRecord aMessageRecord, ulong aChannelID);
    Task<bool> HasMotdBeenPostedAsync(DateTime aDateUTC, ulong aGuildID);
}