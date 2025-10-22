using DSharpPlus.SlashCommands;

public class MyCommands : ApplicationCommandModule
{
    [SlashCommand("hello", "Say hello to the bot")]
    public async Task HelloCommand(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync($"Hello, {ctx.User.Username}! 👋");
    }
    [SlashCommand("otd", "Get today's On This Day message")]
    public async Task OTDCommand(InteractionContext ctx)
    {
        //TODO: Get a rng message for this day some time ago...
    }
}