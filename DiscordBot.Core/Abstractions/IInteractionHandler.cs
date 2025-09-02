namespace DiscordBot.Core.Abstractions;

/// <summary>
/// Defines the contract for handling Discord interactions (slash commands, buttons, etc.)
/// </summary>
public interface IInteractionHandler
{
    /// <summary>
    /// Gets the number of loaded interaction modules
    /// </summary>
    int ModuleCount { get; }

    /// <summary>
    /// Initializes the interaction handler and loads all interaction modules
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers all slash commands with Discord
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task RegisterCommandsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about all available interactions
    /// </summary>
    IReadOnlyCollection<InteractionInfo> GetInteractions();
}

/// <summary>
/// Represents the type of interaction
/// </summary>
public enum InteractionType
{
    SlashCommand,
    UserCommand,
    MessageCommand,
    Button,
    SelectMenu,
    Modal
}

/// <summary>
/// Represents information about an interaction
/// </summary>
/// <param name="Name">The name of the interaction</param>
/// <param name="Description">The description of the interaction</param>
/// <param name="Type">The type of the interaction</param>
public sealed record InteractionInfo(
    string Name,
    string? Description,
    InteractionType Type);