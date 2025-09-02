namespace DiscordBot.Core.Configuration;

/// <summary>
/// Root configuration for the Discord bot
/// </summary>
public sealed class BotConfiguration
{
    /// <summary>
    /// Gets the configuration section name
    /// </summary>
    public const string SectionName = "Bot";

    /// <summary>
    /// Discord-specific settings
    /// </summary>
    public DiscordSettings Discord { get; init; } = new();

    /// <summary>
    /// General bot settings
    /// </summary>
    public BotSettings Settings { get; init; } = new();

    /// <summary>
    /// Feature toggles
    /// </summary>
    public FeatureSettings Features { get; init; } = new();

    /// <summary>
    /// Logging configuration
    /// </summary>
    public LoggingSettings Logging { get; init; } = new();
}

/// <summary>
/// Discord API and connection settings
/// </summary>
public sealed class DiscordSettings
{
    /// <summary>
    /// Bot token from Discord Developer Portal
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// Optional guild ID for development (faster command updates)
    /// </summary>
    public ulong? GuildId { get; init; }

    /// <summary>
    /// Number of messages to cache per channel
    /// </summary>
    public int MessageCacheSize { get; init; } = 100;

    /// <summary>
    /// Number of users to always download on startup
    /// </summary>
    public int AlwaysDownloadUsers { get; init; } = 250;

    /// <summary>
    /// Gateway intents configuration
    /// </summary>
    public string GatewayIntents { get; init; } = "AllUnprivileged";
}

/// <summary>
/// General bot behavior settings
/// </summary>
public sealed class BotSettings
{
    /// <summary>
    /// Prefix for text-based commands
    /// </summary>
    public string CommandPrefix { get; init; } = "!";

    /// <summary>
    /// Bot activity/status configuration
    /// </summary>
    public ActivitySettings Activity { get; init; } = new();

    /// <summary>
    /// Command execution timeout in milliseconds
    /// </summary>
    public int CommandTimeout { get; init; } = 3000;
}

/// <summary>
/// Bot activity/presence settings
/// </summary>
public sealed class ActivitySettings
{
    /// <summary>
    /// Activity type (Playing, Streaming, Listening, Watching, Competing)
    /// </summary>
    public string Type { get; init; } = "Playing";

    /// <summary>
    /// Activity description text
    /// </summary>
    public string Name { get; init; } = "with .NET 8";

    /// <summary>
    /// Optional stream URL (only used when Type is Streaming)
    /// </summary>
    public string? StreamUrl { get; init; }
}

/// <summary>
/// Feature toggle settings
/// </summary>
public sealed class FeatureSettings
{
    /// <summary>
    /// Enable slash command support
    /// </summary>
    public bool EnableSlashCommands { get; init; } = true;

    /// <summary>
    /// Enable traditional prefix command support
    /// </summary>
    public bool EnablePrefixCommands { get; init; } = true;

    /// <summary>
    /// Enable interaction support (buttons, modals, etc.)
    /// </summary>
    public bool EnableInteractions { get; init; } = true;

    /// <summary>
    /// Enable automatic reconnection on disconnect
    /// </summary>
    public bool EnableAutoReconnect { get; init; } = true;

    /// <summary>
    /// Enable detailed command execution logging
    /// </summary>
    public bool EnableCommandLogging { get; init; } = true;
}

/// <summary>
/// Logging configuration settings
/// </summary>
public sealed class LoggingSettings
{
    /// <summary>
    /// Minimum log level
    /// </summary>
    public string MinimumLevel { get; init; } = "Information";

    /// <summary>
    /// Enable console logging
    /// </summary>
    public bool EnableConsoleLogging { get; init; } = true;

    /// <summary>
    /// Enable file logging
    /// </summary>
    public bool EnableFileLogging { get; init; } = true;

    /// <summary>
    /// Path template for log files
    /// </summary>
    public string LogFilePath { get; init; } = "logs/bot-.log";

    /// <summary>
    /// Number of log files to retain
    /// </summary>
    public int RetainedFileCountLimit { get; init; } = 7;
}