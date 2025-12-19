using Microsoft.Extensions.Hosting;

public class ReminderChecker : BackgroundService
{
    private readonly IReminderService _reminderService;
    public ReminderChecker(IReminderService aReminderService)
    {
        _reminderService = aReminderService;
    }
    protected override async Task ExecuteAsync(CancellationToken aStoppingToken)
    {
        //load the days reminders
        _reminderService.LoadExpiringReminderList();

        while(!aStoppingToken.IsCancellationRequested)
        {
            var lInterval = _reminderService.GetNextReminderInterval();
            if (lInterval == TimeSpan.Zero)
            {
                await Task.Delay(TimeSpan.FromMinutes(10), aStoppingToken);
                continue;
            }
            await Task.Delay(lInterval, aStoppingToken);
            await _reminderService.CheckForExpiredReminders(); 
        }
    }
}