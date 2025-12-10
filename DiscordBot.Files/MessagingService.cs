using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;

public class MessagingService
{
    private readonly DiscordClient _discord;
    private readonly HttpClient _httpClient;
    private readonly DatabaseHelper _dbh;
    private CohereClient _cohereClient;
    private readonly ulong BotID = 1428047784245854310;
    
    public MessagingService(DiscordClient aDiscord, 
                            HttpClient aHttpClient, 
                            DatabaseHelper aDatabaseHelper, 
                            CohereClient aCohereClient)
    {
        _discord = aDiscord;
        _httpClient = aHttpClient;
        _dbh = aDatabaseHelper;
        _cohereClient = aCohereClient;
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
    /// <summary>
    /// Formats and sends a message using a webhook
    /// </summary>
    /// <param name="aMessage">The Original message</param>
    /// <param name="aBestMsg">Best message with grouped content</param>
    /// <param name="aChannelID"></param>
    /// <returns></returns>
    public async Task SendWebhookMessageAsync(DiscordMessage? aMessage, 
                        ulong aChannelID,
                        (string userName, string content, string? footer) aFormat)    {

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
    public async Task SendChannelMessageAsync(string aMessage, ulong aChannelID)
    {
        var lmsgBuilder = new DiscordMessageBuilder();
        lmsgBuilder.WithContent(aMessage);
        var lChannel = await _discord.GetChannelAsync(aChannelID);
        await lChannel.SendMessageAsync(lmsgBuilder);
    }
    //TODO: Don't hard code channel (it's set to general have it get a channel)
    public List<MessageRecord> GetLast24HoursMsgs() => _dbh.GetLast24HoursMsgs(DateTime.Now, "429063504725671950");
    public List<MessageRecord> GetTodaysMsgs() => _dbh.GetTodaysMsgs(DateTime.UtcNow.Date);
    public string GetMoTDChannelID() => _dbh.GetMoTDChannelID()!;
    public string GetWeightedChannelID() => _dbh.GetWeightedChannelID()!;
    public async Task<DiscordChannel> GetSourceChannel(ulong aChannelID) => await _discord.GetChannelAsync(aChannelID)!;
    public CohereClient GetCohereClient() => _cohereClient;
    public async Task<DiscordGuild> GetDiscordGuild(ulong aGuildID) => await _discord.GetGuildAsync(aGuildID);
    public string GetTLDRChannelID() => _dbh.GetTLDRChannelID()!;
    
}