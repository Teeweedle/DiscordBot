using DSharpPlus.Entities;

public sealed class ConversationResponse
{
    private static readonly int RespondDelay = 2;
    private readonly CohereClient _cohereClient;
    private readonly Dictionary<ulong, DateTime> _lastBotRespond = new();

    public ConversationResponse(CohereClient aCohereClient)
    {
        _cohereClient = aCohereClient;
    }
    public async Task<string> TryBuildResponse(DiscordMessage aMessage)
    {
        string lPrompt = $"Respond to the following message as if you are another user in Discord but do it " 
            + $"in a playful manner. The reponse should be funny, helpful, and informative "
            + $"(maybe a little sarcastic and flirty): {aMessage.Content}";

        return await _cohereClient.AskAsync(lPrompt);
    }
    /// <summary>
    /// Checks if a bot can respond to a user in a message.
    /// </summary>
    /// <param name="aUser">The user to respond to.</param>
    /// <param name="aMessage">The message to respond to.</param>
    /// <returns>True if the bot can respond, false otherwise.</returns>
    /// <remarks>
    /// This function checks if the bot has responded to the user in the last <see cref="RespondDelay"/> minutes,
    /// if the message is empty, if the message contains a URL, or if the message starts with "http://".
    /// </remarks>
    private bool CanRespond(DiscordUser aUser, DiscordMessage aMessage)
    {            
        //No empty messages
        if (string.IsNullOrWhiteSpace(aMessage.Content))
        {
            Console.WriteLine("Empty message");
            return false;
        }
            
        //No URLs
        if(Uri.IsWellFormedUriString(aMessage.Content.Trim(), UriKind.Absolute))
        {
            Console.WriteLine("No URL");
            return false;
        }
            
        //No http links
        if(aMessage.Content.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("No http link");
            return false;
        }
            
        return true;
    }
}