using System.Diagnostics;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.DependencyInjection;

public class MyCommands : ApplicationCommandModule
{   
    private readonly MessagingService _messagingService;
    private readonly IReminderService _reminderService;
    private readonly MotdService _motdService;
    private readonly ChannelSummaryService _channelSummaryService;
    private readonly BotInfoService _botInfoService;
    public MyCommands(Messaging aMessaging, 
                    MessagingService aMessagingService,
                    IReminderService aReminderService, 
                    MotdService aMotdService, 
                    ChannelSummaryService aChannelSummaryService,
                    BotInfoService aBotInfoService)
    {
        _messagingService = aMessagingService;
        _reminderService = aReminderService;
        _motdService = aMotdService;
        _channelSummaryService = aChannelSummaryService;
        _botInfoService = aBotInfoService;
    } 
    [SlashCommand("SetMotDChannel", "Sets the channel to send MotD to. Requires admin permissions.")]
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
    [SlashCommand("SetMotDWeightedChannel", "Sets the weighted channel messages in this channel are more likely to be picked.")]
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
        ulong lGuildID = ctx.Guild.Id;
        if(ctx.Guild == null)
        {
            await ctx.CreateResponseAsync("You must be in a guild to use this command.");
            return;
        } 
        string? lChannelID = ldb.GetWeightedChannelID(lGuildID.ToString());
        if(string.IsNullOrEmpty(lChannelID))
        {
            await ctx.CreateResponseAsync("No weighted channel set.");
            return; 
        }    
        var channel = await ctx.Client.GetChannelAsync(ulong.Parse(lChannelID));

        await ctx.CreateResponseAsync($"Current weighted channel is {channel?.Name}.");
    }   
    [SlashCommand("TLDR", "Get a summary of the past 24 hours in the current channel.")]
    [SlashRequirePermissions(Permissions.SendMessages)]
    public async Task TLDRCommand(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync(
            InteractionResponseType.DeferredChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AsEphemeral(true)
        );

        string lSummary = await _channelSummaryService.GetChannelSummaryAsync(ctx.Channel.Id);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(new DiscordEmbedBuilder()
                .WithTitle($"TLDR for #{ctx.Channel.Name}")
                .WithDescription(lSummary)
                .WithColor(DiscordColor.Green))
            );
    }
      
    [SlashCommand("SetTarget", "he bot will respond to this user. Requires admin permissions.")]
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
            lDB.SetTargetUserAndChannel(aUser.Id.ToString(), aChannel.Id.ToString(), ctx.Guild.Id.ToString() );
    
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent($"Set target to {aUser.Username} in {aChannel.Name}.")); 
        }
        catch (Exception ex)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent($"[SetTarget Error] {ex.GetType().Name}: {ex.Message}"));
        }       
    }
    [SlashCommand("PostMotD", "Posts the MotD for today in the channel you use this command in")]
    [SlashCommandPermissions(Permissions.Administrator)]
    public async Task MotdCommand(InteractionContext ctx)
    {
        ulong lGuildID = ctx.Guild.Id;
        ulong lChannelID = ctx.Channel.Id;

        if(!ctx.Member.Permissions.HasPermission(Permissions.Administrator))
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("You must be an admin to use this command."));
            return;
        }
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AsEphemeral(true));

        MessageRecord? lMotd = await _motdService.GetMotdAsync(DateTime.UtcNow, lGuildID);

        if(lMotd == null)
        {
            await _messagingService.SendNoMotdFoundAsync(lChannelID);        
            return;
        }
        try
        {
            // var lStopWatch = Stopwatch.StartNew();
            await _messagingService.PostMotDAsync(lMotd, lChannelID);
            // lStopWatch.Stop();
            // var elapsed = lStopWatch.Elapsed; 
            // await ctx.EditResponseAsync( 
            //     new DiscordWebhookBuilder().WithContent($"Done in {elapsed.TotalMilliseconds:N0} ms"));
            await ctx.DeleteResponseAsync();
        }
        catch (Exception ex)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"[PostMotD Error] {ex.GetType().Name}: {ex.Message}"));
            throw;
        }        
    }
    [SlashCommand("RemindMe", "Sets a custom reminder for you to be reminded of something in the future")]
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
            await _reminderService.CreateReminder(ctx.Member.Id, ctx.Guild.Id, aAmount, aUnit, aMessage, ctx.Interaction.Id);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Reminder Set ✅"));
        }
        catch (Exception ex)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"[RemindMe Error] {ex.GetType().Name}: {ex.Message}"));
            throw;
        }
    }
    [SlashCommand("Info", "Command and Channel info for this bot")]
    [SlashCommandPermissions(Permissions.SendMessages)]
    public async Task InfoCommand(InteractionContext ctx)
    {
        ulong lGuildID = ctx.Guild.Id;
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AsEphemeral(true));

        try
        {
            BotInfoDTO lInfo = await _botInfoService.GetChannelInfo(lGuildID.ToString());

            var lEmbed = new DiscordEmbedBuilder()
            .WithTitle("🤖 Bot Configuration & Commands")
                .WithDescription("### ⚙️ Setup Commands\n" +
                                "• `/setmotdchannel` - Sets the channel to send MotD to\n" +
                                "• `/setmotdweightedchannel` - Sets the weighted channel messages in this channel are more likely to be picked\n" +
                                "• `/settargetuser` - The bot will respond to this user\n" +
                                "• `/settargetchannel` - The bot will respond to the chosen user in this channel\n" +
                                "### 🛠️ General Commands\n" +
                                "• `/postmotd` - Posts the MotD for today in the channel you use this command in\n" +
                                "• `/remindme` - Sets a custom reminder for you to be reminded of something in the future\n" +
                                "• `/tldr` - Get a summary of the past 24 hours in the current channel\n" +
                                "• `/info` - Get info on commands and channels for this bot")
                .AddField("📢 **MotD Channel**", 
                    lInfo.MotdChannel is null ? "❌ *Not Set*" : $"# {lInfo.MotdChannel}", inline: true)
                .AddField("💪 **Weighted Channel**", 
                    lInfo.WeightedChannel is null ? "❌ *Not Set*" : $"# {lInfo.WeightedChannel}", inline: true)
                .AddField("👤 **Target User**", 
                    lInfo.TargetUser is null ? "❌ *Not Set*" : lInfo.TargetUser, inline: true)
                .AddField("🎯 **Target Channel**", 
                    lInfo.TargetChannel is null ? "❌ *Not Set*" : $"# {lInfo.TargetChannel}", inline: true)
                .AddField("📆 **Has MotD been posted**", 
                    lInfo.HasMotdBeenPosted ? "✅ Yes" : "❌ No", inline: true)
                .WithColor(DiscordColor.Green)
                .Build();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(lEmbed));
        }        
        catch (Exception ex)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"[Info Error] {ex.GetType().Name}: {ex.Message}"));
            Console.WriteLine($"[Info Error] {ex}");
        }
    }
}