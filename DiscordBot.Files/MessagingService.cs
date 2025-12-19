using DSharpPlus;
using DSharpPlus.EventArgs;

public class MessagingService : IReminderNotifier
{
    private readonly DiscordClient _discord;
    private readonly DatabaseHelper _dbh;
    private readonly Messaging _messaging;
    
    public MessagingService(DiscordClient aDiscord, 
                            DatabaseHelper aDatabaseHelper, 
                            Messaging aMessaging)                            
    {
        _discord = aDiscord;
        _dbh = aDatabaseHelper;
        _messaging = aMessaging;

        _discord.MessageCreated += OnMessageCreatedAsync;
    }
    public async Task OnMessageCreatedAsync(DiscordClient sender, MessageCreateEventArgs e) 
    {
        if (!e.Author.IsBot)
            _dbh.SaveMessage(e.Message);
        if(string.Equals(e.Author.Id.ToString(), _dbh.GetTargetUserID(), StringComparison.Ordinal) &&
            string.Equals(e.Channel.Id.ToString(), _dbh.GetTargetChannelID(), StringComparison.Ordinal))
        {
            await _messaging.RespondToUser(e.Message, e.Channel, e.Author);
        }                
    } 

    public Task SendReminderAsync(ReminderRecord aReminderRecord)
    {
        throw new NotImplementedException();
    }
}