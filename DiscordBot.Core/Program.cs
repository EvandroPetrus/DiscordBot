using System;
using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot.Core.Abstractions;
using DiscordBot.Core.Configuration;
using DiscordBot.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/bot-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Discord Bot...");

    var host = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, config) =>
        {
            config.Sources.Clear();
            config.AddConfiguration(configuration);
        })
        .ConfigureServices((context, services) =>
        {
            // Configure bot settings
            services.Configure<BotConfiguration>(
                context.Configuration.GetSection(BotConfiguration.SectionName));

            // Configure Discord.NET
            services.AddSingleton(provider =>
            {
                var config = context.Configuration
                    .GetSection(BotConfiguration.SectionName)
                    .Get<BotConfiguration>() ?? throw new InvalidOperationException("Bot configuration is missing");

                var socketConfig = new DiscordSocketConfig
                {
                    GatewayIntents = ParseGatewayIntents(config.Discord.GatewayIntents),
                    MessageCacheSize = config.Discord.MessageCacheSize,
                    AlwaysDownloadUsers = config.Discord.AlwaysDownloadUsers > 0,
                    AlwaysDownloadDefaultStickers = true,
                    AlwaysResolveStickers = true,
                    LogLevel = LogSeverity.Verbose,
                    ConnectionTimeout = 10000,
                    DefaultRetryMode = RetryMode.AlwaysRetry,
                    UseInteractionSnowflakeDate = false
                };

                return new DiscordSocketClient(socketConfig);
            });

            // Command service configuration
            services.AddSingleton(provider =>
            {
                var commandConfig = new CommandServiceConfig
                {
                    LogLevel = LogSeverity.Verbose,
                    CaseSensitiveCommands = false,
                    DefaultRunMode = Discord.Commands.RunMode.Async,
                    IgnoreExtraArgs = true
                };

                return new CommandService(commandConfig);
            });

            // Interaction service configuration  
            services.AddSingleton(provider =>
            {
                var client = provider.GetRequiredService<DiscordSocketClient>();
                var interactionConfig = new InteractionServiceConfig
                {
                    LogLevel = LogSeverity.Verbose,
                    DefaultRunMode = Discord.Interactions.RunMode.Async,
                    UseCompiledLambda = true
                };

                return new InteractionService(client, interactionConfig);
            });

            // Register services with proper abstractions
            services.AddSingleton<LoggingService>();
            services.AddSingleton<ICommandHandler, CommandHandlerService>();
            services.AddSingleton<IInteractionHandler, InteractionHandlerService>();
            services.AddSingleton<IDiscordService, DiscordBotService>();

            // Register the Discord bot as a hosted service
            services.AddHostedService<DiscordBotService>(provider =>
                (DiscordBotService)provider.GetRequiredService<IDiscordService>());
        })
        .UseSerilog()
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Helper method to parse gateway intents
static GatewayIntents ParseGatewayIntents(string intentsString)
{
    if (string.IsNullOrWhiteSpace(intentsString))
        return GatewayIntents.AllUnprivileged;

    if (Enum.TryParse<GatewayIntents>(intentsString, out var intents))
        return intents;

    // Parse comma-separated intents
    var intentsList = intentsString.Split(',', StringSplitOptions.RemoveEmptyEntries);
    var result = GatewayIntents.None;

    foreach (var intent in intentsList)
    {
        if (Enum.TryParse<GatewayIntents>(intent.Trim(), out var parsedIntent))
            result |= parsedIntent;
    }

    return result == GatewayIntents.None ? GatewayIntents.AllUnprivileged : result;
}
