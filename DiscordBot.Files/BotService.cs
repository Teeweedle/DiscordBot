using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class BotService : BackgroundService
{
    private readonly DiscordClient _discord;
    private readonly IServiceProvider _services;
    private readonly IChannelScraper _channelScraper;
    private static ulong _guildID = 429063504725671947;
    private readonly ILogger<BotService> _logger;
    private readonly IGuildDataManager _guildDataManager;

    public BotService(DiscordClient aDiscord, 
                    IServiceProvider aServices, 
                    IChannelScraper aChannelScraper,
                    ILogger<BotService> aLogger,
                    IGuildDataManager aGuildDataManager)
    {        
        _discord = aDiscord;
        _services = aServices;
        _logger = aLogger;
        //start the channel scraper
        _channelScraper = aChannelScraper;
        _guildDataManager = aGuildDataManager;

        _discord.GuildDeleted += OnGuildDeletedAsync;
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