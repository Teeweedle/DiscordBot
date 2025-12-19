using DSharpPlus;

public interface IChannelScraper
{
    Task ScrapeAllGuilds(DiscordClient aDiscord);
}