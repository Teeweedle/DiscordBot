class Program
{    
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Bot is starting... ");

        var MOTDService = new OnThisDayService();

        string? token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
            throw new Exception("DISCORD_BOT_TOKEN is not set");

        var bot = new BotService(token);
        if (args.Length > 0 && args[0].Equals("postmotd", StringComparison.OrdinalIgnoreCase))
        {
            bool testMode = args.Length > 1 && args[1].Equals("test", StringComparison.OrdinalIgnoreCase);
            await bot.PostMotDAsync(testMode);
            return;
        }
        await bot.RunAsync();
    }    
}
