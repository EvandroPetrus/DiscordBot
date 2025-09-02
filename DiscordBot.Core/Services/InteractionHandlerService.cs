using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot.Core.Abstractions;
using DiscordBot.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBot.Core.Services;

/// <summary>
/// Service responsible for handling Discord interactions (slash commands, buttons, etc.)
/// </summary>
public sealed class InteractionHandlerService : IInteractionHandler
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly ILogger<InteractionHandlerService> _logger;
    private readonly BotConfiguration _config;

    public int ModuleCount => _interactions.Modules.Count;

    public InteractionHandlerService(
        DiscordSocketClient client,
        InteractionService interactions,
        IServiceProvider services,
        ILogger<InteractionHandlerService> logger,
        IOptions<BotConfiguration> config)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(interactions);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(config);

        _client = client;
        _interactions = interactions;
        _services = services;
        _logger = logger;
        _config = config.Value;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!_config.Features.EnableSlashCommands)
        {
            _logger.LogInformation("Slash commands are disabled in configuration");
            return;
        }

        // Add interaction modules
        await _interactions.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);

        // Hook up events
        _client.InteractionCreated += HandleInteractionAsync;
        _interactions.SlashCommandExecuted += SlashCommandExecutedAsync;
        _interactions.ContextCommandExecuted += ContextCommandExecutedAsync;
        _interactions.ComponentCommandExecuted += ComponentCommandExecutedAsync;

        _logger.LogInformation("Interaction handler initialized with {Count} modules containing {CommandCount} slash commands",
            _interactions.Modules.Count,
            _interactions.SlashCommands.Count);
    }

    public async Task RegisterCommandsAsync(CancellationToken cancellationToken = default)
    {
        if (!_config.Features.EnableSlashCommands)
        {
            _logger.LogInformation("Skipping slash command registration - disabled in configuration");
            return;
        }

        try
        {
            if (_config.Discord.GuildId.HasValue && _config.Discord.GuildId.Value != 0)
            {
                // Register commands to a specific guild for faster updates during development
                await _interactions.RegisterCommandsToGuildAsync(_config.Discord.GuildId.Value);
                _logger.LogInformation("Registered {Count} commands to guild {GuildId}",
                    _interactions.SlashCommands.Count,
                    _config.Discord.GuildId);
            }
            else
            {
                // Register commands globally (takes up to 1 hour to propagate)
                await _interactions.RegisterCommandsGloballyAsync();
                _logger.LogInformation("Registered {Count} commands globally (may take up to 1 hour to propagate)",
                    _interactions.SlashCommands.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register interaction commands");
            throw;
        }
    }

    public IReadOnlyCollection<InteractionInfo> GetInteractions()
    {
        var interactions = new List<InteractionInfo>();

        // Add slash commands
        interactions.AddRange(_interactions.SlashCommands.Select(cmd =>
            new InteractionInfo(
                cmd.Name,
                cmd.Description,
                Abstractions.InteractionType.SlashCommand)));

        // Add context commands (user and message commands)
        interactions.AddRange(_interactions.ContextCommands.Select(cmd =>
            new InteractionInfo(
                cmd.Name,
                null,
                Abstractions.InteractionType.UserCommand))); // Default to UserCommand for context commands

        return interactions.AsReadOnly();
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(_client, interaction);

            // Execute with timeout
            using var cts = new CancellationTokenSource(_config.Settings.CommandTimeout);
            await _interactions.ExecuteCommandAsync(context, _services);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Interaction {InteractionId} timed out", interaction.Id);

            if (!interaction.HasResponded)
            {
                await interaction.RespondAsync("⏱️ Command execution timed out.", ephemeral: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling interaction {InteractionId} of type {InteractionType}",
                interaction.Id,
                interaction.Type);

            try
            {
                if (!interaction.HasResponded)
                {
                    await interaction.RespondAsync("❌ An error occurred while processing your request.", ephemeral: true);
                }
                else
                {
                    await interaction.FollowupAsync("❌ An error occurred while processing your request.", ephemeral: true);
                }
            }
            catch (Exception followupEx)
            {
                _logger.LogError(followupEx, "Failed to send error message for interaction {InteractionId}", interaction.Id);
            }
        }
    }

    private Task SlashCommandExecutedAsync(SlashCommandInfo info, IInteractionContext context, IResult result)
    {
        LogCommandResult("Slash command", info.Name, context, result);
        return Task.CompletedTask;
    }

    private Task ContextCommandExecutedAsync(ContextCommandInfo info, IInteractionContext context, IResult result)
    {
        LogCommandResult("Context command", info.Name, context, result);
        return Task.CompletedTask;
    }

    private Task ComponentCommandExecutedAsync(ComponentCommandInfo info, IInteractionContext context, IResult result)
    {
        LogCommandResult("Component command", info.Name, context, result);
        return Task.CompletedTask;
    }

    private void LogCommandResult(string commandType, string commandName, IInteractionContext context, IResult result)
    {
        if (result.IsSuccess)
        {
            if (_config.Features.EnableCommandLogging)
            {
                _logger.LogInformation("{CommandType} '{CommandName}' executed successfully by {User}#{Discriminator} in {Guild}/{Channel}",
                    commandType,
                    commandName,
                    context.User.Username,
                    context.User.Discriminator,
                    context.Guild?.Name ?? "DM",
                    context.Channel?.Name ?? "Unknown");
            }
        }
        else
        {
            _logger.LogError("{CommandType} '{CommandName}' failed: {Error} | User: {User} | Guild: {Guild}",
                commandType,
                commandName,
                result.ErrorReason,
                context.User.Username,
                context.Guild?.Name ?? "DM");
        }
    }
}