using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

public class BotService
{
    private readonly string _token;
    private readonly DiscordClient _discord;
    private readonly DatabaseHelper _db = new();

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
        var slash = _discord.UseSlashCommands();
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
    //TODO: Move to DBHelper
    private void SaveMessage(DiscordMessage aMessage)
    {
        _db.SaveMessage(
            aMessage.Id.ToString(),
            aMessage.Channel.Id.ToString(),
            aMessage.Author.Id.ToString(),
            aMessage.Content,
            aMessage.CreationTimestamp.UtcDateTime
        );
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
        foreach(var guild in aDiscord.Guilds.Values)
        {
            Console.WriteLine($"Channel Count: {guild.Channels.Count}");
            Console.WriteLine($"{guild.Name}");
            foreach(var channel in guild.Channels.Values)
            {
                if(channel.Type == ChannelType.Text)
                {
                    Console.WriteLine($"Scraping #{channel.Name} in {guild.Name}...");
                    await ScrapeChannelAsync(channel);
                }
            }
        }
    }
}