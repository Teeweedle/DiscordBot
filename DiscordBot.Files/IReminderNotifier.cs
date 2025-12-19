public interface IReminderNotifier
{
    Task SendReminderAsync(ReminderRecord aReminderRecord);
}