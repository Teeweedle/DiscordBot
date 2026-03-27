using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class BotService : BackgroundService
{
    private readonly DiscordClient _discord;
    private readonly IServiceProvider _services;
    private readonly MessagingService _messageService;
    private readonly IChannelScraper _channelScraper;
    private static ulong _guildID = 429063504725671947;
    private readonly ILogger<BotService> _logger;
    private readonly IGuildDataManager _guildDataManager;

    public BotService(DiscordClient aDiscord, 
                    IServiceProvider aServices, 
                    IChannelScraper aChannelScraper,
                    ILogger<BotService> aLogger,
                    IGuildDataManager aGuildDataManager,
                    MessagingService aMessagingService)
    {        
        _discord = aDiscord;
        _services = aServices;
        _logger = aLogger;
        //start the channel scraper
        _channelScraper = aChannelScraper;
        _guildDataManager = aGuildDataManager;
        _messageService = aMessagingService;

        _discord.GuildDeleted += OnGuildDeletedAsync;
        _discord.GuildMemberRemoved += OnGuildMemberRemovedAsync;
        _discord.MessageCreated += OnMessageCreatedAsync;
        _discord.MessageDeleted += OnMessageDeletedAsync;
        _discord.MessageReactionAdded += OnMessageReactionAddedAsync;
        _discord.MessageReactionRemoved += OnMessageReactionRemovedAsync;
        // _discord.MessageUpdated += OnMessageUpdatedAsync;
    }
    private async Task OnMessageReactionAddedAsync(DiscordClient sender, MessageReactionAddEventArgs args)
    {
        await _messageService.OnMessageReactionAddedAsync(args);
    }
    private async Task OnMessageReactionRemovedAsync(DiscordClient sender, MessageReactionRemoveEventArgs args)
    {
        await _messageService.OnMessageReactionRemovedAsync(args);
    }
    private async Task OnMessageDeletedAsync(DiscordClient sender, MessageDeleteEventArgs args)
    {
        await _messageService.OnMessageDeletedAsync(args);
    }

    private async Task OnMessageCreatedAsync(DiscordClient sender, MessageCreateEventArgs args)
    {
        await _messageService.OnMessageCreatedAsync(args);
    }

    private async Task OnGuildMemberRemovedAsync(DiscordClient sender, GuildMemberRemoveEventArgs args)
    {
        await _guildDataManager.PurgeUserDataAsync(args.Member.Id, args.Guild.Id);
    }

    private async Task OnGuildDeletedAsync(DiscordClient sender, GuildDeleteEventArgs args)
    {
        await _guildDataManager.PurgeGuildDataAsync(args.Guild.Id);
    }

    protected override async Task ExecuteAsync(CancellationToken aStoppingToken)
    {              
        RegisterCommands();
        
        await _discord.ConnectAsync();

        // await DeleteCommands();  //for testing
        // Keep the program running
        await Task.Delay(-1, aStoppingToken);
    }
    public void RegisterCommands()
    {
        var slash = _discord.UseSlashCommands(new SlashCommandsConfiguration
        {
            Services = _services
        });
        slash.SlashCommandErrored += (s, e) =>
        {
            _logger.LogError(e.Exception, "Slash command errored");
            return Task.CompletedTask;
        };
        slash.RegisterCommands<MyCommands>();
    }
    public async Task DeleteCommands()
    {
        var cmds = await _discord.GetGuildApplicationCommandsAsync(_guildID);
        foreach (var cmd in cmds)
        {
            Console.WriteLine($"Deleting command: {cmd.Name} ({cmd.Id})");
            await _discord.DeleteGuildApplicationCommandAsync(_guildID, cmd.Id);
        }
        Console.WriteLine("All guild commands deleted. Restarting clean…");
    }
}