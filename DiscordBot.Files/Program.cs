using DSharpPlus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

class Program
{    
    public static async Task Main(string[] args)
    {
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
                var lEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                string? lToken = lEnv == "Development" 
                    ? Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN_DEV") 
                    : Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
                if (string.IsNullOrWhiteSpace(lToken))
                    throw new Exception("DISCORD_BOT_TOKEN is not set for current environment.");

                services.AddSingleton(new DiscordClient(new DiscordConfiguration
                {
                    Token = lToken,
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
                    var apiKey = Environment.GetEnvironmentVariable("COHERE_API_KEY") ?? throw new Exception("COHERE_API_KEY is not set");
                    var logger = provider.GetRequiredService<ILogger<CohereClient>>();
                    return new CohereClient(apiKey, logger);
                });
                services.AddSingleton<Messaging>();
                services.AddSingleton<ReminderSignal>();
                services.AddSingleton<ChannelSummaryService>();
                services.AddSingleton<ConversationResponse>();
                services.AddSingleton<BotInfoService>();
                services.AddSingleton<DiscordLookupService>();
                services.AddSingleton<IMessagingService, MessagingService>();
                services.AddSingleton<IReminderNotifier, MessagingService>();
                services.AddSingleton<IReminderService, ReminderService>();
                services.AddSingleton<IChannelScraper, ChannelScraper>();
                services.AddSingleton<IMotdPostingService, MotdService>();
                services.AddSingleton<IFeatureGateService, FeatureGateService>();
                services.AddSingleton<IGuildDataManager, GuildDataManager>();
                services.AddSingleton<IMotdService, MotdService>();

                services.AddHostedService<ReminderChecker>();
                services.AddHostedService<MotdPoster>();
                services.AddHostedService<BotService>();
            });
}
