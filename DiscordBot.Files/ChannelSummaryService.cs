using System.Text;

public sealed class ChannelSummaryService
{
    private readonly DatabaseHelper _dbh;
    private readonly CohereClient _cohereClient;
    public ChannelSummaryService(DatabaseHelper aDb, CohereClient aCohereClient)
    {
        _dbh = aDb;
        _cohereClient = aCohereClient;        
    }
    public async Task<string> GetChannelSummaryAsync(ulong aChannelID)
    {
        List<MessageRecord> lMessages =  _dbh.GetLast24HoursMsgs(DateTime.Now, aChannelID.ToString());

        if(lMessages.Count == 0) return "No messages found in the last 24 hours.";

        StringBuilder lSB = new StringBuilder();
        foreach (var m in lMessages)
        {
            lSB.Append(' ').Append(m.Content.Replace("\n", " "));
        }
        
        string lPrompt = $"Summarize the following text. Format the summary with bullets or list items so it is easy to read "
            + $"and don't include a title, just the summary: {lSB.ToString()}";

        return await _cohereClient.AskAsync(lPrompt);     
    } 
}