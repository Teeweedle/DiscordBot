public interface IReminderService
{
    void LoadExpiringReminderList();
    Task CreateReminder(ulong aUserID, ulong aGuildID, long aAmount, string aDuration, string aMessage, ulong aInteractionID);
    TimeSpan GetNextReminderInterval();
    Task CheckForExpiredReminders();
}