using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

public class BotService
{
    private readonly string _token;
    private readonly DiscordClient _discord;
    private readonly DatabaseHelper _dbh = new();
    private readonly string _cohereKey;
    private readonly ulong BotTestChannelID = 1445164447093227633;
    private readonly ulong BotID = 1428047784245854310;
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly Dictionary<ulong, DateTime> _lastChannelSrape = new();
    private readonly Dictionary<ulong, DateTime> _lastBotRespond = new();
    private static readonly int ScrapeDelay = 5;
    private static readonly int RespondDelay = 2;

    public BotService(string aToken)
    {
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
            if(string.Equals(e.Author.Id.ToString(), _dbh.GetTargetUserID(), StringComparison.Ordinal) &&
                string.Equals(e.Channel.Id.ToString(), _dbh.GetTargetChannelID(), StringComparison.Ordinal))
            {
                await RespondToUser(e.Message, e.Channel, e.Author);
            }                
       };
    }
    public async Task<DiscordWebhook> EnsureAvailableWebhookAsync(ulong aChannelID)
    {
        DiscordChannel lChannel = await _discord.GetChannelAsync(aChannelID);

        string? lWebHookID = _dbh.GetWebHookID(lChannel.GuildId!.Value.ToString(), aChannelID.ToString());
        string? lWebHookToken = _dbh.GetWebHookToken(lChannel.GuildId!.Value.ToString(), aChannelID.ToString());

        if (!string.IsNullOrEmpty(lWebHookID) && !string.IsNullOrEmpty(lWebHookToken))
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
        
        _dbh.SaveWebHook(
            lChannel.GuildId!.Value.ToString(), 
            aChannelID.ToString(), 
            lWebHook.Id.ToString(), 
            lWebHook.Token!);
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
                // Console.WriteLine($"Guild Count: {_discord.Guilds.Count}");
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
            
            _lastChannelSrape[aChannel.Id] = DateTime.UtcNow;
        }
        catch (DSharpPlus.Exceptions.UnauthorizedException)
        {
            Console.WriteLine($"No access to #{aChannel.Name}");
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
    private bool CanRespond(DiscordUser aUser, DiscordMessage aMessage)
    {
        if(_lastBotRespond.TryGetValue(aUser.Id, out var last) &&
            DateTime.UtcNow - last < TimeSpan.FromMinutes(RespondDelay))
        {
            Console.WriteLine("Respond Delay");
            return false;
        }
            
        //No empty messages
        if (string.IsNullOrWhiteSpace(aMessage.Content))
        {
            Console.WriteLine("Empty message");
            return false;
        }
            
        //No URLs
        if(Uri.IsWellFormedUriString(aMessage.Content.Trim(), UriKind.Absolute))
        {
            Console.WriteLine("No URL");
            return false;
        }
            
        //No http links
        if(aMessage.Content.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("No http link");
            return false;
        }
            
        return true;
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
    public async Task PostMotDAsync(bool testMode = false)
    {
        Console.WriteLine($"Posting MOTD... (Test Mode: {testMode})");
        var MOTDService = new OnThisDayService();
        ulong lChannelID;

        List<MessageRecord> lMessages = _dbh.GetTodaysMsgs(DateTime.UtcNow.Date);
        List<MessageRecord> lMergedMessages = MergeMultiPartMessages(lMessages);
        if (testMode)
        {
            lChannelID = BotTestChannelID;
        }
        else
        {
            string? lmotdID = _dbh.GetMoTDChannelID()!;
            if(string.IsNullOrEmpty(lmotdID)) //LEFT OFF HERE, handle if no channel
            {            
                await SendMessage("No MoTD channel set.", BotTestChannelID);
                return;
            }
            lChannelID = ulong.Parse(lmotdID);   
        }
        
        if (lMergedMessages.Count == 0)
        {
            await SendMessage("Today is a slow day in history. No messages were found for today.", lChannelID);
            return;
        }
        string? lWeightedChannelID = _dbh.GetWeightedChannelID();
        var lBestMsg = MOTDService.GetMotD(lMergedMessages, lWeightedChannelID ?? string.Empty);
        if(lBestMsg == null)
        {
            await SendMessage("No message found for today.", lChannelID);
            return;
        }
        var lSourceChannel = await _discord.GetChannelAsync(ulong.Parse(lBestMsg.ChannelID));
        var lOriginalMsg = await lSourceChannel.GetMessageAsync(ulong.Parse(lBestMsg.MessageID));
        var lMotDFormat = MotDFormatter(lOriginalMsg, lBestMsg);

        await SendMessage(lOriginalMsg, lChannelID, lMotDFormat);                         
    }
    /// <summary>
    /// Formats and sends a message using a webhook
    /// </summary>
    /// <param name="aMessage">The Original message</param>
    /// <param name="aBestMsg">Best message with grouped content</param>
    /// <param name="aChannelID"></param>
    /// <returns></returns>
    private async Task SendMessage(DiscordMessage? aMessage, 
                        ulong aChannelID,
                        (string userName, string content, string? footer) aFormat)
    {
        var lWebHook = await EnsureAvailableWebhookAsync(aChannelID);  
        var lBot = await _discord.GetUserAsync(BotID);
        
        var lWebHookBuilder = new DiscordWebhookBuilder()
            .WithUsername(aFormat.userName)
            .WithContent(aFormat.content)
            .WithAvatarUrl(lBot.AvatarUrl);

        if(aMessage != null)
        {
           foreach (var attachment in aMessage.Attachments)
            {
                var lStream = await _httpClient.GetStreamAsync(attachment.Url);
                lWebHookBuilder.AddFile(attachment.FileName, lStream);
            } 
        }    
        
        await lWebHook.ExecuteAsync(lWebHookBuilder);

        if (!string.IsNullOrEmpty(aFormat.footer))
        {
            var lFooterBuilder = new DiscordWebhookBuilder()
                .WithUsername(aFormat.userName)
                .WithContent(aFormat.footer)
                .WithAvatarUrl(lBot.AvatarUrl);
            await lWebHook.ExecuteAsync(lFooterBuilder);
        }
    }
    /// <summary>
    /// Formats and sends a message using DiscordMessageBuilder, generally used if no channels are set
    /// </summary>
    /// <param name="aMessage"></param>
    /// <param name="aChannelID">Default Channel</param>
    /// <returns></returns>
    private async Task SendMessage(string aMessage, ulong aChannelID)
    {
        var lmsgBuilder = new DiscordMessageBuilder();
        lmsgBuilder.WithContent(aMessage);
        await _discord.GetChannelAsync(aChannelID).Result.SendMessageAsync(lmsgBuilder);
    }
    /// <summary>
    /// Groups messages from the same user within 7 minutes
    /// </summary>
    /// <param name="aMessages">A list of messages to be checked for similarity</param>
    /// <returns>A list of messages that have been merged based on timestamp and author</returns>
    private List<MessageRecord> MergeMultiPartMessages(List<MessageRecord> aMessages)
    {
        if (aMessages.Count == 0)
            return aMessages;
        List<MessageRecord> lSortedMessages = aMessages
            .OrderBy(m => m.Timestamp)
            .ToList();

        List<MessageRecord> lMergedMessages = new List<MessageRecord>();

        int i = 0;
        while(i < lSortedMessages.Count)
        {
            MessageRecord lStartNewMessage = lSortedMessages[i];
            List<MessageRecord> lGroup = new List<MessageRecord>();
            lGroup.Add(lStartNewMessage);

            int j = i + 1;
            while(j < lSortedMessages.Count)
            {
                MessageRecord lNextMessage = lSortedMessages[j];
                
                bool lSameAuthor = lStartNewMessage.AuthorID == lNextMessage.AuthorID;
                //current message is within 7 minutes of the first message
                bool lWithin7Minutes = (lNextMessage.Timestamp - lStartNewMessage.Timestamp).TotalMinutes <= 7; 
                
                if(!lSameAuthor || !lWithin7Minutes)
                    break;

                lGroup.Add(lNextMessage);
                j++;
            }

            lMergedMessages.Add(CollapseMessageContent(lGroup));
            i = j;            
        }

        return lMergedMessages;
    }
    /// <summary>
    /// Combines multiple messages into one
    /// </summary>
    /// <param name="aMessages">A list of messages from the same user within 7 minutes</param>
    /// <returns>A combined message</returns>
    private MessageRecord CollapseMessageContent(List<MessageRecord> aMessages)
    {
        MessageRecord lFirstMessage = aMessages[0];

        MessageRecord lNewMessage = new MessageRecord
        {
            Content = lFirstMessage.Content,
            AttachmentCount = lFirstMessage.AttachmentCount,
            ReactionCount = lFirstMessage.ReactionCount,
            Timestamp = lFirstMessage.Timestamp,
            MessageID = lFirstMessage.MessageID,
            GuildID = lFirstMessage.GuildID,
            ChannelID = lFirstMessage.ChannelID,
            AuthorID = lFirstMessage.AuthorID,
            Interestingness = lFirstMessage.Interestingness
        };
        for(int i = 1; i < aMessages.Count; i++)
        {
            lNewMessage.Content += "\n" + aMessages[i].Content;
            lNewMessage.AttachmentCount += aMessages[i].AttachmentCount;
            lNewMessage.ReactionCount += aMessages[i].ReactionCount;
        }        
        return lNewMessage;
    }
    public async Task PostChannelSummaryAsync(bool testMode = false)
    {
        Console.WriteLine($"Posting channel summary... (Test Mode: {testMode})");
        List<MessageRecord> lMessages =  _dbh.GetLast24HoursMsgs(DateTime.Now, "429063504725671950");

        CohereClient lCohere = new CohereClient(_cohereKey);
        string lContent = string.Empty;
        ulong lChannelID;

        foreach (var m in lMessages)
        {
            lContent += " ";
            lContent += m.Content.Replace("\n", " ");
        }
        
        string lPrompt = $"Summarize the following text: {lContent}";
        string lResponse = await lCohere.AskAsync(lPrompt);

        if (testMode)
        {
            lChannelID = BotTestChannelID;
        }
        else
        {
            string? lTLDRChannelID = _dbh.GetTLDRChannelID()!;
            lChannelID = ulong.Parse(lTLDRChannelID);
        }
        if(string.IsNullOrEmpty(lResponse))
        {
            await SendMessage("No response from Cohere.", lChannelID);
            return;
        }
        var lTLDRFormat = TLDRFormatter(lResponse);
        await SendMessage(null, lChannelID, lTLDRFormat);
    }
    private async Task RespondToUser(DiscordMessage aMessage, DiscordChannel aChannel, DiscordUser aUser)
    {
        if(!CanRespond(aUser, aMessage))
            return;
        Console.WriteLine("Responding...");
        DiscordGuild lGuild = await _discord.GetGuildAsync(aChannel.GuildId.Value);
        CohereClient lCohere = new CohereClient(_cohereKey);

        string lPrompt = $"Respond to the following message as if you are another user in Discord but do it " 
            + $"in in a sultry yet playful manner. The reponse doesn't have to be long or overly detailed "
            + $"but it should be interesting and slightly suggestive: {aMessage.Content}";
        string lResponse = await lCohere.AskAsync(lPrompt);
        var lSultryFormat = await SultryResponse(lResponse, aUser, lGuild);
        _lastBotRespond[aUser.Id] = DateTime.UtcNow;

        await SendMessage(null, aChannel.Id, lSultryFormat);
    }
    public static (string, string, string?) MotDFormatter(DiscordMessage aMsg, MessageRecord aBestMsg)
    {
        string lUserName = $"\nOn this day in {aBestMsg.Timestamp.Year}";
        string lContent = $"------------------------------------------------\n" 
                + $"<@{aBestMsg.AuthorID}> said: \n"
                + $"{aBestMsg.Content} \n\n";
        string lFooter = $"------------------------------------------------\n" 
                + $"[view orignal message]({aMsg.JumpLink})";

        return (lUserName, lContent, lFooter);
    }
    public static (string, string, string?) TLDRFormatter(string aMsg)
    {
        string lUserName = $"\nToday's TLDR:";
        string lContent = $"------------------------------------------------\n" 
                + $"{aMsg}";
        string lFooter = string.Empty;

        return (lUserName, lContent, lFooter);
    }
    public static async Task<(string, string, string?)> SultryResponse(string aMsg, DiscordUser aUser, DiscordGuild aGuild)
    {   
        DiscordMember lMember = await aGuild.GetMemberAsync(aUser.Id);
        string lUserNickName = lMember.Nickname ?? aUser.Username;
        string lUserName = $"\n{lUserNickName}'s Sex Kitten:";
        string lContent = $"{aMsg}";
        string lFooter = string.Empty;

        return (lUserName, lContent, lFooter);
    }
}