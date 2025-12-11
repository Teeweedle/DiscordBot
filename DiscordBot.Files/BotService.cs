using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

public class BotService
{
    private readonly string _token;
    private readonly DiscordClient _discord;
    private readonly DatabaseHelper _dbh;
    private readonly MessagingService _messagingService;
    private readonly Messaging _messaging;
    private readonly CohereClient _cohereClient;
    private readonly string _cohereKey;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<ulong, DateTime> _lastChannelSrape = new();
    private static readonly int ScrapeDelay = 5;

    public BotService(string aToken)
    {        
        _dbh = new DatabaseHelper();
        _httpClient = new HttpClient();
        _token = aToken;
        _discord = new DiscordClient(new DiscordConfiguration
        {
            Token = _token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents
        });
        IConfigurationBuilder lBuilder = new ConfigurationBuilder().AddUserSecrets<Program>();
        IConfiguration lConfig = lBuilder.Build();
        _cohereKey = lConfig["COHERE_API_KEY"] ?? throw new Exception("COHERE_API_KEY is not set");
        _cohereClient = new CohereClient(_cohereKey);
        _messagingService = new MessagingService(_discord, _httpClient, _dbh, _cohereClient);
        _messaging = new Messaging(_messagingService);
    }
    public void RegisterCommands()
    {
        var lServices = new ServiceCollection()
            .AddSingleton<DatabaseHelper>(_dbh)
            .AddSingleton<Messaging>(_messaging)
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
            if(string.Equals(e.Author.Id.ToString(), _dbh.GetTargetUserID(), StringComparison.Ordinal) &&
                string.Equals(e.Channel.Id.ToString(), _dbh.GetTargetChannelID(), StringComparison.Ordinal))
            {
                await _messaging.RespondToUser(e.Message, e.Channel, e.Author);
            }                
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
    // /// <summary>
    // /// Calls DatabaseHelper.SaveMessage
    // /// </summary>
    // /// <param name="aMessage">A DiscordMessage</param>
    private void SaveMessage(DiscordMessage aMessage)
    {
        _dbh.SaveMessage(aMessage);
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
    public Task PostMotDAsync(bool testMode) => _messaging.PostMotDAsync(testMode);
    public Task PostChannelSummaryAsync(bool testMode) => _messaging.PostChannelSummaryAsync(testMode);    
}