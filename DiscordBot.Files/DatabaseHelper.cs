using DSharpPlus.Entities;
using Microsoft.Data.Sqlite;
public class DatabaseHelper
{
    private static readonly string _folderPath = Path.Combine(AppContext.BaseDirectory, "data");
    private static readonly string _messagesDBPath = Path.Combine(_folderPath, "Messages.db");
    private static readonly string _channelInfoDBPath = Path.Combine(_folderPath, "ChannelInfo.db");
    private readonly string _messagesConnectionString = $"Data Source={_messagesDBPath}";
    private string _channelInfoConnectionString = $"Data Source={_channelInfoDBPath}";

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
        using var lConnection = new SqliteConnection(_messagesConnectionString);
        lConnection.Open();
        List<MessageRecord> lMessages = new List<MessageRecord>();

        //select all messages from today (m/d) and any year but this year
        string lSql = @"SELECT * FROM Messages
                            WHERE strftime('%m-%d', Timestamp) = strftime('%m-%d', $date)
                            AND strftime('%Y', Timestamp) = (
                                    SELECT strftime('%Y', Timestamp)
                                    FROM Messages
                                    WHERE strftime('%m-%d', Timestamp) = strftime('%m-%d', $date)
                                    AND strftime('%Y', Timestamp) != strftime('%Y', 'now')
                                    ORDER BY RANDOM()
                                    LIMIT 1
                                )
                            AND LENGTH(Content) > 3";
        using var lcmd = new SqliteCommand(lSql, lConnection);
        lcmd.Parameters.AddWithValue("$date", aDate.ToString("yyyy-MM-dd"));

        using var lReader = lcmd.ExecuteReader();
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
                Timestamp = DateTime.Parse(lReader.GetString(lReader.GetOrdinal("Timestamp"))),
                Interestingness = 0
            });
        }

        return lMessages;
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
}