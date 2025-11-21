using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;

public class BotService
{
    private readonly string _token;
    private readonly DiscordClient _discord;
    private readonly DatabaseHelper _dbh = new();
    private readonly ulong BotTestChannelID = 1428046909737533480;
    private static readonly Regex MediaLinkRegex = new Regex(
        @"https?:\/\/(?:[^\s]+?\.(?:gif|mp3|mp4|png|jpg|jpeg|webm)|(?:www\.)?(?:reddit\.com|v\.redd\.it|imgur\.com|gfycat\.com|tenor\.com|youtube\.com|youtu\.be)[^\s]*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public BotService(string aToken)
    {
        _token = aToken;
        _discord = new DiscordClient(new DiscordConfiguration
        {
            Token = _token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents
        });
    }
    public void RegisterCommands()
    {
        var lServices = new ServiceCollection()
            .AddSingleton<DatabaseHelper>(_dbh)
            .BuildServiceProvider();

        var slash = _discord.UseSlashCommands(new SlashCommandsConfiguration
        {
            Services = lServices
        });
        slash.RegisterCommands<MyCommands>();
    }
    public void RegisterEventHandler()
    {
        _discord.MessageCreated += async (s, e) =>
       {
           if (!e.Author.IsBot)
               SaveMessage(e.Message);
       };
    }

    public async Task RunAsync()
    {
        RegisterCommands();
        RegisterEventHandler();
        Console.WriteLine($"Today is: {DateOnly.FromDateTime(DateTime.Now)}");
        _discord.GuildDownloadCompleted += async (s, e) =>
        {
            try
            {
                Console.WriteLine("Bot is connected and ready.");
                Console.WriteLine($"Guild Count: {_discord.Guilds.Count}");
                await ScrapeAllGuilds(_discord);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Scrape Error] {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        };

        // Connect to Discord
        await _discord.ConnectAsync();

        // Keep the program running
        await Task.Delay(-1);
    }
    /// <summary>
    /// Calls DatabaseHelper.SaveMessage
    /// </summary>
    /// <param name="aMessage">A DiscordMessage</param>
    private void SaveMessage(DiscordMessage aMessage)
    {
        _dbh.SaveMessage(aMessage);
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
                    if (!m.Author.IsBot && m.Content.Length > 3)
                    {
                        SaveMessage(m);
                    }
                }
    
                var lastMessage = lMessages.Last();
                lMessages = await aChannel.GetMessagesBeforeAsync(lastMessage.Id, 100);
    
                await Task.Delay(500);
            }
        }
        catch (DSharpPlus.Exceptions.UnauthorizedException)
        {
            Console.WriteLine($"No access to #{aChannel.Name}");
        }
    }
    private async Task ScrapeAllGuilds(DiscordClient aDiscord)
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
                    await ScrapeChannelAsync(channel);
                }
            }
        }
    }
    public async Task PostMotDAsync()
    {
        Console.WriteLine("Posting MOTD...");
        var MOTDService = new OnThisDayService();

        List<MessageRecord> lMessages = _dbh.GetTodaysMsgs(DateTime.UtcNow.Date);
        var lMotdID = _dbh.GetMoTDChannelID();
        if(lMotdID == null) //LEFT OFF HERE, handle if no channel
        {
            var lmsgBuilder = new DiscordMessageBuilder().
                WithContent("No MoTD channel set.");
            await _discord.GetChannelAsync(BotTestChannelID).Result.SendMessageAsync(lmsgBuilder);
            return;
        }
        DiscordChannel lChannel = await _discord.GetChannelAsync(ulong.Parse(lMotdID));
        
        var lBestMsg = MOTDService.GetMotD(lMessages, _dbh.GetWeightedChannelID()!);

        if (lBestMsg != null)
        {
            if(lChannel is DiscordChannel)
            {                
                await lChannel.SendMessageAsync(FormatMessage(lBestMsg));    
            }            
        }
    }
    private DiscordEmbed FormatMessage(MessageRecord aMessage)
    {
        string lMessageURL = $"https://discord.com/channels/{aMessage.GuildID}/{aMessage.ChannelID}/{aMessage.MessageID}";
        var lMediaLink = MediaLinkRegex.Match(aMessage.Content).ToString();
        var lRemoveLinkMsg = aMessage.Content.Replace(lMediaLink, "");

        var embed = new DiscordEmbedBuilder()
            .WithTitle($"On this day in {aMessage.Timestamp.Year}")
            .WithDescription($"<@{aMessage.AuthorID}> said: {lRemoveLinkMsg}")            
            .WithColor(DiscordColor.Red);
        //TODO: Add media link for link embeding
        if (lMediaLink != null)
            embed.WithUrl(lMediaLink);                    
            // embed.WithImageUrl(lMediaLink);
        
        embed.AddField("\u200B", $"[view orignal message]({lMessageURL})", inline:true);
        return embed.Build();
    }
}