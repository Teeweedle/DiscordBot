using DSharpPlus.Entities;

public class ReminderService
{
    DatabaseHelper _db;
    public event Action<ReminderRecord>? ReminderNeedsTracking;
    public ReminderService(DatabaseHelper aDb)
    {
        _db = aDb;
    }
    public async Task CreateReminder(ulong aUserID, long aAmount, string aDuration, string aMessage, ulong aInteractionID)
    {
        var lDuration = ParseDuration(aDuration, aAmount);
        var lReminderExpiration = DateTime.Now + lDuration;
        ReminderRecord lReminder = new ReminderRecord
        {
            UserID = aUserID,
            ExpirationDate = lReminderExpiration,
            Message = aMessage,
            InteractionID = aInteractionID
        };
        _db.SaveRemindMe(lReminder);
        if(ShouldTrackReminder(lReminder.ExpirationDate.ToString()))
        {
            ReminderNeedsTracking?.Invoke(lReminder);
        }
        await Task.Delay(lDuration);
    }
    public bool ShouldTrackReminder(string aExpirationDate)
    {
        return DateTime.Parse(aExpirationDate) - DateTime.Now < TimeSpan.FromHours(24);
    }
    private TimeSpan ParseDuration (string aDuration, long aAmount)
    {
        return aDuration switch
        {
            "s" => TimeSpan.FromSeconds(aAmount),
            "m" => TimeSpan.FromMinutes(aAmount),
            "h" => TimeSpan.FromHours(aAmount),
            "d" => TimeSpan.FromDays(aAmount),
            "mo" => TimeSpan.FromDays(aAmount * 30),
            "y" => TimeSpan.FromDays(aAmount * 365),
            _ => TimeSpan.Zero
        };
    }
    public async Task<List<ReminderRecord>> LoadReminderList() => _db.CheckRemindMeForTracking();
}