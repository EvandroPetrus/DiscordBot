using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Core.Abstractions;
using DiscordBot.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBot.Core.Services;

/// <summary>
/// Service responsible for handling Discord prefix commands
/// </summary>
public sealed partial class CommandHandlerService : ICommandHandler
{
    private readonly DiscordSocketClient _client;
    private readonly CommandService _commands;
    private readonly IServiceProvider _services;
    private readonly ILogger<CommandHandlerService> _logger;
    private readonly BotConfiguration _config;

    public int ModuleCount => _commands.Modules.Count();

    public CommandHandlerService(
        DiscordSocketClient client,
        CommandService commands,
        IServiceProvider services,
        ILogger<CommandHandlerService> logger,
        IOptions<BotConfiguration> config)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(config);

        _client = client;
        _commands = commands;
        _services = services;
        _logger = logger;
        _config = config.Value;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!_config.Features.EnablePrefixCommands)
        {
            LogPrefixCommandsDisabled();
            return;
        }

        // Load command modules
        await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);

        // Hook up the message received event
        _client.MessageReceived += HandleCommandAsync;
        _commands.CommandExecuted += OnCommandExecutedAsync;

        LogCommandHandlerInitialized(_commands.Modules.Count(), _commands.Commands.Count());
    }

    public IReadOnlyCollection<Abstractions.CommandInfo> GetCommands()
    {
        return _commands.Modules
            .SelectMany(m => m.Commands)
            .Select(cmd => new Abstractions.CommandInfo(
                cmd.Name,
                cmd.Summary,
                cmd.Module.Name))
            .ToList()
            .AsReadOnly();
    }

    private async Task HandleCommandAsync(SocketMessage messageParam)
    {
        // Don't process system or bot messages
        if (messageParam is not SocketUserMessage message || message.Author.IsBot)
            return;

        // Check if we're in a guild channel
        if (message.Channel is not ITextChannel)
            return;

        int argPos = 0;

        // Check if message has prefix or mentions the bot
        if (!message.HasStringPrefix(_config.Settings.CommandPrefix, ref argPos) &&
            !message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            return;

        // Create command context
        var context = new SocketCommandContext(_client, message);

        // Execute command with timeout
        using var cts = new CancellationTokenSource(_config.Settings.CommandTimeout);

        try
        {
            await _commands.ExecuteAsync(context, argPos, _services);
        }
        catch (OperationCanceledException)
        {
            LogCommandTimeout(message.Content);
            await context.Channel.SendMessageAsync("⏱️ Command execution timed out.");
        }
    }

    private async Task OnCommandExecutedAsync(
        Optional<Discord.Commands.CommandInfo> command,
        ICommandContext context,
        IResult result)
    {
        if (!command.IsSpecified)
            return;

        if (result.IsSuccess)
        {
            if (_config.Features.EnableCommandLogging)
            {
                LogCommandExecuted(
                    command.Value.Name,
                    context.User.Username,
                    context.User.Discriminator,
                    context.Guild?.Name ?? "DM",
                    context.Channel.Name);
            }
            return;
        }

        // Handle errors
        var errorEmbed = new EmbedBuilder()
            .WithTitle("❌ Command Error")
            .WithDescription(result.ErrorReason)
            .WithColor(Color.Red)
            .WithFooter($"Command: {command.Value.Name}")
            .WithCurrentTimestamp()
            .Build();

        await context.Channel.SendMessageAsync(embed: errorEmbed);

        LogCommandFailed(command.Value.Name, result.ErrorReason, context.User.Username, context.Guild?.Name ?? "DM");
    }
    
    // High-performance logging methods
    [LoggerMessage(Level = LogLevel.Information, Message = "Prefix commands are disabled in configuration")]
    private partial void LogPrefixCommandsDisabled();
    
    [LoggerMessage(Level = LogLevel.Information, Message = "Command handler initialized with {Count} modules and {CommandCount} commands")]
    private partial void LogCommandHandlerInitialized(int count, int commandCount);
    
    [LoggerMessage(Level = LogLevel.Warning, Message = "Command execution timed out for message: {Message}")]
    private partial void LogCommandTimeout(string message);
    
    [LoggerMessage(Level = LogLevel.Information, Message = "Command {CommandName} executed successfully by {User}#{Discriminator} in {Guild}/{Channel}")]
    private partial void LogCommandExecuted(string commandName, string user, string discriminator, string guild, string channel);
    
    [LoggerMessage(Level = LogLevel.Error, Message = "Command {CommandName} failed: {Error} | User: {User} | Guild: {Guild}")]
    private partial void LogCommandFailed(string commandName, string error, string user, string guild);
}