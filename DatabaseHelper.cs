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
            Timestamp TEXT
        )";
        using var createCmd = new SqliteCommand(lTableCmd, lConnection);
        createCmd.ExecuteNonQuery();
    }

    public void SaveMessage(string aMessageID, string aChannelID, string aAuthorID, string aContent, DateTime aTimeStamp)
    {
        using var lConnection = new SqliteConnection(_connectionString);
        lConnection.Open();

        string lInsertCmd = @"INSERT OR IGNORE INTO Messages
            (MessageID, ChannelID, AuthorID, Content, Timestamp)
            VALUES ($id, $channel, $author, $content, $time)";

        using var lCmd = new SqliteCommand(lInsertCmd, lConnection);
        lCmd.Parameters.AddWithValue("$id", aMessageID);
        lCmd.Parameters.AddWithValue("$channel", aChannelID);
        lCmd.Parameters.AddWithValue("$author", aAuthorID);
        lCmd.Parameters.AddWithValue("$content", aContent);
        lCmd.Parameters.AddWithValue("$time", aTimeStamp);

        lCmd.ExecuteNonQuery();
    }
}