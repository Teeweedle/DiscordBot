using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

public class ChannelScraper : IChannelScraper
{
    private readonly DiscordClient _discord;
    private readonly DatabaseHelper _dbh;
    private Dictionary<ulong, DateTime> _lastChannelSrape = new();
    private Dictionary<ulong, bool> _fullyScraped = new();
    private Dictionary<ulong, string?> _lastMsgID = new();
    private static readonly int ScrapeDelay = 5;
    private readonly ILogger<ChannelScraper> _logger;

    public ChannelScraper(DiscordClient aDiscord, 
                            DatabaseHelper aDb,
                            ILogger<ChannelScraper> aLogger)
    {
        _discord = aDiscord;
        _dbh = aDb;
        _logger = aLogger;
        _discord.GuildDownloadCompleted += OnGuildDownloadCompleted;    
    }
    private async Task OnGuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Bot is connected and ready.");
            await ScrapeAllGuilds(_discord);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnGuildDownloadCompleted");
            throw;
        }
    }
    public async Task ScrapeAllGuilds(DiscordClient aDiscord)
    {
        foreach (var guild in aDiscord.Guilds.Values)
        {
            _logger.LogInformation($"Guild Count: {aDiscord.Guilds.Count}");
            _logger.LogInformation($"Scraping {guild.Name}...");

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
            var lbotMember = await aChannel.Guild.GetMemberAsync(_discord.CurrentUser.Id);
            var lChannelPermissions = aChannel.PermissionsFor(lbotMember);
            if(!lChannelPermissions.HasPermission(Permissions.ReadMessageHistory) || !lChannelPermissions.HasPermission(Permissions.AccessChannels))
            {
                _logger.LogDebug($"Bot does not have permission to scrape #{aChannel.Name} in {aChannel.Guild.Name}...");
                 return;
            }   
            if (!_fullyScraped[aChannel.Id])
            {
                await FullScrapeChannelAsync(aChannel);
                return;
            }else
            {
                await TailScrapeChannelAsync(aChannel);
            }
        }
        catch(DSharpPlus.Exceptions.UnauthorizedException ex)
        {
            _logger.LogDebug("UnauthorizedException in ScrapeChannelAsync{ChannelID} in guild {GuildID}", 
                            aChannel.Id, aChannel.Guild.Id);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ScrapeChannelAsync");
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
}