using DSharpPlus;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Hosting;

public class BotService : BackgroundService
{
    private readonly DiscordClient _discord;
    private readonly IServiceProvider _services;
    private readonly IChannelScraper _channelScraper;
    private static ulong _guildID = 429063504725671947;

    public BotService(DiscordClient aDiscord, 
                    IServiceProvider aServices, 
                    IChannelScraper aChannelScraper)
    {        
        _discord = aDiscord;
        _services = aServices;
        //start the channel scraper
        _channelScraper = aChannelScraper;
    }
    protected override async Task ExecuteAsync(CancellationToken aStoppingToken)
    {              
        Console.WriteLine($"Today is: {DateOnly.FromDateTime(DateTime.Now)}");
        
        RegisterCommands();

        await _discord.ConnectAsync();
        Console.WriteLine($"Guild count after connect: {_discord.Guilds.Count}");
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