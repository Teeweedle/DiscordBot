using Microsoft.Extensions.Hosting;

public class ReminderChecker : BackgroundService
{
    private readonly IReminderService _reminderService;
    private readonly ReminderSignal _reminderSignal;
    public ReminderChecker(IReminderService aReminderService, ReminderSignal aReminderSignal)
    {
        _reminderService = aReminderService;
        _reminderSignal = aReminderSignal;
    }
    protected override async Task ExecuteAsync(CancellationToken aStoppingToken)
    {
        while (!aStoppingToken.IsCancellationRequested)
        {
            _reminderService.LoadExpiringReminderList();
            await _reminderService.CheckForExpiredReminders();

            TimeSpan lNextInterval = _reminderService.GetNextReminderInterval();

            await _reminderSignal.WaitAsync(lNextInterval, aStoppingToken);
        }
    }
}