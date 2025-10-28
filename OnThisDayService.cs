public class OnThisDayService
{
    public readonly DatabaseHelper _dbh = new DatabaseHelper();
    public MessageRecord GetMOD(DateTime aDate)
    {
        List<MessageRecord> lMessages = _dbh.GetTodaysMsgs(aDate);
        var lMOTD = new OnThisDay(lMessages);
        lMOTD.GenerateInterestingness();

        var lBestMsg = lMessages
            .OrderByDescending(m => m.Interestingness)
            .FirstOrDefault();
        Console.WriteLine($"Best message: {lBestMsg.Interestingness}");
        return lBestMsg;
    }
}