using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
public class MotdPoster : BackgroundService
{
    private readonly IMotdPostingService _motdService;
    private readonly MessagingService _messagingService;
    private readonly ILogger<MotdPoster> _logger;

    public MotdPoster(IMotdPostingService aMotdService, 
                    MessagingService aMessagingService, 
                    ILogger<MotdPoster> aLogger)
    {
        _motdService = aMotdService;
        _messagingService = aMessagingService;
        _logger = aLogger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                DateTime lNow = DateTime.UtcNow;
                DateTime lNextRun = lNow.Date.AddHours(17);//noon est
    
                if(lNow >= lNextRun) 
                    lNextRun = lNextRun.AddDays(1);
    
                TimeSpan lInterval = lNextRun - lNow;
                await Task.Delay(lInterval, stoppingToken);
    
                DateTime lToday = DateTime.UtcNow.Date;
    
                List<ulong> lGuildsDueForMotdPosting = await _motdService.GetGuildsDueForMotdPostingAsync(lToday);
    
                foreach (var guildID in lGuildsDueForMotdPosting)
                {
                    try
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
                            await _motdService.SendMotdAsync(lMotd, lMotdChannelID);
                        }else
                        {
                            await _messagingService.SendNoMotdFoundAsync(lMotdChannelID);
                            await _motdService.SetLastMotdDate(DateTime.UtcNow, guildID);
                        } 
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error posting MOTD for guild {GuildID}. Continue with next guild", guildID);
                        throw;
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