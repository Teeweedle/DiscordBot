using System.Runtime.CompilerServices;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.VisualBasic;

public class ChannelScraper : IChannelScraper
{
    private readonly DiscordClient _discord;
    private readonly DatabaseHelper _dbh;
    private Dictionary<ulong, DateTime> _lastChannelSrape = new();
    private Dictionary<ulong, bool> _fullyScraped = new();
    private Dictionary<ulong, string?> _lastMsgID = new();
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
                if(channel.Type != ChannelType.Text)
                    continue;

                var(fullyScraped, lastMsgID) = GetChannelFullyScrapped(guild.Id, channel.Id);
                _fullyScraped[channel.Id] = fullyScraped;
                _lastMsgID[channel.Id] = lastMsgID;

                if(fullyScraped)
                    _lastChannelSrape[channel.Id] = DateTime.UtcNow;

                _ = ScrapeLoopAsync(channel);
            }
        }
    }
    private async Task ScrapeChannelAsync(DiscordChannel aChannel)
    {
        try
        {
            Console.WriteLine($"Scraping #{aChannel.Name} in {aChannel.Guild.Name}...");
            if (!_fullyScraped[aChannel.Id])
            {
                await FullScrapeChannelAsync(aChannel);
                return;
            }else
            {
                await TailScrapeChannelAsync(aChannel);
            }
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
            await Task.Delay(TimeSpan.FromMinutes(ScrapeDelay));
        }
    }
    private async Task FullScrapeChannelAsync(DiscordChannel aChannel)
    {
        var lMessages = await aChannel.GetMessagesAsync(100);

        while (lMessages.Count > 0)
        {
            foreach (var m in lMessages)
            {
                if (!m.Author.IsBot)
                    SaveMessage(m);
            }
            var lLastMessage = lMessages.Last();
            lMessages = await aChannel.GetMessagesBeforeAsync(lLastMessage.Id, 100);

            await Task.Delay(500);
        }

        _fullyScraped[aChannel.Id] = true;
        SetChannelScrapeState(aChannel.Guild.Id, aChannel.Id, true, null);

        _lastChannelSrape[aChannel.Id] = DateTime.UtcNow;
    }
    private async Task TailScrapeChannelAsync(DiscordChannel aChannel)
    {
        var lMessages = await aChannel.GetMessagesAsync(100);
        foreach (var m in lMessages)
        {
            if (!m.Author.IsBot)
                SaveMessage(m);
        }

        _lastChannelSrape[aChannel.Id] = DateTime.UtcNow;
    }
    private bool CanScrape(DiscordChannel aChannel)
    {
        if(!_fullyScraped.TryGetValue(aChannel.Id, out var fullyScraped) || !fullyScraped)
            return true;

        if(!_lastChannelSrape.TryGetValue(aChannel.Id, out var last))
            return true;

        return DateTime.UtcNow - last >= TimeSpan.FromHours(ScrapeDelay);
    }
    private void SetChannelScrapeState(ulong aGuildID, ulong aChannelID, bool aFullyScraped, string aLastMessageID)
    {
        _dbh.SetChannelScrapeState(aGuildID, aChannelID, aFullyScraped, aLastMessageID);
    }
    private (bool FullyScraped, string? LastMessageID) GetChannelFullyScrapped(ulong aGuildID, ulong aChannelID)
    {
        return _dbh.GetChannelScrapeState(aGuildID, aChannelID);
    }
    // /// <summary>
    // /// Calls DatabaseHelper.SaveMessage
    // /// </summary>
    // /// <param name="aMessage">A DiscordMessage</param>
    private void SaveMessage(DiscordMessage aMessage)
    {
        _dbh.SaveMessage(aMessage);
    }
    private void LoadLastScanned()
    {
        _lastChannelSrape = _dbh.GetLastScanned();
    }
}