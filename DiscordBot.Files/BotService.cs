using System.Runtime.InteropServices;
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
    private static readonly HttpClient _httpClient = new HttpClient();

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
    public async Task<DiscordWebhook> EnsureAvailableWebhookAsync(ulong aChannelID)
    {
        DiscordChannel lChannel = await _discord.GetChannelAsync(aChannelID);

        string? lWebHookID = _dbh.GetWebHookID(lChannel.GuildId!.Value.ToString(), aChannelID.ToString());
        string? lWebHookToken = _dbh.GetWebHookToken(lChannel.GuildId!.Value.ToString(), aChannelID.ToString());

        if (lWebHookID != null && lWebHookToken != null)
        {
            return await _discord.GetWebhookWithTokenAsync(ulong.Parse(lWebHookID!), lWebHookToken!);
        }

        var lExistingWebhooks = await lChannel.GetWebhooksAsync();
        var lWebHook = lExistingWebhooks.FirstOrDefault(x => x.Name == "OnThisDayWebhook");
        
        if (lWebHook == null)
        {
            lWebHook = await lChannel.CreateWebhookAsync("OnThisDayWebhook");

            if (lWebHook == null)
            {
                throw new Exception("Failed to create webhook., bot does not have permissions.");
            }
        }
        
        _dbh.SaveWebHook(lChannel.GuildId!.Value.ToString(), aChannelID.ToString(), lWebHook.Id.ToString(), lWebHook.Token!);
        return await _discord.GetWebhookWithTokenAsync(lWebHook.Id, lWebHook.Token!);
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

        if (lMessages.Count == 0)
        {
            await SendMessage("Today is a slow day in history. no messages were found for today.");
            return;
        }
        var lMotdID = _dbh.GetMoTDChannelID();
        if(lMotdID == null) //LEFT OFF HERE, handle if no channel
        {            
            await SendMessage("No MoTD channel set.");
            return;
        }
        DiscordChannel lChannel = await _discord.GetChannelAsync(ulong.Parse(lMotdID));
        
    

        var lBestMsg = MOTDService.GetMotD(lMessages, _dbh.GetWeightedChannelID()!);
        var lSourceChannel = await _discord.GetChannelAsync(ulong.Parse(lBestMsg.ChannelID));
        var lOriginalMsg = await lSourceChannel.GetMessageAsync(ulong.Parse(lBestMsg.MessageID));

        if (lBestMsg != null)
        {
            if(lChannel is DiscordChannel)
            {                
                await SendMessage(lOriginalMsg);    
            }            
        }
    }
    /// <summary>
    /// Formats and sends a message using a webhook
    /// </summary>
    /// <param name="aMessage">The message to send</param>
    /// <returns></returns>
    private async Task SendMessage(DiscordMessage aMessage)
    {
        var lWebHook = await EnsureAvailableWebhookAsync(aMessage.ChannelId);        

        var lWebHookBuilder = new DiscordWebhookBuilder()
            .WithUsername($"On this day in {aMessage.CreationTimestamp.Year}")
            .WithContent($"<@{aMessage.Author.Id}> said: \n\n"+
                $"{aMessage.Content} \n\n");
            
        foreach (var attachment in aMessage.Attachments)
        {
            var lStream = await _httpClient.GetStreamAsync(attachment.Url);
            lWebHookBuilder.AddFile(attachment.FileName, lStream);
        }
        await lWebHook.ExecuteAsync(lWebHookBuilder);

        var lFooter = new DiscordWebhookBuilder()
            .WithUsername($"On this day in {aMessage.CreationTimestamp.Year}")
            .WithContent($"[view orignal message]({aMessage.JumpLink})");
        await lWebHook.ExecuteAsync(lFooter);
    }
    private async Task SendMessage(string aMessage)
    {
        var lmsgBuilder = new DiscordMessageBuilder();
        lmsgBuilder.WithContent(aMessage);
        await _discord.GetChannelAsync(BotTestChannelID).Result.SendMessageAsync(lmsgBuilder);
    }
}