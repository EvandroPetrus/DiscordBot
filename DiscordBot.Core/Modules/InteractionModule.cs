using Discord;
using Discord.Interactions;
using DiscordBot.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DiscordBot.Core.Modules;

/// <summary>
/// Slash commands interaction module
/// </summary>
[Group("bot", "Bot management and information commands")]
public sealed class InteractionModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IDiscordService _discordService;
    private readonly ILogger<InteractionModule> _logger;

    public InteractionModule(IDiscordService discordService, ILogger<InteractionModule> logger)
    {
        _discordService = discordService ?? throw new ArgumentNullException(nameof(discordService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [SlashCommand("ping", "Check the bot's response time")]
    public async Task PingAsync()
    {
        var embed = new EmbedBuilder()
            .WithTitle("🏓 Pong!")
            .WithDescription($"**Latency:** {_discordService.Latency}ms\n**Status:** {GetStatusEmoji(_discordService.Latency)}")
            .WithColor(GetLatencyColor(_discordService.Latency))
            .WithFooter($"Requested by {Context.User.Username}", Context.User.GetAvatarUrl())
            .WithCurrentTimestamp()
            .Build();

        await RespondAsync(embed: embed, ephemeral: false);
        _logger.LogDebug("Ping slash command executed by {User}", Context.User.Username);
    }

    [SlashCommand("status", "Get detailed bot status")]
    public async Task StatusAsync()
    {
        var process = Process.GetCurrentProcess();
        var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();
        var memoryUsage = process.WorkingSet64 / (1024.0 * 1024.0); // Convert to MB

        var embed = new EmbedBuilder()
            .WithTitle("📊 Bot Status")
            .WithColor(Color.Blue)
            .AddField("Connection", _discordService.IsConnected ? "✅ Connected" : "❌ Disconnected", inline: true)
            .AddField("Latency", $"{_discordService.Latency}ms", inline: true)
            .AddField("Guilds", _discordService.GuildCount.ToString(), inline: true)
            .AddField("Uptime", FormatUptime(uptime), inline: true)
            .AddField("Memory Usage", $"{memoryUsage:F2} MB", inline: true)
            .AddField("CPU Cores", Environment.ProcessorCount.ToString(), inline: true)
            .WithFooter($"Process ID: {process.Id}")
            .WithCurrentTimestamp()
            .Build();

        await RespondAsync(embed: embed, ephemeral: false);
    }

    [SlashCommand("invite", "Get the bot invite link")]
    public async Task InviteAsync()
    {
        var application = await Context.Client.GetApplicationInfoAsync();
        var permissions = new GuildPermissions(
            addReactions: true,
            viewChannel: true,
            sendMessages: true,
            embedLinks: true,
            attachFiles: true,
            readMessageHistory: true,
            useExternalEmojis: true,
            connect: true,
            speak: true,
            useVoiceActivation: true
        );

        var inviteUrl = $"https://discord.com/api/oauth2/authorize?client_id={application.Id}&permissions={permissions.RawValue}&scope=bot%20applications.commands";

        var components = new ComponentBuilder()
            .WithButton("Invite Bot", style: ButtonStyle.Link, url: inviteUrl, emote: new Emoji("🔗"))
            .Build();

        var embed = new EmbedBuilder()
            .WithTitle("🤖 Invite Bot")
            .WithDescription("Click the button below to invite me to your server!")
            .WithColor(Color.Green)
            .AddField("Required Permissions",
                "• Send Messages\n" +
                "• Embed Links\n" +
                "• Read Message History\n" +
                "• Use Slash Commands",
                inline: true)
            .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl())
            .Build();

        await RespondAsync(embed: embed, components: components, ephemeral: false);
    }

    [SlashCommand("help", "Get help with bot commands")]
    public async Task HelpAsync(
        [Summary("category", "Command category to get help for")]
        [Choice("General", "general")]
        [Choice("Moderation", "moderation")]
        [Choice("Fun", "fun")]
        [Choice("Utility", "utility")]
        string? category = null)
    {
        var embed = new EmbedBuilder()
            .WithTitle("📚 Bot Help")
            .WithColor(Color.Blue)
            .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());

        if (string.IsNullOrEmpty(category))
        {
            embed.WithDescription("Select a category to view available commands:")
                .AddField("General", "Basic bot commands and information", inline: false)
                .AddField("Moderation", "Server moderation tools (Admin only)", inline: false)
                .AddField("Fun", "Entertainment and game commands", inline: false)
                .AddField("Utility", "Useful tools and utilities", inline: false);
        }
        else
        {
            embed.WithDescription($"Commands in **{char.ToUpper(category[0]) + category[1..]}** category:");

            var commands = category switch
            {
                "general" => "• `/bot ping` - Check bot latency\n• `/bot status` - Get bot status\n• `/bot invite` - Get invite link",
                "moderation" => "• Coming soon...",
                "fun" => "• Coming soon...",
                "utility" => "• Coming soon...",
                _ => "No commands available in this category."
            };

            embed.AddField("Available Commands", commands);
        }

        embed.WithFooter("Use /help [category] to see specific commands")
            .WithCurrentTimestamp();

        await RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    [ComponentInteraction("help_button:*")]
    public async Task HandleHelpButton(string category)
    {
        await DeferAsync(ephemeral: true);
        await HelpAsync(category);
    }

    [SlashCommand("settings", "Configure bot settings")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task SettingsAsync()
    {
        var components = new ComponentBuilder()
            .WithSelectMenu("settings_menu", new List<SelectMenuOptionBuilder>
            {
                new SelectMenuOptionBuilder("Prefix", "prefix", "Change the command prefix"),
                new SelectMenuOptionBuilder("Logging", "logging", "Configure logging settings"),
                new SelectMenuOptionBuilder("Features", "features", "Toggle bot features")
            }, "Select a setting to configure")
            .Build();

        var embed = new EmbedBuilder()
            .WithTitle("⚙️ Bot Settings")
            .WithDescription("Select a setting category from the dropdown below:")
            .WithColor(Color.DarkBlue)
            .Build();

        await RespondAsync(embed: embed, components: components, ephemeral: true);
    }

    [ComponentInteraction("settings_menu")]
    public async Task HandleSettingsMenu(string[] selections)
    {
        var selection = selections.FirstOrDefault();

        var embed = new EmbedBuilder()
            .WithTitle($"⚙️ {char.ToUpper(selection![0]) + selection[1..]} Settings")
            .WithColor(Color.DarkBlue);

        embed.WithDescription(selection switch
        {
            "prefix" => "Command prefix configuration (requires implementation)",
            "logging" => "Logging settings configuration (requires implementation)",
            "features" => "Feature toggle configuration (requires implementation)",
            _ => "Unknown setting selected"
        });

        await RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    private static Color GetLatencyColor(int latency) => latency switch
    {
        < 100 => Color.Green,
        < 200 => Color.Gold,
        < 500 => Color.Orange,
        _ => Color.Red
    };

    private static string GetStatusEmoji(int latency) => latency switch
    {
        < 100 => "🟢 Excellent",
        < 200 => "🟡 Good",
        < 500 => "🟠 Fair",
        _ => "🔴 Poor"
    };

    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
            return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
        if (uptime.TotalHours >= 1)
            return $"{uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
        if (uptime.TotalMinutes >= 1)
            return $"{uptime.Minutes}m {uptime.Seconds}s";

        return $"{uptime.Seconds}s";
    }
}