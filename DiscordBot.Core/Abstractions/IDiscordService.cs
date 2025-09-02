namespace DiscordBot.Core.Abstractions;

/// <summary>
/// Defines the contract for the main Discord bot service
/// </summary>
public interface IDiscordService
{
    /// <summary>
    /// Gets whether the bot is currently connected
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the current latency to Discord's servers in milliseconds
    /// </summary>
    int Latency { get; }

    /// <summary>
    /// Gets the number of guilds the bot is in
    /// </summary>
    int GuildCount { get; }

    /// <summary>
    /// Starts the Discord bot service
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the Discord bot service
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the bot's activity status
    /// </summary>
    /// <param name="activity">The activity to set</param>
    /// <param name="status">The user status to set</param>
    Task SetActivityAsync(string activity, BotStatus status = BotStatus.Online);
}

/// <summary>
/// Represents the bot's online status
/// </summary>
public enum BotStatus
{
    Online,
    Idle,
    DoNotDisturb,
    Invisible,
    Offline
}