using Discord;
using Discord.WebSocket;
using DiscordBot.Core.Abstractions;
using DiscordBot.Core.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBot.Core.Services;

/// <summary>
/// Main Discord bot service responsible for connection management and lifecycle
/// </summary>
public sealed class DiscordBotService : BackgroundService, IDiscordService
{
    private readonly DiscordSocketClient _client;
    private readonly ICommandHandler _commandHandler;
    private readonly IInteractionHandler _interactionHandler;
    private readonly ILogger<DiscordBotService> _logger;
    private readonly BotConfiguration _config;
    private readonly SemaphoreSlim _startStopSemaphore = new(1, 1);

    public bool IsConnected => _client.ConnectionState == ConnectionState.Connected;
    public int Latency => _client.Latency;
    public int GuildCount => _client.Guilds.Count;

    public DiscordBotService(
        DiscordSocketClient client,
        ICommandHandler commandHandler,
        IInteractionHandler interactionHandler,
        ILogger<DiscordBotService> logger,
        IOptions<BotConfiguration> config)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(commandHandler);
        ArgumentNullException.ThrowIfNull(interactionHandler);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(config);

        _client = client;
        _commandHandler = commandHandler;
        _interactionHandler = interactionHandler;
        _logger = logger;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await StartAsync(stoppingToken);

