namespace DiscordBot.Core.Abstractions;

/// <summary>
/// Defines the contract for Discord bot service operations
/// </summary>
public interface IDiscordBotService
{
    /// <summary>
    /// Gets the current connection state of the bot
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the bot's username
    /// </summary>
    string? BotUsername { get; }

    /// <summary>
    /// Gets the bot's ID
    /// </summary>
    ulong? BotId { get; }

    /// <summary>
    /// Sets the bot's activity status
    /// </summary>
    Task SetActivityAsync(string activity, ActivityType type = ActivityType.Playing, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the bot's online status
    /// </summary>
    Task SetStatusAsync(UserStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about connected guilds
    /// </summary>
    Task<IReadOnlyCollection<GuildInfo>> GetGuildsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents basic guild information
/// </summary>
public sealed record GuildInfo(ulong Id, string Name, int MemberCount);

/// <summary>
/// Represents activity types for the bot
/// </summary>
public enum ActivityType
{
    Playing,
    Streaming,
    Listening,
    Watching,
    Competing
}

/// <summary>
/// Represents user status
/// </summary>
public enum UserStatus
{
    Online,
    Idle,
    DoNotDisturb,
    Invisible,
    Offline
}