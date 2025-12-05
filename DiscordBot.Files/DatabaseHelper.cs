using DSharpPlus.Entities;
using Microsoft.Data.Sqlite;
public class DatabaseHelper
{
    private static readonly string _folderPath = Path.Combine(AppContext.BaseDirectory, "data");
    private static readonly string _messagesDBPath = Path.Combine(_folderPath, "Messages.db");
    private static readonly string _channelInfoDBPath = Path.Combine(_folderPath, "ChannelInfo.db");
    private static readonly string _webhookInfoDBPath = Path.Combine(_folderPath, "WebhookInfo.db");
    private readonly string _messagesConnectionString = $"Data Source={_messagesDBPath}";
    private string _channelInfoConnectionString = $"Data Source={_channelInfoDBPath}";
    private string _webhookInfoConnectionString = $"Data Source={_webhookInfoDBPath}";

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
                                                null, System.Globalization.DateTimeStyles.AssumeLocal)
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
    /// Sets TLDR channel for this guild. Saves it to database located in data folder ChannelInfo.db. 
    /// AI will post a summary of messages from General in this channel
    /// <param name="aGuildID"></param>
    /// <param name="aChannelID">Channel to post TLDR</param>
    public void SetTLDRChannel(string aGuildID, string aChannelID)
    {
        using var lConnection = new SqliteConnection(_channelInfoConnectionString);
        lConnection.Open();

        CheckChannelTableExists(lConnection);

        string lInsertCmd = @"
            INSERT INTO ChannelInfo (GuildID, TLDRChannelID) 
            VALUES ($GuildID, $TLDRChannelID) 
            ON CONFLICT (GuildID) DO UPDATE SET TLDRChannelID = $TLDRChannelID";

        using var lCmd = new SqliteCommand(lInsertCmd, lConnection);

        lCmd.Parameters.AddWithValue("$GuildID", aGuildID);
        lCmd.Parameters.AddWithValue("$TLDRChannelID", aChannelID);
        lCmd.ExecuteNonQuery();
    }
    public string? GetTLDRChannelID()
    {
        using var lConnection = new SqliteConnection(_channelInfoConnectionString);
        lConnection.Open();

        string lSql = @"
            SELECT TLDRChannelID 
            FROM ChannelInfo 
            LIMIT 1";
            
        using var lCmd = new SqliteCommand(lSql, lConnection);
        
        var result = lCmd.ExecuteScalar();
        return result?.ToString();        
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
}