using DSharpPlus.Entities;
using Microsoft.Data.Sqlite;
using NUnit.Framework.Constraints;
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
        lCmd.Parameters.AddWithValue("$Timestamp", aMessage.CreationTimestamp.ToString("o"));

        lCmd.ExecuteNonQuery();
    }
    /// <summary>
    /// Returns all messages from the database that share today's date
    /// </summary>
    /// <returns>List of MessageRecords</returns>
    public List<MessageRecord> GetTodaysMsgs(DateTime aDate)
    {
        var lStartUTC = new DateTime(aDate.Year, aDate.Month, aDate.Day, 0, 0, 0, DateTimeKind.Utc);
        var lEndUTC = lStartUTC.AddDays(1);

        using var lConnection = new SqliteConnection(_messagesConnectionString);
        lConnection.Open();

        var lYearsWithMessages = GetYearsWithMessages(lConnection, lStartUTC, lEndUTC);
        
        var lMessages = new List<MessageRecord>();
        if (lYearsWithMessages.Count == 0)//If there are no years with messages
            return new List<MessageRecord>();
            
        var lRNG = new Random();
        var lYear = lYearsWithMessages[lRNG.Next(lYearsWithMessages.Count)];

        using var lCmd = lConnection.CreateCommand();
        lCmd.CommandText = @"
                            SELECT *
                            FROM Messages
                            WHERE strftime('%m', Timestamp, 'utc') = $Month
                                AND strftime('%d', Timestamp, 'utc') = $Day
                                AND strftime('%Y', Timestamp, 'utc') = $Year
                                AND (LENGTH(Content) > 3 OR AttachmentCount > 0)";
        lCmd.Parameters.AddWithValue("$Month", aDate.Month.ToString("D2")); 
        lCmd.Parameters.AddWithValue("$Day", aDate.Day.ToString("D2"));
        lCmd.Parameters.AddWithValue("$Year", lYear);
        
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
                                                null, System.Globalization.DateTimeStyles.AdjustToUniversal)
            });
        }      

        return lMessages;
    }
    private List<int> GetYearsWithMessages(SqliteConnection aConnection, DateTime aStartUTC, DateTime aEndUTC)
    {
        
        var lYearsWithMessages = new List<int>();

        using(var lCmd = aConnection.CreateCommand())
        {
            lCmd.CommandText = @"
                                SELECT DISTINCT strftime('%Y', Timestamp) as Year
                                FROM Messages
                                WHERE Timestamp BETWEEN $start AND $end
                                ORDER BY Random()";
            lCmd.Parameters.AddWithValue("$start", aStartUTC.ToString("yyyy-MM-ddTHH:mm:ss"));
            lCmd.Parameters.AddWithValue("$end", aEndUTC.ToString("yyyy-MM-ddTHH:mm:ss"));

            using var lYearReader = lCmd.ExecuteReader();
            while (lYearReader.Read())
            {
                lYearsWithMessages.Add(int.Parse(lYearReader.GetString(0)));
            }
        }
        return lYearsWithMessages;
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
                MoTDChannelID TEXT
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
    public string? GetWebHookID(string aGuildID, string aChannelID)
    {
        using var lConnection = new SqliteConnection(_webhookInfoConnectionString);
        lConnection.Open();

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