using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
public class MotdPoster : BackgroundService
{
    private readonly IMotdPostingService _motdPostingService;
    private readonly IMessagingService _messagingService;
    private readonly ILogger<MotdPoster> _logger;

    public MotdPoster(IMotdPostingService aMotdPostingService, 
                    IMessagingService aMessagingService, 
                    ILogger<MotdPoster> aLogger)
    {
        _motdPostingService = aMotdPostingService;
        _messagingService = aMessagingService;
        _logger = aLogger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MotdPoster is starting at {DateTime}", DateTime.UtcNow);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                DateTime lNow = DateTime.UtcNow;
                DateTime lNextRun = lNow.Date.AddHours(17);//noon est
    
                if(lNow >= lNextRun) 
                    lNextRun = lNextRun.AddDays(1);
    
                TimeSpan lInterval = lNextRun - lNow;
                _logger.LogInformation("MotdPoster is sleeping for {Interval}", lInterval);
                await Task.Delay(lInterval, stoppingToken);
    
                DateTime lToday = DateTime.UtcNow.Date;
    
                List<ulong> lGuildsDueForMotdPosting = await _motdPostingService.GetGuildsDueForMotdPostingAsync(lToday);
    
                foreach (var guildID in lGuildsDueForMotdPosting)
                {
                    try
                    {
                        ulong lMotdChannelID = await _motdPostingService.GetMotdChannelID(guildID);
                        if(lMotdChannelID == 0)//motd channel not set
                        {
                            await _messagingService.SendMissingMotdChannelAsync(guildID);
                            continue;
                        }
        
                        MessageRecord? lMotd = await _motdPostingService.GetMotdAsync(lToday, guildID);
        
                        if(lMotd != null)
                        {
                            await _motdPostingService.SendMotdAsync(lMotd, lMotdChannelID);
                        }else
                        {
                            await _messagingService.SendNoMotdFoundAsync(lMotdChannelID);
                            await _motdPostingService.SetLastMotdDate(DateTime.UtcNow, guildID);
                        } 
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error posting MOTD for guild {GuildID}. Continue with next guild", guildID);
                        continue;
                    }                  
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MotdPoster. Restarting...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}