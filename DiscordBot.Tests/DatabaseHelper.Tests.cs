using Microsoft.Data.Sqlite;

public class DatabaseHelperTests
{
    private DatabaseHelper _testDB;
    [SetUp]
    public void Setup()
    {
        _testDB = new DatabaseHelper("Data Source=file:memdb1?mode=memory&cache=shared");

        using var conn = new SqliteConnection("Data Source=file:memdb1?mode=memory&cache=shared");
        conn.Open();
        string createTable = @"
            CREATE TABLE IF NOT EXISTS ChannelInfo (
                GuildID TEXT PRIMARY KEY,
                WeightedChannelID TEXT,
                MoTDChannelID TEXT
            )";
        using var cmd = new SqliteCommand(createTable, conn);
        cmd.ExecuteNonQuery();

    }
    [Test]
    public void SetWeightedChannel_StoreValues()
    {
        _testDB.SetWeightedChannel("123", "234");
        Assert.That(_testDB.GetWeightedChannelID(), Is.EqualTo("234"));
    }
    [Test]
    public void SetWeightedChannel_UpdateValues()
    {
        _testDB.SetWeightedChannel("123", "234");
        _testDB.SetWeightedChannel("123", "345");
        Assert.That(_testDB.GetWeightedChannelID(), Is.EqualTo("345"));
    }
    [Test]
    public void GetWeightedChannel_NullWhenNotSet()
    {
        var result = _testDB.GetWeightedChannelID();
        Assert.That(result, Is.Null);
    }
    [Test]
    public void SetMotDChannel_StoreValues()
    {
        _testDB.SetMotDChannel("123", "234");
        Assert.That(_testDB.GetMoTDChannelID(), Is.EqualTo("234"));
    }
    [Test]
    public void SetMotDChannel_UpdateValues()
    {
        _testDB.SetMotDChannel("123", "234");
        _testDB.SetMotDChannel("123", "345");
        Assert.That(_testDB.GetMoTDChannelID(), Is.EqualTo("345"));
    }
    [Test]
    public void GetMotDChannel_NullWhenNotSet()
    {
        var result = _testDB.GetMoTDChannelID();
        Assert.That(result, Is.Null);
    }
}