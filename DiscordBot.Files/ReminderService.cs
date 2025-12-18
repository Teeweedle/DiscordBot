using DSharpPlus.Entities;
using NUnit.Framework.Constraints;

public class ReminderService : IReminderService
{
    DatabaseHelper _db;
    // public event Action<ReminderRecord>? ReminderNeedsTracking;
    private List<ReminderRecord> _reminders = new List<ReminderRecord>();
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
            TrackExpiringReminder(lReminder);
            // ReminderNeedsTracking?.Invoke(lReminder);
        }
        await Task.Delay(lDuration);
    }
    private bool ShouldTrackReminder(string aExpirationDate)
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
    public async Task LoadExpiringReminderList()
    {
        _reminders = _db.GetExpiringReminders();
        if (_reminders.Count > 0)
        {
            var lInterval = GetNextReminderInterval();
            //start background service ReminderChecker and pass the first interval
        }
    }
    private TimeSpan GetNextReminderInterval()
    {
        return (_reminders[0].ExpirationDate - DateTime.Now).Add(TimeSpan.FromSeconds(1));
    }
    public void CheckForExpiredReminders()
    {
        
    }
    public void SendReminder()
    {
        //send message reminder
        List<ReminderRecord> lExpiredReminders = _reminders
                                            .Where(reminder => reminder.ExpirationDate < DateTime.Now)
                                            .ToList();
        foreach (var lReminder in lExpiredReminders)
        {
            //send message
            //remove from list
            //remove from database
        }

    }
    public void RemoveReminder(ReminderRecord aReminder)
    {
        _reminders.RemoveAll(reminder => reminder.ExpirationDate < DateTime.Now);
        _db.RemoveRemindMe(aReminder.InteractionID);
    } 
    public void TrackExpiringReminder(ReminderRecord aReminder)
    {
        _reminders.Add(aReminder);
        //sort list by earliest date first
        _reminders.Sort((a, b) => a.ExpirationDate.CompareTo(b.ExpirationDate));
    }
}