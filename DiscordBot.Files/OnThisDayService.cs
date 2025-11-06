public class OnThisDayService
{
    /// <summary>
    /// Returns the MessageRecord with the highest interestingness
    /// </summary>
    /// <param name="aMessages">A list of messages from this day any year</param>
    /// <returns></returns>
    public MessageRecord GetMotD(List<MessageRecord> aMessages, string aWeightedChannelID) { 
        
        var lMOTD = new OnThisDay(aMessages);
        lMOTD.GenerateInterestingness(aWeightedChannelID);

        var lBestMsg = aMessages
            .OrderByDescending(m => m.Interestingness)
            .FirstOrDefault();
        Console.WriteLine($"Best Interestingness message - {lBestMsg!.Interestingness} \n" +
                            $"The message is - {lBestMsg.Content}\n" +
                            $"The attachment count is - {lBestMsg.AttachmentCount}");
        return lBestMsg;
    }
}