using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.DependencyInjection;

public class MyCommands : ApplicationCommandModule
{   
    private readonly Messaging _messaging;
    private readonly IReminderService _reminderService;
    public MyCommands(Messaging aMessaging, IReminderService aReminderService)
    {
        _messaging = aMessaging;
        _reminderService = aReminderService;
    } 
    [SlashCommand("SetMotDChannel", "Set MotD channel for this guild. Requires admin permissions.")]
    [SlashCommandPermissions(Permissions.Administrator)]
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
    [SlashCommand("SetMotDWeightedChannel", "Set weighted channel for MotD functionality. Requires admin permissions.")]
    [SlashCommandPermissions(Permissions.Administrator)]
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
    [SlashCommand("GetCurrentWeightedChannel", "Get current MotD weighted channel.")]
    [SlashCommandPermissions(Permissions.Administrator)]
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
    [SlashCommand("TLDR", "Get TLDR for this channel.")]
    [SlashRequirePermissions(Permissions.SendMessages)]
    public async Task TLDRCommand(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync(
            InteractionResponseType.DeferredChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AsEphemeral(true)
        );

        string lSummary = await _messaging.GetChannelSummaryAsync(ctx.Channel);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle($"TLDR for #{ctx.Channel.Name}")
                .WithDescription(lSummary)
                .WithColor(DiscordColor.Yellow))
            );
    }
      
    [SlashCommand("SetTarget", "Set target user and target channel for responses. Requires admin permissions.")]
    [SlashCommandPermissions(Permissions.Administrator)]
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
    [SlashCommand("PostMotD", "Get today's MotD")]
    [SlashCommandPermissions(Permissions.Administrator)]
    public async Task OTDCommand(InteractionContext ctx)
    {
        if(!ctx.Member.Permissions.HasPermission(Permissions.Administrator))
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("You must be an admin to use this command."));
            return;
        }
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AsEphemeral(true));
        try
        {
            // var lStopWatch = Stopwatch.StartNew();
            await _messaging.PostMotDAsync(ctx.Channel.Id);
            // lStopWatch.Stop();
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Done."));
        }
        catch (Exception ex)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"[PostMotD Error] {ex.GetType().Name}: {ex.Message}"));
            throw;
        }        
    }
    [SlashCommand("RemindMe", "Remind me when...")]
    [SlashCommandPermissions(Permissions.SendMessages)]
    public async Task RemindMeCommand(InteractionContext ctx, 
                            [Option("amount", "Numerical Value")] long aAmount, 
                            [Option("unit", "Unit of time")]
                                [Choice("seconds", "s")] 
                                [Choice("minutes", "m")] 
                                [Choice("hours", "h")] 
                                [Choice("days", "d")] 
                                [Choice("months", "mo")] 
                                [Choice("years", "y")] string aUnit,
                            [Option("message", "Reminder message")] string aMessage)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AsEphemeral(true));
        try
        {
            await _reminderService.CreateReminder(ctx.Member.Id, aAmount, aUnit, aMessage, ctx.Interaction.Id);
        }
        catch (Exception ex)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"[RemindMe Error] {ex.GetType().Name}: {ex.Message}"));
            throw;
        }
    }
}