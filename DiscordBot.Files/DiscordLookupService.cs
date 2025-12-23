using DSharpPlus;
using DSharpPlus.Entities;

public sealed class DiscordLookupService
{
    private readonly DiscordClient _discord;

    public DiscordLookupService(DiscordClient aDiscordClient)
    {
        _discord = aDiscordClient;
    }
        public async Task<string> GetDiscordChannelAsync(ulong aDiscordID)
    {
        DiscordChannel lChannel = await _discord.GetChannelAsync(aDiscordID);
        return lChannel.Name; 
    }
    public async Task<string> GetDiscordUserAsync(ulong aUserID)
    {
        DiscordUser lUser = await _discord.GetUserAsync(aUserID);
        return lUser.Username;
    }
}