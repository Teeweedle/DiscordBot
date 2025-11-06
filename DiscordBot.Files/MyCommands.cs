using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Data.Sqlite;

public class MyCommands : ApplicationCommandModule
{
    private readonly DatabaseHelper _dbh;
    public MyCommands(DatabaseHelper aDbh)
    {
        _dbh = aDbh;
    } 
    [SlashCommand("SetMotDChannel", "Set MotD channel for this guild. Requires admin permissions.")]
    public async Task SetMOTDChannelCommand(InteractionContext ctx, [Option("channel", "Channel to set")] DiscordChannel channel)
    {
        //TODO: Store channel for posting
        //await ctx.CreateResponseAsync($"Set MotD channel to {channel.Name}");
    }
    /// <summary>
    /// Set weighted channel for MotD functionality. Messages count for more weight in this channel.
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="channel"></param>
    /// <returns></returns>
    [SlashCommand("SetMotDWeightedChannel", "Set weighted channel for MotD functionality. Requires admin permissions.")]
    public async Task SetMOTDWeightedChannelCommand(InteractionContext ctx, [Option("channel", "Channel to set")] DiscordChannel channel)
    {
        if (!ctx.Member.Permissions.HasPermission(Permissions.Administrator))
        {
            await ctx.CreateResponseAsync("You must be an admin to use this command.");
            return;
        }
        using var lConnection = new SqliteConnection("Data Source=Database.db;Version=3;");
        lConnection.Open();

        string lInsertCmd = @"
            INSERT INTO WeightedChannels (GuildID, WeightedChannelID) 
            VALUES ($GuildID, $WeightedChannelID) 
            ON CONFLICT (GuildID) DO UPDATE SET WeightedChannelID = $WeightedChannelID";

        using var lCmd = new SqliteCommand(lInsertCmd, lConnection);

        lCmd.Parameters.AddWithValue("$GuildID", ctx.Guild.Id.ToString());
        lCmd.Parameters.AddWithValue("$WeightedChannelID", channel.Id.ToString());
        lCmd.ExecuteNonQuery();

        await ctx.CreateResponseAsync($"Set weighted channel to {channel.Name}");
    }
    [SlashCommand("GetCurrentWeightedChannel", "Get current MotD weighted channel.")]
    public async Task CurrentWeightedChannelCommand(InteractionContext ctx)
    {
        if(ctx.Guild == null)
        {
            await ctx.CreateResponseAsync("You must be in a guild to use this command.");
            return;
        } 
        string? lChannelID = _dbh.GetWeightedChannelID(ctx.Guild.Id.ToString());
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