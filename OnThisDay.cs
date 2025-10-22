
public class OnThisDay
{
    private readonly DatabaseHelper _db = new();
    private List<MessageRecord> _messages = new List<MessageRecord>();

    public OnThisDay() => _messages = _db.GetMoD();

    public void GenerateHeuristic()
    {
        foreach (var message in _messages)
        {
            
        }
            
    }
}