using DSharpPlus.Entities;
using Microsoft.Data.Sqlite;
public class DatabaseHelper
{
    private static readonly string _folderPath = Path.Combine(AppContext.BaseDirectory, "data");
    private static readonly string _dbPath = Path.Combine(_folderPath, "Messages.db");
    private readonly string _connectionString = $"Data Source={_dbPath}";

    public DatabaseHelper()
    {
        if (!Directory.Exists(_folderPath))
            Directory.CreateDirectory(_folderPath);
        using var lConnection = new SqliteConnection(_connectionString);
        lConnection.Open();

        string lTableCmd = @"CREATE TABLE IF NOT EXISTS Messages(
            MessageID TEXT PRIMARY KEY,
            GuildID TEXT,
            ChannelID TEXT,
            AuthorID TEXT,
            Content TEXT,
            AttachmentCount INTEGER,
            Timestamp TEXT
        )";
        using var createCmd = new SqliteCommand(lTableCmd, lConnection);
        createCmd.ExecuteNonQuery();
    }

    public void SaveMessage(DiscordMessage aMessage)
    {
        using var lConnection = new SqliteConnection(_connectionString);
        lConnection.Open();

        string lInsertCmd = @"INSERT OR REPLACE INTO Messages
            (MessageID, GuildID, ChannelID, AuthorID, Content, AttachmentCount, Timestamp)
            VALUES ($MessageID, $GuildID, $ChannelID, $AuthorID, $Content, $AttachmentCount, $Timestamp)";

        using var lCmd = new SqliteCommand(lInsertCmd, lConnection);
        lCmd.Parameters.AddWithValue("$MessageID", aMessage.Id.ToString());
        lCmd.Parameters.AddWithValue("$GuildID", aMessage.Channel.Guild.Id.ToString());
        lCmd.Parameters.AddWithValue("$ChannelID", aMessage.Channel.Id.ToString());
        lCmd.Parameters.AddWithValue("$AuthorID", aMessage.Author.Id.ToString());
        lCmd.Parameters.AddWithValue("$Content", aMessage.Content);
        lCmd.Parameters.AddWithValue("$AttachmentCount", aMessage.Attachments.Count);
        lCmd.Parameters.AddWithValue("$Timestamp", aMessage.CreationTimestamp.ToString("o"));

        lCmd.ExecuteNonQuery();
    }
    /// <summary>
    /// Returns all messages from the database that share today's date
    /// </summary>
    /// <returns>List of MessageRecords</returns>
    public List<MessageRecord> GetTodaysMsgs(DateTime aDate)
    {
        using var lConnection = new SqliteConnection(_connectionString);
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
                Timestamp = DateTime.Parse(lReader.GetString(lReader.GetOrdinal("Timestamp"))),
                Interestingness = 0
            });
        }

        return lMessages;
    }
}