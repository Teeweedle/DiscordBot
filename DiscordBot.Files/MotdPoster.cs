using Microsoft.Extensions.Hosting;
public class MotdPoster : BackgroundService
{
    private readonly IMotdPostingService _motdService;
    private readonly MessagingService _messagingService;

    public MotdPoster(IMotdPostingService aMotdService, MessagingService aMessagingService)
    {
        _motdService = aMotdService;
        _messagingService = aMessagingService;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ulong lMotdChannelID = await _motdService.GetMotdChannelID();

            if(lMotdChannelID == 0)//motd channel not set
                return;

            DateTime lLastMotdDate = await _motdService.GetLastMotDDate();
            if(DateTime.UtcNow - lLastMotdDate >= TimeSpan.FromDays(1))
            {
                MessageRecord? lMotd = await _motdService.GetMotdAsync(DateTime.UtcNow.Date);
                if(lMotd != null)
                {
                    await _messagingService.PostMotDAsync(lMotd, lMotdChannelID);
                    lLastMotdDate = DateTime.UtcNow.Date;
                }else
                {
                    await _messagingService.SendEmptyMotdResponseAsync();
                } 
            }
            TimeSpan lInterval = TimeSpan.FromDays(1) - (DateTime.UtcNow - lLastMotdDate);
            await Task.Delay(lInterval, stoppingToken);
        }
    }
}