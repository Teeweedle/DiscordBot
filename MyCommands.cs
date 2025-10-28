using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

public class MyCommands : ApplicationCommandModule
{
    [SlashCommand("hello", "Say hello to the bot")]
    public async Task HelloCommand(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync($"Hello, {ctx.User.Username}! 👋");
    }
    [SlashCommand("setMOTDChannel", "Set MotD channel for this guild. Requires admin permissions.")]    
    public async Task SetMOTDChannelCommand(InteractionContext ctx, [Option("channel", "Channel to set")] DiscordChannel channel)
    {
        //TODO: Store channel for posting
        //await ctx.CreateResponseAsync($"Set MotD channel to {channel.Name}");
    }
    [SlashCommand("otd", "Get today's On This Day message")]
    public async Task OTDCommand(InteractionContext ctx)
    {
        //TODO: Get a rng message for this day some time ago...
        
    }
}