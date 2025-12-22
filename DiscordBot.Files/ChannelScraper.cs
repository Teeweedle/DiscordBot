using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

public class ChannelScraper : IChannelScraper
{
    private readonly DiscordClient _discord;
    private readonly DatabaseHelper _dbh;
    private readonly Dictionary<ulong, DateTime> _lastChannelSrape = new();
    private static readonly int ScrapeDelay = 5;

    public ChannelScraper(DiscordClient aDiscord, DatabaseHelper aDb)
    {
        _discord = aDiscord;
        _dbh = aDb;

        _discord.GuildDownloadCompleted += OnGuildDownloadCompleted;    
    }
    private async Task OnGuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs e)
    {
        try
        {
            Console.WriteLine("Bot is connected and ready.");
            await ScrapeAllGuilds(_discord);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Scrape Error] {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }
    public async Task ScrapeAllGuilds(DiscordClient aDiscord)
    {
        foreach (var guild in aDiscord.Guilds.Values)
        {
            Console.WriteLine($"Channel Count: {guild.Channels.Count}");
            Console.WriteLine($"{guild.Name}");
            foreach (var channel in guild.Channels.Values)
            {
                if (channel.Type == ChannelType.Text)
                {
                    Console.WriteLine($"Scraping #{channel.Name} in {guild.Name}...");
                    _ = ScrapeLoopAsync(channel);
                }
            }
        }
    }
    private async Task ScrapeChannelAsync(DiscordChannel aChannel)
    {
        try
        {
            var lMessages = await aChannel.GetMessagesAsync(100);
            while (lMessages.Count > 0)
            {
                foreach (var m in lMessages)
                {
                    if (!m.Author.IsBot)
                    {
                        if (m.Content.Length > 3 || m.Attachments.Count > 0)
                        {
                            SaveMessage(m);
                        }
                    }                    
                }
    
                var lastMessage = lMessages.Last();
                lMessages = await aChannel.GetMessagesBeforeAsync(lastMessage.Id, 100);
    
                await Task.Delay(500);
            }
            
            _lastChannelSrape[aChannel.Id] = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ScrapeChannelAsync: {aChannel.Name}: {ex.Message}");
        }
    }
    private async Task ScrapeLoopAsync(DiscordChannel aChannel)
    {
        while (true)
        {
            if (CanScrape(aChannel))
            {
                await ScrapeChannelAsync(aChannel);
            }
            await Task.Delay(TimeSpan.FromMinutes(5));
        }
    }
    private bool CanScrape(DiscordChannel aChannel)
    {
        if(!_lastChannelSrape.TryGetValue(aChannel.Id, out var last))
            return true;
        return DateTime.UtcNow - last >= TimeSpan.FromHours(ScrapeDelay);
    }
    // /// <summary>
    // /// Calls DatabaseHelper.SaveMessage
    // /// </summary>
    // /// <param name="aMessage">A DiscordMessage</param>
    private void SaveMessage(DiscordMessage aMessage)
    {
        _dbh.SaveMessage(aMessage);
    }
}