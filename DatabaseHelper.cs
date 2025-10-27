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

        string lInsertCmd = @"INSERT OR IGNORE INTO Messages
            (MessageID, ChannelID, AuthorID, Content, AttachmentCount, Timestamp)
            VALUES ($MessageID, $ChannelID, $AuthorID, $Content, $AttachmentCount, $Timestamp)";

        using var lCmd = new SqliteCommand(lInsertCmd, lConnection);
        lCmd.Parameters.AddWithValue("$MessageID", aMessage.Id.ToString());
        lCmd.Parameters.AddWithValue("$ChannelID", aMessage.Channel.Id.ToString());
        lCmd.Parameters.AddWithValue("$AuthorID", aMessage.Author.Id.ToString());
        lCmd.Parameters.AddWithValue("$Content", aMessage.Content);
        lCmd.Parameters.AddWithValue("$AttachmentCount", aMessage.Attachments.Count);
        lCmd.Parameters.AddWithValue("$Timestamp", aMessage.CreationTimestamp.UtcDateTime.ToString("o"));

        lCmd.ExecuteNonQuery();
    }
    /// <summary>
    /// Returns all messages from the database that share today's date
    /// </summary>
    /// <returns>List of MessageRecords</returns>
    public List<MessageRecord> GetTodaysMsgs()
    {
        using var lConnection = new SqliteConnection(_connectionString);
        lConnection.Open();
        List<MessageRecord> lMessages = new List<MessageRecord>();

        string lSql = @"SELECT * FROM Messages
                        WHERE date(Timestamp) = $date
                        and LENGTH(Content > 3)
                        and content NOT LIKE '%/%'
                        and content NOT LIKE '%!%')";
        using var lcmd = new SqliteCommand(lSql, lConnection);
        lcmd.Parameters.AddWithValue("$date", DateTime.Today);

        using var lReader = lcmd.ExecuteReader();
        while (lReader.Read())
        {
            lMessages.Add(new MessageRecord
            {
                MessageID = lReader.GetOrdinal("MessageID").ToString(),
                ChannelID = lReader.GetOrdinal("ChannelID").ToString(),
                AuthorID = lReader.GetOrdinal("AuthorID").ToString(),
                Content = lReader.GetOrdinal("Content").ToString(),
                AttachmentCount = Convert.ToInt32(lReader.GetOrdinal("AttachmentCount")),
                Timestamp = DateTime.Parse(lReader.GetString(lReader.GetOrdinal("Timestamp"))),
                Interestingness = 0
            });
        }

        return lMessages;
    }
}