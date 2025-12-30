using DSharpPlus;
using DSharpPlus.Entities;


public sealed class DiscordLookupService
{
    private readonly DiscordClient _discord;

    public DiscordLookupService(DiscordClient aDiscordClient)
    {
        _discord = aDiscordClient;
    }
    public async Task<string> GetDiscordChannelAsync(ulong aChannelID)
    {
        DiscordChannel lChannel = await _discord.GetChannelAsync(aChannelID);
        return lChannel.Name; 
    }
    public async Task<string> GetDiscordUserAsync(ulong aUserID)
    {
        DiscordUser lUser = await _discord.GetUserAsync(aUserID);
        return lUser.Username;
    }
    public async Task<DateTime> GetLastMOTDDateAsync(ulong aMOTDChannelID)
    {
        DiscordChannel lChannel = await _discord.GetChannelAsync(aMOTDChannelID);
        
        var lLastMessages = await lChannel.GetMessagesAsync(1);
        var lLastMessage = lLastMessages.FirstOrDefault();

        if (lLastMessage == null) 
            return DateTime.MinValue;

        DateTime lLastMessageDate = lLastMessage.Timestamp.DateTime;
        return lLastMessageDate;
    }
}