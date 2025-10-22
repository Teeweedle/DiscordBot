using DSharpPlus.SlashCommands;

public class MyCommands : ApplicationCommandModule
{
    [SlashCommand("hello", "Say hello to the bot")]
    public async Task HelloCommand(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync($"Hello, {ctx.User.Username}! 👋");
    }
}