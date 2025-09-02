namespace DiscordBot.Core.Abstractions;

/// <summary>
/// Defines the contract for handling Discord prefix commands
/// </summary>
public interface ICommandHandler
{
    /// <summary>
    /// Gets the number of loaded command modules
    /// </summary>
    int ModuleCount { get; }

    /// <summary>
    /// Initializes the command handler and loads all command modules
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about all available commands
    /// </summary>
    IReadOnlyCollection<CommandInfo> GetCommands();
}

/// <summary>
/// Represents information about a command
/// </summary>
/// <param name="Name">The name of the command</param>
/// <param name="Description">The description of the command</param>
/// <param name="Module">The module containing the command</param>
public sealed record CommandInfo(
    string Name,
    string? Description,
    string Module);