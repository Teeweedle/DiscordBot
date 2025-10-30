
using System.Text.RegularExpressions;

public class OnThisDayTests
{
    private OnThisDay? _otd;
    private MessageRecord? _message;

    [SetUp]
    public void Setup()
    {
        _message = new MessageRecord {
            Content = "This has four words https://www.forfun.gif <@250359835248295948>",
            AttachmentCount = 1,
            Timestamp = DateTime.UtcNow,
            MessageID = "1",
            GuildID = "1",
            ChannelID = "1",
            AuthorID = "1",
            Interestingness = 0
        };
        _otd = new OnThisDay(new List<MessageRecord> { _message! });
    }
    [Test]
    public void ContentWordCount_FourWords_ReturnExpectedScore()
    {
        var matches = OnThisDay.WordCountRegex.Matches(_message!.Content);

        foreach (Match m in matches)
        {
            Console.WriteLine($"Matched: '{m.Value}'");
        }

        float lScore = _otd!.ContentWordCount(_message!.Content);
        Assert.That(lScore, Is.EqualTo(4 * 0.5f));
    }
    [Test]
    public void HasAttachment_OneAttachmentAndContent_ReturnExpectedScore()
    {
        int lAttachmentCount = 1;
        float lScore = _otd!.HasAttachment(lAttachmentCount, _message!.Content);
        Assert.That(lScore, Is.EqualTo(2 * 3));
    }
    [Test]
    public void MentionsUser_OneMentionAndContent_ReturnExpectedScore()
    {
        float lScore = _otd!.MentionsUser(_message!.Content);
        Assert.That(lScore, Is.EqualTo(1 * 0.5f));
    }
    [Test]
    public void GenerateInterestingness_OneMessage_ReturnExpectedScore()
    {        
        _otd!.GenerateInterestingness();
        Assert.That(_message!.Interestingness, Is.EqualTo(8.5f));
    }
}
