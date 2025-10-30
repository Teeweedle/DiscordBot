public class OnThisDayServiceTests
{
    private List<MessageRecord>? _messages;
    [SetUp]
    public void Setup()
    {
        _messages = new List<MessageRecord>
        {
            new MessageRecord {
                Content = "Check this out https://example.com/cool.mp4",
                AttachmentCount = 1,
                Timestamp = DateTime.UtcNow,
                MessageID = "1",
                GuildID = "1",
                ChannelID = "1",
                AuthorID = "1",
                Interestingness = 0
            },
            new MessageRecord {
                Content = "Hello world! Just chatting here.",
                AttachmentCount = 0,
                Timestamp = DateTime.UtcNow,
                MessageID = "2",
                GuildID = "1",
                ChannelID = "1",
                AuthorID = "2",
                Interestingness = 0
            },
            new MessageRecord {
                Content = "Look at this funny gif https://funny.com/fun.gif <@123456789012345678>",
                AttachmentCount = 1,
                Timestamp = DateTime.UtcNow,
                MessageID = "3",
                GuildID = "1",
                ChannelID = "1",
                AuthorID = "3",
                Interestingness = 0
            },
            new MessageRecord {
                Content = "Just a plain text message with no links",
                AttachmentCount = 0,
                Timestamp = DateTime.UtcNow,
                MessageID = "4",
                GuildID = "1",
                ChannelID = "1",
                AuthorID = "4",
                Interestingness = 0
            },
            new MessageRecord {
                Content = "Multiple attachments here https://site.com/a.mp3 and https://site.com/b.mp4",
                AttachmentCount = 2,
                Timestamp = DateTime.UtcNow,
                MessageID = "5",
                GuildID = "1",
                ChannelID = "1",
                AuthorID = "5",
                Interestingness = 0
            }
        };
    }
    [Test]  
    public void PostMotD_ReturnsMessageWithHighestInterestingness()  
    {  
        var lService = new OnThisDayService();
        var lMessage = lService.GetMotD(_messages!);
        Console.WriteLine(lMessage!.Content);  
        Assert.That(lMessage!.Content, Is.EqualTo("Multiple attachments here https://site.com/a.mp3 and https://site.com/b.mp4"));  
    }
} 


