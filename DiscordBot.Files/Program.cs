using DSharpPlus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

class Program
{    
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Bot is starting... ");

        var lHost = CreateHostBuilder(args).Build();

        await lHost.RunAsync();
    }
    public static IHostBuilder CreateHostBuilder(string[] args) => 
        new HostBuilder()
            .ConfigureLogging(logging =>
            {
                logging.AddFilter("Microsoft.Extensions.Logging.EventLog", LogLevel.None);
                logging.AddConsole();
                logging.AddDebug();
            })
            .ConfigureAppConfiguration((context, config) => 
            {
                config.AddUserSecrets<Program>();    
            })
            .ConfigureServices((context, services) =>
            {
                string? token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
                if (string.IsNullOrWhiteSpace(token))
                    throw new Exception("DISCORD_BOT_TOKEN is not set");

                services.AddSingleton(new DiscordClient(new DiscordConfiguration
                {
                    Token = token,
                    TokenType = TokenType.Bot,
                    Intents = DiscordIntents.Guilds | 
                            DiscordIntents.GuildMessages |
                            DiscordIntents.MessageContents |
                            DiscordIntents.GuildMembers,
                            AlwaysCacheMembers = true,
                            AutoReconnect = true
                }));
                //dependency injection
                services.AddSingleton<DatabaseHelper>();
                services.AddSingleton<HttpClient>();
                services.AddSingleton<CohereClient>(provider =>
                {
                    var config = provider.GetRequiredService<IConfiguration>();
                    var apiKey = config["COHERE_API_KEY"] ?? throw new Exception("COHERE_API_KEY is not set");

                    return new CohereClient(apiKey);
                });
                services.AddSingleton<MessagingService>();
                services.AddSingleton<Messaging>();
                services.AddSingleton<ReminderSignal>();
                services.AddSingleton<MotdService>();
                services.AddSingleton<ChannelSummaryService>();
                services.AddSingleton<ConversationResponse>();
                services.AddSingleton<BotInfoService>();
                services.AddSingleton<DiscordLookupService>();
                services.AddSingleton<ReminderService>();
                services.AddSingleton<IReminderNotifier, MessagingService>();
                services.AddSingleton<IReminderService, ReminderService>();
                services.AddSingleton<IChannelScraper, ChannelScraper>();

                services.AddHostedService<ReminderChecker>();
                services.AddHostedService<BotService>();
            });
}
