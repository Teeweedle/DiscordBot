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
        while(!aStoppingToken.IsCancellationRequested)
        {
            await _reminderService.LoadExpiringReminderList();
            await Task.Delay(-1);
        }
    }
}