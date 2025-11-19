using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;

public class MyCommands : ApplicationCommandModule
{   
    /// <summary>
    /// Set MotD channel for this guild. Saves it to database located in data folder ChannelInfo.db
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="aChannel"></param>
    /// <returns></returns>
    [SlashCommand("SetMotDChannel", "Set MotD channel for this guild. Requires admin permissions.")]
    public async Task SetMOTDChannelCommand(InteractionContext ctx, [Option("channel", "Channel to set")] DiscordChannel aChannel)
    {
        var lDB = ctx.Services.GetRequiredService<DatabaseHelper>();

        if (!ctx.Member.Permissions.HasPermission(Permissions.Administrator))
        {
            await ctx.CreateResponseAsync("You must be an admin to use this command.");
            return;
        }
 
        lDB.SetMotDChannel(ctx.Guild.Id.ToString(), aChannel.Id.ToString());

        await ctx.CreateResponseAsync($"Set MotD channel to {aChannel.Name}");
    }
    /// <summary>
    /// Set weighted channel for MotD functionality. Messages count for more weight in this channel. Saves it to database located in data folder ChannelInfo.db
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="channel"></param>
    /// <returns></returns>
    [SlashCommand("SetMotDWeightedChannel", "Set weighted channel for MotD functionality. Requires admin permissions.")]
    public async Task SetMOTDWeightedChannelCommand(InteractionContext ctx, [Option("channel", "Channel to set")] DiscordChannel channel)
    {
        var ldb = ctx.Services.GetRequiredService<DatabaseHelper>();

        if (!ctx.Member.Permissions.HasPermission(Permissions.Administrator))
        {
            await ctx.CreateResponseAsync("You must be an admin to use this command.");
            return;
        }

        ldb.SetWeightedChannel(ctx.Guild.Id.ToString(), channel.Id.ToString());

        await ctx.CreateResponseAsync($"Set weighted channel to {channel.Name}");
    }
    /// <summary>
    /// Get current weighted channel
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    [SlashCommand("GetCurrentWeightedChannel", "Get current MotD weighted channel.")]
    public async Task CurrentWeightedChannelCommand(InteractionContext ctx)
    {
        var ldb = ctx.Services.GetRequiredService<DatabaseHelper>();
        if(ctx.Guild == null)
        {
            await ctx.CreateResponseAsync("You must be in a guild to use this command.");
            return;
        } 
        string? lChannelID = ldb.GetWeightedChannelID(ctx.Guild.Id.ToString());
        if(string.IsNullOrEmpty(lChannelID))
        {
            await ctx.CreateResponseAsync("No weighted channel set.");
            return; 
        }    
        var channel = await ctx.Client.GetChannelAsync(ulong.Parse(lChannelID));

        await ctx.CreateResponseAsync($"Current weighted channel is {channel?.Name}.");
    }
    [SlashCommand("otd", "Get today's On This Day message")]
    public async Task OTDCommand(InteractionContext ctx)
    {
        //TODO: Get a rng message for this day some time ago...

    }   
}