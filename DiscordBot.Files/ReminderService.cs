
public class ReminderService : IReminderService
{
    private DatabaseHelper _db;
    private readonly IReminderNotifier _reminderNotifier;
    private readonly ReminderSignal _reminderSignal;
    private readonly Messaging _messaging;
    private List<ReminderRecord> _reminders = new List<ReminderRecord>();
    public ReminderService(DatabaseHelper aDb, 
                            IReminderNotifier aReminderNotifier, 
                            ReminderSignal aReminderSignal, 
                            Messaging aMessaging)
    {
        _db = aDb;
        _reminderNotifier = aReminderNotifier;
        _reminderSignal = aReminderSignal;
        _messaging = aMessaging;
    }
    public async Task CreateReminder(ulong aUserID, ulong aGuildID, long aAmount, string aDuration, string aMessage, ulong aInteractionID)
    {
        var lDuration = ParseDuration(aDuration, aAmount);
        var lReminderExpiration = DateTime.Now + lDuration;
        ReminderRecord lReminder = new ReminderRecord
        {
            UserID = aUserID,
            GuildID = aGuildID,
            ExpirationDate = lReminderExpiration,
            Message = aMessage,
            InteractionID = aInteractionID
        };
        _db.SaveRemindMe(lReminder);
        if(ShouldTrackReminder(lReminder.ExpirationDate))
        {
            TrackExpiringReminder(lReminder);
            _reminderSignal.WakeUp();
        }
    }
    private bool ShouldTrackReminder(DateTime aExpirationDate)
    {
        return aExpirationDate - DateTime.Now < TimeSpan.FromHours(24);
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
    public void LoadExpiringReminderList()
    {
        _reminders = _db.GetExpiringReminders();
    }
    public TimeSpan GetNextReminderInterval()
    {    
        if (_reminders.Count == 0) return TimeSpan.FromHours(24);

        return (_reminders[0].ExpirationDate - DateTime.Now).Add(TimeSpan.FromSeconds(1));
    }
    public async Task CheckForExpiredReminders()
    {
        List<ReminderRecord> lExpiredReminders = _reminders
                                            .Where(reminder => reminder.ExpirationDate < DateTime.Now)
                                            .ToList();
        foreach (var lReminder in lExpiredReminders)
        {
            await _messaging.SendDMToUserAsync(lReminder);
            DeleteReminder(lReminder);
        }
    }    
    /// <summary>
    /// Removes the reminder from the list of tracked reminders and from the database
    /// </summary>
    /// <param name="aReminder">The reminder to remove</param>
    private void DeleteReminder(ReminderRecord aReminder)
    {
        _reminders.Remove(aReminder);//Check if right
        _db.RemoveRemindMe(aReminder.InteractionID);
    } 
    private void TrackExpiringReminder(ReminderRecord aReminder)
    {
        _reminders.Add(aReminder);
        //sort list by earliest date first
        _reminders.Sort((a, b) => a.ExpirationDate.CompareTo(b.ExpirationDate));
    }
}