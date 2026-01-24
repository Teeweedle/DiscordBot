public interface IMessagingService
{
    Task PurgeGuildMessagesAsync(ulong aGuildID);
    Task PurgeGuildWebHooksAsync(ulong aGuildID);
    Task PurgeGuildTargetUserAndChannelsAsync(ulong aGuildID);
    Task SendMissingMotdChannelAsync(ulong aGuildID);
    Task SendNoMotdFoundAsync(ulong aGuildID);
    Task PostMotdAsync(MessageRecord aBestMsg, ulong aChannelID);
}