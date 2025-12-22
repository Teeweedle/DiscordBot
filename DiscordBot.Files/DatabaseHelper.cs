using System.Globalization;
using DSharpPlus.Entities;
using Microsoft.Data.Sqlite;
public class DatabaseHelper
{
    private static readonly string _folderPath = Path.GetFullPath(
                                                    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "data"));
    private static readonly string _messagesDBPath = Path.Combine(_folderPath, "Messages.db");
    private static readonly string _channelInfoDBPath = Path.Combine(_folderPath, "ChannelInfo.db");
    private static readonly string _webhookInfoDBPath = Path.Combine(_folderPath, "WebhookInfo.db");
    private static readonly string _targetUserAndChannelDBPath = Path.Combine(_folderPath, "TargetUserAndChannel.db");
    private static readonly string _remindMeDBPath = Path.Combine(_folderPath, "RemindMe.db");
    private readonly string _messagesConnectionString = $"Data Source={_messagesDBPath}";
    private string _channelInfoConnectionString = $"Data Source={_channelInfoDBPath}";
    private string _webhookInfoConnectionString = $"Data Source={_webhookInfoDBPath}";
    private string _targetUserAndChannelConnectionString = $"Data Source={_targetUserAndChannelDBPath}";
    private string _remindMeConnectionString = $"Data Source={_remindMeDBPath}";
    private const string _dateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public DatabaseHelper(string? aConnectionString = null)
    {
        _channelInfoConnectionString = aConnectionString ?? _channelInfoConnectionString;

        if (aConnectionString == null)
        {
            if (!Directory.Exists(_folderPath))
                Directory.CreateDirectory(_folderPath);
            using var lConnection = new SqliteConnection(_messagesConnectionString);
            lConnection.Open();
    
            string lTableCmd = @"CREATE TABLE IF NOT EXISTS Messages(
                MessageID TEXT PRIMARY KEY,
                GuildID TEXT,
                ChannelID TEXT,
                AuthorID TEXT,
                Content TEXT,
                AttachmentCount INTEGER,
                ReactionCount INTEGER,
                Timestamp TEXT
            )";
            using var createCmd = new SqliteCommand(lTableCmd, lConnection);
            createCmd.ExecuteNonQuery();
        }
    }
    public void SaveMessage(DiscordMessage aMessage)
    {
        using var lConnection = new SqliteConnection(_messagesConnectionString);
        lConnection.Open();

        string lInsertCmd = @"INSERT OR REPLACE INTO Messages
            (MessageID, GuildID, ChannelID, AuthorID, Content, AttachmentCount, ReactionCount, Timestamp)
            VALUES ($MessageID, $GuildID, $ChannelID, $AuthorID, $Content, $AttachmentCount, $ReactionCount, $Timestamp)";

        using var lCmd = new SqliteCommand(lInsertCmd, lConnection);
        lCmd.Parameters.AddWithValue("$MessageID", aMessage.Id.ToString());
        lCmd.Parameters.AddWithValue("$GuildID", aMessage.Channel.Guild.Id.ToString());
        lCmd.Parameters.AddWithValue("$ChannelID", aMessage.Channel.Id.ToString());
        lCmd.Parameters.AddWithValue("$AuthorID", aMessage.Author.Id.ToString());
        lCmd.Parameters.AddWithValue("$Content", aMessage.Content);
        lCmd.Parameters.AddWithValue("$AttachmentCount", aMessage.Attachments.Count);

        
        lCmd.Parameters.AddWithValue("$ReactionCount", aMessage.Reactions.Count);
        lCmd.Parameters.AddWithValue("$Timestamp", 
                    aMessage.CreationTimestamp.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"));

        lCmd.ExecuteNonQuery();
    }
    /// <summary>
    /// Returns all messages from the database that share today's date
    /// </summary>
    /// <returns>List of MessageRecords</returns>
    public List<MessageRecord> GetTodaysMsgs(DateTime aCurrentDate)
    {
        using var lConnection = new SqliteConnection(_messagesConnectionString);
        Console.WriteLine($"Connection string: {_messagesConnectionString}");
        lConnection.Open();

        List<int> lYearsWithMessages = GetYearsWithMessages(lConnection, aCurrentDate);
        
        List<MessageRecord> lMessages = new List<MessageRecord>();
        if (lYearsWithMessages.Count == 0)//If there are no years with messages
            return new List<MessageRecord>();
            
        var lRNG = new Random();
        var lYear = lYearsWithMessages[lRNG.Next(lYearsWithMessages.Count)];

        using var lCmd = lConnection.CreateCommand();
        lCmd.CommandText = @"
                            SELECT *
                            FROM Messages
                            WHERE strftime('%m', Timestamp) = $Month
                                AND strftime('%d', Timestamp) = $Day
                                AND strftime('%Y', Timestamp) = $Year
                                AND (LENGTH(Content) > 3 OR AttachmentCount > 0)";
        lCmd.Parameters.AddWithValue("$Month", aCurrentDate.Month.ToString("D2")); 
        lCmd.Parameters.AddWithValue("$Day", aCurrentDate.Day.ToString("D2"));
        lCmd.Parameters.AddWithValue("$Year", lYear.ToString());
        
        using var lReader = lCmd.ExecuteReader();
        while (lReader.Read())
        {
            lMessages.Add(new MessageRecord
            {
                MessageID = lReader.GetString(lReader.GetOrdinal("MessageID")),
                GuildID = lReader.GetString(lReader.GetOrdinal("GuildID")),
                ChannelID = lReader.GetString(lReader.GetOrdinal("ChannelID")),
                AuthorID = lReader.GetString(lReader.GetOrdinal("AuthorID")),
                Content = lReader.GetString(lReader.GetOrdinal("Content")),
                AttachmentCount = lReader.GetInt32(lReader.GetOrdinal("AttachmentCount")),
                ReactionCount = lReader.GetInt32(lReader.GetOrdinal("ReactionCount")),
                Timestamp = DateTime.Parse(lReader.GetString(lReader.GetOrdinal("Timestamp")), 
                                                null, System.Globalization.DateTimeStyles.AssumeLocal)
            });
        }      

        return lMessages;
    }
    /// <summary>
    /// Returns a list of years that have messages exluding the current year
    /// </summary>
    /// <param name="aConnection"></param>
    /// <param name="aCurrentDate">The date to check</param>
    /// <returns></returns>
    private List<int> GetYearsWithMessages(SqliteConnection aConnection, DateTime aCurrentDate)
    {        
        var lYearsWithMessages = new List<int>();
        using(var lCmd = aConnection.CreateCommand())
        {
            lCmd.CommandText = @"
                                SELECT DISTINCT strftime('%Y', Timestamp) as Year
                                FROM Messages
                                WHERE strftime('%m', Timestamp) = $Month
                                    AND strftime('%d', Timestamp) = $Day
                                    And strftime('%Y', Timestamp) != $CurrentYear
                                ORDER BY Random()";
            lCmd.Parameters.AddWithValue("$Month", aCurrentDate.Month.ToString("D2"));
            lCmd.Parameters.AddWithValue("$Day", aCurrentDate.Day.ToString("D2"));
            lCmd.Parameters.AddWithValue("$CurrentYear", aCurrentDate.Year.ToString());

            using var lYearReader = lCmd.ExecuteReader();
            while (lYearReader.Read())
            {
                lYearsWithMessages.Add(int.Parse(lYearReader.GetString(0)));
            }
        }
        return lYearsWithMessages;
    }
    public List<MessageRecord> GetLast24HoursMsgs(DateTime aCurrentDate, string aChannelID)
    {
        List<MessageRecord> lMessageList = new List<MessageRecord>();
        using SqliteConnection lConnection = new SqliteConnection(_messagesConnectionString);
        lConnection.Open();

        using SqliteCommand lCmd = lConnection.CreateCommand();
        lCmd.CommandText = @"
                            SELECT *
                            FROM Messages
                            WHERE Timestamp >= $Cutoff
                            AND ChannelID = $ChannelID";
        DateTime lCutoff = aCurrentDate.ToUniversalTime().AddDays(-1);
        lCmd.Parameters.AddWithValue("$Cutoff", lCutoff.ToString("yyyy-MM-dd HH:mm:ss"));
        lCmd.Parameters.AddWithValue("$ChannelID", aChannelID);
        using var lReader = lCmd.ExecuteReader();
        while (lReader.Read())
        {
            MessageRecord lMessage = new MessageRecord
            {
                MessageID = lReader.GetString(lReader.GetOrdinal("MessageID")),
                GuildID = lReader.GetString(lReader.GetOrdinal("GuildID")),
                ChannelID = lReader.GetString(lReader.GetOrdinal("ChannelID")),
                AuthorID = lReader.GetString(lReader.GetOrdinal("AuthorID")),
                Content = lReader.GetString(lReader.GetOrdinal("Content")),
                AttachmentCount = lReader.GetInt32(lReader.GetOrdinal("AttachmentCount")),
                ReactionCount = lReader.GetInt32(lReader.GetOrdinal("ReactionCount")),
                Timestamp = DateTime.Parse(lReader.GetString(lReader.GetOrdinal("Timestamp")), 
                                                null, DateTimeStyles.AssumeLocal)
            };
            lMessageList.Add(lMessage);
        }
        return lMessageList;
    }
    /// <summary>
    /// Sets MOTD channel for this guild. Saves it to database located in data folder ChannelInfo.db
    /// </summary>
    /// <param name="aGuildID">Discord guild ID</param>
    /// <param name="aChannelID">MOTD channel ID</param>
    public void SetMotDChannel(string aGuildID, string aChannelID)
    {
        using var lConnection = new SqliteConnection(_channelInfoConnectionString);
        lConnection.Open();

        CheckChannelTableExists(lConnection);

        string lInsertCmd = @"
            INSERT INTO ChannelInfo (GuildID, MoTDChannelID) 
            VALUES ($GuildID, $MoTDChannelID) 
            ON CONFLICT (GuildID) DO UPDATE SET MoTDChannelID = $MoTDChannelID";

        using var lCmd = new SqliteCommand(lInsertCmd, lConnection);

        lCmd.Parameters.AddWithValue("$GuildID", aGuildID);
        lCmd.Parameters.AddWithValue("$MoTDChannelID", aChannelID);
        lCmd.ExecuteNonQuery();
    }
    /// <summary>
    /// Sets weighted channel to value messages at some X% more likely to be picked. Saves them to database located in data folder ChannelInfo.db
    /// </summary>
    /// <param name="aGuildID">Discord guild ID</param>
    /// <param name="aChannelID">Weighted channel ID</param>
    public void SetWeightedChannel(string aGuildID, string aChannelID)
    {
        using var lConnection = new SqliteConnection(_channelInfoConnectionString);
        lConnection.Open();

        CheckChannelTableExists(lConnection);

        string lInsertCmd = @"
            INSERT INTO ChannelInfo (GuildID, WeightedChannelID) 
            VALUES ($GuildID, $WeightedChannelID) 
            ON CONFLICT (GuildID) DO UPDATE SET WeightedChannelID = $WeightedChannelID";

        using var lCmd = new SqliteCommand(lInsertCmd, lConnection);

        lCmd.Parameters.AddWithValue("$GuildID", aGuildID);
        lCmd.Parameters.AddWithValue("$WeightedChannelID", aChannelID);
        lCmd.ExecuteNonQuery();
    }
    /// <summary>
    /// Returns weighted channel ID from ChannelInfo.db
    /// </summary>
    /// <returns></returns>
    public string? GetWeightedChannelID()
    {
        using var lConnection = new SqliteConnection(_channelInfoConnectionString);
        lConnection.Open();

        string lSql = @"
            SELECT WeightedChannelID 
            FROM ChannelInfo 
            LIMIT 1";
            
        using var lCmd = new SqliteCommand(lSql, lConnection);
        
        var result = lCmd.ExecuteScalar();
        return result?.ToString();          
    }
    /// <summary>
    /// Returns MOTD channel ID from ChannelInfo.db
    /// </summary>
    /// <returns></returns>
    public string? GetMoTDChannelID()
    {
        using var lConnection = new SqliteConnection(_channelInfoConnectionString);
        lConnection.Open();

        string lSql = @"
            SELECT MoTDChannelID 
            FROM ChannelInfo
            LIMIT 1";
            
        using var lCmd = new SqliteCommand(lSql, lConnection);
        
        var result = lCmd.ExecuteScalar();
        return result?.ToString();
    }
    private void CheckChannelTableExists(SqliteConnection aConnection)
    {
      string lTableCmd = @"
            CREATE TABLE IF NOT EXISTS ChannelInfo (
                GuildID TEXT PRIMARY KEY,
                WeightedChannelID TEXT,
                MoTDChannelID TEXT,
                TLDRChannelID TEXT
            )";
        using var createCmd = new SqliteCommand(lTableCmd, aConnection);
        createCmd.ExecuteNonQuery();
    }
    private void CheckWebHookTableExists(SqliteConnection aConnection)
    {
      string lTableCmd = @"
            CREATE TABLE IF NOT EXISTS WebhookInfo (
                GuildID TEXT,
                ChannelID TEXT,
                WebhookID TEXT,
                WebhookToken TEXT,
                PRIMARY KEY (GuildID, ChannelID)
            )";
        using var createCmd = new SqliteCommand(lTableCmd, aConnection);
        createCmd.ExecuteNonQuery();
    }
    public void SaveWebHook(string aGuildID, string aChannelID, string aWebhookID, string aWebhookToken)
    {
        using var lConnection = new SqliteConnection(_webhookInfoConnectionString);
        lConnection.Open();

        CheckWebHookTableExists(lConnection);

        string lInsertCmd = @"
            INSERT INTO WebhookInfo (GuildID, ChannelID, WebhookID, WebhookToken) 
            VALUES ($GuildID, $ChannelID, $WebhookID, $WebhookToken) 
            ON CONFLICT (GuildID, ChannelID) 
            DO UPDATE SET WebhookID = $WebhookID, WebhookToken = $WebhookToken";

        using var lCmd = new SqliteCommand(lInsertCmd, lConnection);

        lCmd.Parameters.AddWithValue("$GuildID", aGuildID);
        lCmd.Parameters.AddWithValue("$ChannelID", aChannelID);
        lCmd.Parameters.AddWithValue("$WebhookID", aWebhookID);
        lCmd.Parameters.AddWithValue("$WebhookToken", aWebhookToken);
        lCmd.ExecuteNonQuery();
    }
    /// <summary>
    /// Returns webhook ID from WebhookInfo.db
    /// </summary>
    /// <param name="aGuildID"></param>
    /// <param name="aChannelID"></param>
    /// <returns></returns>
    public string? GetWebHookID(string aGuildID, string aChannelID)
    {
        using var lConnection = new SqliteConnection(_webhookInfoConnectionString);
        lConnection.Open();

        CheckWebHookTableExists(lConnection);

        string lSql = @"
            SELECT WebhookID 
            FROM WebhookInfo
            WHERE GuildID = $GuildID AND ChannelID = $ChannelID";
            
        using var lCmd = new SqliteCommand(lSql, lConnection);
        lCmd.Parameters.AddWithValue("$GuildID", aGuildID);
        lCmd.Parameters.AddWithValue("$ChannelID", aChannelID);
        
        var result = lCmd.ExecuteScalar();
        return result?.ToString();
    }
    /// <summary>
    /// Returns webhook token from WebhookInfo.db
    /// </summary>
    /// <param name="aGuildID"></param>
    /// <param name="aChannelID"></param>
    /// <returns></returns>
    public string? GetWebHookToken(string aGuildID, string aChannelID)
    {
        using var lConnection = new SqliteConnection(_webhookInfoConnectionString);
        lConnection.Open();

        string lSql = @"
            SELECT WebhookToken 
            FROM WebhookInfo
            WHERE GuildID = $GuildID AND ChannelID = $ChannelID";
            
        using var lCmd = new SqliteCommand(lSql, lConnection);
        lCmd.Parameters.AddWithValue("$GuildID", aGuildID);
        lCmd.Parameters.AddWithValue("$ChannelID", aChannelID);
        
        var result = lCmd.ExecuteScalar();
        return result?.ToString();
    }
    public void SetTargetUserAndChannel(string aUserID, string aChannelID)
    {
        using var lConnection = new SqliteConnection(_targetUserAndChannelConnectionString);
        lConnection.Open();

        CheckIfTargetUserAndChannelSet(lConnection);

        string lInsertCmd = @"
            INSERT INTO TargetUserAndChannel (ID, UserID, ChannelID) 
            VALUES (1, $UserID, $ChannelID) 
            ON CONFLICT (ID) DO UPDATE SET 
                UserID = $UserID, 
                ChannelID = $ChannelID";

        using var lCmd = new SqliteCommand(lInsertCmd, lConnection);

        lCmd.Parameters.AddWithValue("$UserID", aUserID);
        lCmd.Parameters.AddWithValue("$ChannelID", aChannelID);
        lCmd.ExecuteNonQuery();
    }
    public string? GetTargetChannelID()
    {
        using var lConnection = new SqliteConnection(_targetUserAndChannelConnectionString);
        lConnection.Open();

        string lSql = @"
            SELECT ChannelID 
            FROM TargetUserAndChannel";
            
        using var lCmd = new SqliteCommand(lSql, lConnection);
        
        var result = lCmd.ExecuteScalar();
        return result?.ToString();
    }
    public string? GetTargetUserID()
    {
        using var lConnection = new SqliteConnection(_targetUserAndChannelConnectionString);
        lConnection.Open();

        string lSql = @"
            SELECT UserID 
            FROM TargetUserAndChannel";
            
        using var lCmd = new SqliteCommand(lSql, lConnection);
        
        var result = lCmd.ExecuteScalar();
        return result?.ToString();
    }
    public void CheckIfTargetUserAndChannelSet(SqliteConnection aConnection)
    {
        string lTableCmd = @"
            CREATE TABLE IF NOT EXISTS TargetUserAndChannel (
                ID INTEGER PRIMARY KEY Check (ID = 1),
                UserID TEXT NOT NULL,
                ChannelID TEXT NOT NULL
            )";
        using var createCmd = new SqliteCommand(lTableCmd, aConnection);
        createCmd.ExecuteNonQuery();
    }
    public void SaveRemindMe(ReminderRecord aReminder)
    {
        using var lConnection = new SqliteConnection(_remindMeConnectionString);
        lConnection.Open();

        CheckIfRemindMeTableExists(lConnection);

        string lInsertCmd = @"
            INSERT INTO RemindMeMessage (InteractionID, UserID, GuildID, ExpirationDate, Message) 
            VALUES ($InteractionID, $UserID, $GuildID, $ExpirationDate, $Message)";

        using var lCmd = new SqliteCommand(lInsertCmd, lConnection);

        lCmd.Parameters.AddWithValue("$InteractionID", (long)aReminder.InteractionID);
        lCmd.Parameters.AddWithValue("$UserID", (long)aReminder.UserID);
        lCmd.Parameters.AddWithValue("$GuildID", aReminder.GuildID);
        lCmd.Parameters.AddWithValue("$ExpirationDate", aReminder.ExpirationDate.ToString("yyyy-MM-dd HH:mm:ss"));
        lCmd.Parameters.AddWithValue("$Message", aReminder.Message);
        lCmd.ExecuteNonQuery();
    }
    public void RemoveRemindMe(ulong aInteractionID)
    {
        using var lConnection = new SqliteConnection(_remindMeConnectionString);
        lConnection.Open();

        string lSql = @"
            DELETE FROM RemindMeMessage
            WHERE InteractionID = $InteractionID";
            
        using var lCmd = new SqliteCommand(lSql, lConnection);
        lCmd.Parameters.AddWithValue("$InteractionID", (long)aInteractionID);
        lCmd.ExecuteNonQuery();
    }
    /// <summary>
    /// Retrieves a list of reminders that are due to expire within the next 24 hours.
    /// </summary>
    /// <returns>A list of <see cref="ReminderRecord"/> containing reminders that are due to expire within the next 24 hours.</returns>
    public List<ReminderRecord> GetExpiringReminders()
    {
        using var lConnection = new SqliteConnection(_remindMeConnectionString);        
        lConnection.Open();

        CheckIfRemindMeTableExists(lConnection);

        string lTableCmd = @"
            SELECT InteractionID, UserID, GuildID, ExpirationDate, Message 
            FROM RemindMeMessage
            WHERE ExpirationDate <= datetime('now', '+24 hours')";
            
        using var lCmd = new SqliteCommand(lTableCmd, lConnection);
        using var lReader = lCmd.ExecuteReader();

        var lInteractionIDOrdinal = lReader.GetOrdinal("InteractionID");
        var lUserIDOrdinal = lReader.GetOrdinal("UserID");
        var lGuildIDOrdinal = lReader.GetOrdinal("GuildID");
        var lExpirationDateOrdinal = lReader.GetOrdinal("ExpirationDate");
        var lMessageOrdinal = lReader.GetOrdinal("Message");

        List<ReminderRecord> lReminders = new List<ReminderRecord>();
        while (lReader.Read())
        {
            var lReminder = new ReminderRecord
            {
                InteractionID = (ulong)lReader.GetInt64(lInteractionIDOrdinal),
                UserID = (ulong)lReader.GetInt64(lUserIDOrdinal),
                GuildID = (ulong)lReader.GetInt64(lGuildIDOrdinal),
                ExpirationDate = ParseDateTime(lReader.GetString(lExpirationDateOrdinal)),
                Message = lReader.GetString(lMessageOrdinal)
            };
            lReminders.Add(lReminder);
        }
        return lReminders;
    }
    public void CheckIfRemindMeTableExists(SqliteConnection aConnection)
    {
        string lTableCmd = @"
            CREATE TABLE IF NOT EXISTS RemindMeMessage (
                InteractionID INTEGER PRIMARY KEY,
                UserID INTEGER NOT NULL,
                GuildID INTEGER NOT NULL,
                ExpirationDate TEXT NOT NULL,
                Message TEXT NOT NULL
            )";
        using var createCmd = new SqliteCommand(lTableCmd, aConnection);
        createCmd.ExecuteNonQuery();
    }
    public DateTime ParseDateTime(string aDateTimeString)
    {        
        return DateTime.ParseExact(
                aDateTimeString, 
                _dateTimeFormat, 
                CultureInfo.InvariantCulture, 
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }
}