            // Keep the service running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
            _logger.LogInformation("Discord bot service was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in Discord bot service");
            throw;
        }
    }

    public new async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _startStopSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected)
            {
                _logger.LogWarning("Bot is already connected");
                return;
            }

            _logger.LogInformation("Starting Discord bot service...");

            // Subscribe to client events
            SubscribeToEvents();

            // Initialize handlers
            await InitializeHandlersAsync(cancellationToken);

            // Login and start
            await LoginAndStartAsync(cancellationToken);

            _logger.LogInformation("Discord bot service started successfully");
        }
        finally
        {
            _startStopSemaphore.Release();
        }
    }

    public new async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _startStopSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (!IsConnected)
            {
                _logger.LogWarning("Bot is not connected");
                return;
            }

            _logger.LogInformation("Stopping Discord bot service...");

            UnsubscribeFromEvents();

            await _client.SetStatusAsync(Discord.UserStatus.Offline);
            await _client.LogoutAsync();
            await _client.StopAsync();

            _logger.LogInformation("Discord bot service stopped successfully");
        }
        finally
        {
            _startStopSemaphore.Release();
        }
    }

    public async Task SetActivityAsync(string activity, BotStatus status = BotStatus.Online)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot set activity - bot is not connected");
            return;
        }

        var discordStatus = status switch
        {
            BotStatus.Online => Discord.UserStatus.Online,
            BotStatus.Idle => Discord.UserStatus.Idle,
            BotStatus.DoNotDisturb => Discord.UserStatus.DoNotDisturb,
            BotStatus.Invisible => Discord.UserStatus.Invisible,
            BotStatus.Offline => Discord.UserStatus.Offline,
            _ => Discord.UserStatus.Online
        };

        await _client.SetGameAsync(activity);
        await _client.SetStatusAsync(discordStatus);

        _logger.LogDebug("Updated bot activity to '{Activity}' with status {Status}", activity, status);
    }

    private void SubscribeToEvents()
    {
        _client.Ready += OnReadyAsync;
        _client.Connected += OnConnectedAsync;
        _client.Disconnected += OnDisconnectedAsync;
        _client.JoinedGuild += OnJoinedGuildAsync;
        _client.LeftGuild += OnLeftGuildAsync;

        if (_config.Features.EnableAutoReconnect)
        {
            _client.Disconnected += HandleAutoReconnectAsync;
        }
    }

    private void UnsubscribeFromEvents()
    {
        _client.Ready -= OnReadyAsync;
        _client.Connected -= OnConnectedAsync;
        _client.Disconnected -= OnDisconnectedAsync;
        _client.JoinedGuild -= OnJoinedGuildAsync;
        _client.LeftGuild -= OnLeftGuildAsync;

        if (_config.Features.EnableAutoReconnect)
        {
            _client.Disconnected -= HandleAutoReconnectAsync;
        }
    }

    private async Task InitializeHandlersAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(
            _commandHandler.InitializeAsync(cancellationToken),
            _interactionHandler.InitializeAsync(cancellationToken)
        );
    }

    private async Task LoginAndStartAsync(CancellationToken cancellationToken)
    {
        await _client.LoginAsync(TokenType.Bot, _config.Discord.Token);
        await _client.StartAsync();
    }

    private async Task OnReadyAsync()
    {
        _logger.LogInformation(
            "Bot is ready! Logged in as {Username}#{Discriminator} ({Id})",
            _client.CurrentUser.Username,
            _client.CurrentUser.Discriminator,
            _client.CurrentUser.Id);

        // Set initial bot activity
        await SetInitialActivityAsync();

        // Register slash commands
        await _interactionHandler.RegisterCommandsAsync();

        LogBotStatistics();
    }

    private async Task SetInitialActivityAsync()
    {
        var activityType = _config.Settings.Activity.Type.ToUpperInvariant() switch
        {
            "PLAYING" => Discord.ActivityType.Playing,
            "STREAMING" => Discord.ActivityType.Streaming,
            "LISTENING" => Discord.ActivityType.Listening,
            "WATCHING" => Discord.ActivityType.Watching,
            "COMPETING" => Discord.ActivityType.Competing,
            _ => Discord.ActivityType.Playing
        };

        var activity = activityType == Discord.ActivityType.Streaming && !string.IsNullOrWhiteSpace(_config.Settings.Activity.StreamUrl)
            ? new StreamingGame(_config.Settings.Activity.Name, _config.Settings.Activity.StreamUrl)
            : new Game(_config.Settings.Activity.Name, activityType);

        await _client.SetActivityAsync(activity);
        await _client.SetStatusAsync(Discord.UserStatus.Online);
    }

    private void LogBotStatistics()
    {
        var totalUsers = _client.Guilds.Sum(g => g.MemberCount);
        var totalChannels = _client.Guilds.Sum(g => g.Channels.Count);

        _logger.LogInformation(
            "Connected to {GuildCount} guilds with {UserCount} total users and {ChannelCount} channels",
            GuildCount,
            totalUsers,
            totalChannels);

        _logger.LogInformation(
            "Loaded {CommandModules} command modules and {InteractionModules} interaction modules",
            _commandHandler.ModuleCount,
            _interactionHandler.ModuleCount);
    }

    private Task OnConnectedAsync()
    {
        _logger.LogInformation("Bot connected to Discord gateway (Latency: {Latency}ms)", Latency);
        return Task.CompletedTask;
    }

    private Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is not null)
        {
            _logger.LogWarning(exception, "Bot disconnected from Discord");
        }
        else
        {
            _logger.LogInformation("Bot disconnected from Discord");
        }
        return Task.CompletedTask;
    }

    private Task OnJoinedGuildAsync(SocketGuild guild)
    {
        _logger.LogInformation(
            "Joined guild: {GuildName} ({GuildId}) with {MemberCount} members",
            guild.Name,
            guild.Id,
            guild.MemberCount);
        return Task.CompletedTask;
    }

    private Task OnLeftGuildAsync(SocketGuild guild)
    {
        _logger.LogInformation(
            "Left guild: {GuildName} ({GuildId})",
            guild.Name,
            guild.Id);
        return Task.CompletedTask;
    }

    private async Task HandleAutoReconnectAsync(Exception? exception)
    {
        if (exception is null || IsConnected)
            return;

        _logger.LogWarning("Attempting automatic reconnection in 5 seconds...");

        await Task.Delay(TimeSpan.FromSeconds(5));

        try
        {
            await _client.StartAsync();
            _logger.LogInformation("Successfully reconnected to Discord");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconnect to Discord");
        }
    }

    public override void Dispose()
    {
        _startStopSemaphore?.Dispose();
        base.Dispose();
    }
}