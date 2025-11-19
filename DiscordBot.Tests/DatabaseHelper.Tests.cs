public class DatabaseHelperTests
{
    private DatabaseHelper _testDB;
    [SetUp]
    public void Setup()
    {
        // _testDB = new DatabaseHelper("Data Source=:memory:");

    }
    [Test]
    public void GetWeightedChannelID_NoWeightedChannel_ReturnsNull() => Assert.That(new DatabaseHelper().GetWeightedChannelID("1"), Is.Null);
}