
class Program
{    static async Task Main(string[] args)
    {
        //for testing
        var MOTDService = new OnThisDayService();
        var today = DateTime.UtcNow.Date;

        string? token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
            throw new Exception("DISCORD_BOT_TOKEN is not set");

        var bot = new BotService(token);

        await bot.RunAsync();
        
        //for testing
        //await bot.PostMOTDAsync();
    }    
}

