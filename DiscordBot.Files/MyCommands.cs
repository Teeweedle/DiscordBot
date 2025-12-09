using System.Reflection;
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
    public async Task GetCurrentWeightedChannelCommand(InteractionContext ctx)
    {
        var ldb = ctx.Services.GetRequiredService<DatabaseHelper>();
        if(ctx.Guild == null)
        {
            await ctx.CreateResponseAsync("You must be in a guild to use this command.");
            return;
        } 
        string? lChannelID = ldb.GetWeightedChannelID();
        if(string.IsNullOrEmpty(lChannelID))
        {
            await ctx.CreateResponseAsync("No weighted channel set.");
            return; 
        }    
        var channel = await ctx.Client.GetChannelAsync(ulong.Parse(lChannelID));

        await ctx.CreateResponseAsync($"Current weighted channel is {channel?.Name}.");
    }
    [SlashCommand("SetTLDRChannel", "Set TLDR channel for this guild. Requires admin permissions.")]
    public async Task SetTLDRChannelCommand(InteractionContext ctx, [Option("channel", "Channel to set")] DiscordChannel aChannel)
    {
        try
        {
            var lDB = ctx.Services.GetRequiredService<DatabaseHelper>();
    
            if (!ctx.Member.Permissions.HasPermission(Permissions.Administrator))
            {
                await ctx.CreateResponseAsync("You must be an admin to use this command.");
                return;
            }
    
            lDB.SetTLDRChannel(ctx.Guild.Id.ToString(), aChannel.Id.ToString());
    
            await ctx.CreateResponseAsync($"Set TLDR channel to {aChannel.Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SetTLDRChannel Error] {ex.GetType().Name}: {ex.Message}");            
        }
    }
    [SlashCommand("GetTLDRChannel", "Get TLDR channel for this guild. Requires admin permissions.")]
    public async Task GetTLDRChannelCommand(InteractionContext ctx)
    {
        var lDB = ctx.Services.GetRequiredService<DatabaseHelper>();

        if (!ctx.Member.Permissions.HasPermission(Permissions.Administrator))
        {
            await ctx.CreateResponseAsync("You must be an admin to use this command.");
            return;
        }

        string? lChannelID = lDB.GetTLDRChannelID();
        if (string.IsNullOrEmpty(lChannelID))
        {
            await ctx.CreateResponseAsync("No TLDR channel set.");
            return;
        }

        var channel = await ctx.Client.GetChannelAsync(ulong.Parse(lChannelID));

        await ctx.CreateResponseAsync($"Current TLDR channel is {channel?.Name}.");
    }
    [SlashCommand("SetTarget", "Set target user and target channel for responses. Requires admin permissions.")]
    public async Task SetTargetCommand(InteractionContext ctx, 
                            [Option("user", "User to set")] DiscordUser aUser, 
                            [Option("channel", "Channel to set")] DiscordChannel aChannel)
    {        
        if (!ctx.Member.Permissions.HasPermission(Permissions.Administrator))
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("You must be an admin to use this command."));         
            return;
        }
        try
        {
            var lDB = ctx.Services.GetRequiredService<DatabaseHelper>();
            lDB.SetTargetUserAndChannel(aUser.Id.ToString(), aChannel.Id.ToString());
    
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent($"Set target to {aUser.Username} in {aChannel.Name}.")); 
        }
        catch (Exception ex)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent($"[SetTarget Error] {ex.GetType().Name}: {ex.Message}"));
        }       
    }
    [SlashCommand("otd", "Get today's On This Day message")]
    public async Task OTDCommand(InteractionContext ctx)
    {
        //TODO: Get a rng message for this day some time ago...
        //Send ctx to PostMotD so it knows what channel if no channel is set

    }   
}