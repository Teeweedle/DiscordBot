
public class ChannelSummary
{
    private readonly GroqClient _groq;

    public ChannelSummary(string aApiKey)
    {
        _groq = new GroqClient(aApiKey);   
    }

    public Task<string> AskAsync(List<MessageRecord> aMessages)
    {
        string lContent = string.Empty;
        foreach (var m in aMessages)
        {
            lContent += m.Content.Replace("\n", " ");
        }
        string lPrompt = $"Summarize the following text: {lContent}";
        return _groq.AskAsync(lPrompt);    
    }    
}