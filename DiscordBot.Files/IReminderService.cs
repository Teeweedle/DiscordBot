public interface IReminderService
{
    Task LoadExpiringReminderList();
    void RemoveReminder(ReminderRecord aReminder);
    void TrackExpiringReminder(ReminderRecord aReminder);    
}