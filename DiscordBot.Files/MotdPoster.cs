using DSharpPlus;
using Microsoft.Extensions.Hosting;
public class MotdPoster : BackgroundService
{
    private readonly IMotdPostingService _motdService;
    private readonly MessagingService _messagingService;

    public MotdPoster(IMotdPostingService aMotdService, 
                    MessagingService aMessagingService)
    {
        _motdService = aMotdService;
        _messagingService = aMessagingService;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            DateTime lNow = DateTime.UtcNow;
            DateTime lNextRun = lNow.Date.AddHours(12);

            if(lNow > lNextRun) 
                lNextRun = lNow.Date.AddDays(1);

            TimeSpan lInterval = lNextRun - lNow;
            await Task.Delay(lInterval, stoppingToken);

            DateTime lToday = DateTime.UtcNow.Date;

            List<ulong> lGuildsDueForMotdPosting = await _motdService.GetGuildsDueForMotdPostingAsync(lToday);

            foreach (var guildID in lGuildsDueForMotdPosting)
            {
                ulong lMotdChannelID = await _motdService.GetMotdChannelID(guildID);
                if(lMotdChannelID == 0)//motd channel not set
                {
                    await _messagingService.SendMissingMotdChannelAsync(guildID);
                    continue;
                }

                MessageRecord? lMotd = await _motdService.GetMotdAsync(lToday, guildID);

                if(lMotd != null)
                {
                    await _messagingService.PostMotDAsync(lMotd, lMotdChannelID);
                }else
                {
                    await _messagingService.SendNoMotdFoundAsync(lMotdChannelID);
                } 
                await _motdService.SetLastMotDDate(DateTime.UtcNow.Date, guildID);                    
            }
        }
    }
}