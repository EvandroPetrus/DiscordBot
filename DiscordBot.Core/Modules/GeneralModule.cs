using System.Diagnostics;
using Discord;
using Discord.Commands;
using DiscordBot.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Core.Modules;

/// <summary>
/// General purpose commands module
/// </summary>
[Name("General")]
[Summary("General bot commands")]
public sealed class GeneralModule : ModuleBase<SocketCommandContext>
{
    private readonly IDiscordService _discordService;
    private readonly ILogger<GeneralModule> _logger;

    public GeneralModule(IDiscordService discordService, ILogger<GeneralModule> logger)
    {
        _discordService = discordService ?? throw new ArgumentNullException(nameof(discordService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Command("ping")]
    [Summary("Check the bot's latency")]
    [Alias("pong", "latency")]
    public async Task PingAsync()
    {
        var embed = new EmbedBuilder()
            .WithTitle("🏓 Pong!")
            .WithDescription($"Latency: **{_discordService.Latency}ms**")
            .WithColor(GetLatencyColor(_discordService.Latency))
            .WithFooter($"Requested by {Context.User.Username}", Context.User.GetAvatarUrl())
            .WithCurrentTimestamp()
            .Build();

        await ReplyAsync(embed: embed);
        _logger.LogDebug("Ping command executed by {User} with latency {Latency}ms",
            Context.User.Username, _discordService.Latency);
    }

    [Command("help")]
    [Summary("Display help information")]
    public async Task HelpAsync([Remainder] string? command = null)
    {
        var embed = new EmbedBuilder()
            .WithTitle("📚 Bot Help")
            .WithColor(Color.Blue)
            .WithFooter($"Requested by {Context.User.Username}", Context.User.GetAvatarUrl())
            .WithCurrentTimestamp();

        if (string.IsNullOrWhiteSpace(command))
        {
            embed.WithDescription("Use `/help [command]` to get detailed information about a specific command.");
            embed.AddField("Available Commands",
                "• `ping` - Check bot latency\n" +
                "• `info` - Get bot information\n" +
                "• `uptime` - Check bot uptime\n" +
                "• `invite` - Get bot invite link");
        }
        else
        {
            embed.WithDescription($"Help for command: **{command}**");
            embed.AddField("Command Not Found",
                $"No detailed help available for '{command}' yet.");
        }

        await ReplyAsync(embed: embed.Build());
    }

    [Command("info")]
    [Summary("Display bot information")]
    [Alias("about", "botinfo")]
    public async Task InfoAsync()
    {
        var application = await Context.Client.GetApplicationInfoAsync();
        var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();

        var embed = new EmbedBuilder()
            .WithTitle("ℹ️ Bot Information")
            .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl())
            .WithColor(Color.Blue)
            .AddField("Bot Name", Context.Client.CurrentUser.Username, inline: true)
            .AddField("Bot ID", Context.Client.CurrentUser.Id, inline: true)
            .AddField("Owner", $"{application.Owner.Username}#{application.Owner.Discriminator}", inline: true)
            .AddField("Servers", _discordService.GuildCount, inline: true)
            .AddField("Latency", $"{_discordService.Latency}ms", inline: true)
            .AddField("Uptime", FormatUptime(uptime), inline: true)
            .AddField("Framework", ".NET 8.0", inline: true)
            .AddField("Library", "Discord.Net", inline: true)
            .AddField("Version", "1.0.0", inline: true)
            .WithFooter($"Requested by {Context.User.Username}", Context.User.GetAvatarUrl())
            .WithCurrentTimestamp()
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("uptime")]
    [Summary("Check how long the bot has been running")]
    public async Task UptimeAsync()
    {
        var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();

        var embed = new EmbedBuilder()
            .WithTitle("⏱️ Bot Uptime")
            .WithDescription($"The bot has been running for: **{FormatUptime(uptime)}**")
            .WithColor(Color.Green)
            .AddField("Started At", Process.GetCurrentProcess().StartTime.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss UTC"))
            .WithFooter($"Requested by {Context.User.Username}", Context.User.GetAvatarUrl())
            .WithCurrentTimestamp()
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("invite")]
    [Summary("Get the bot invite link")]
    public async Task InviteAsync()
    {
        var application = await Context.Client.GetApplicationInfoAsync();
        var inviteUrl = $"https://discord.com/api/oauth2/authorize?client_id={application.Id}&permissions=8&scope=bot%20applications.commands";

        var embed = new EmbedBuilder()
            .WithTitle("🔗 Invite Link")
            .WithDescription($"[Click here to invite the bot to your server]({inviteUrl})")
            .WithColor(Color.Green)
            .WithFooter($"Requested by {Context.User.Username}", Context.User.GetAvatarUrl())
            .WithCurrentTimestamp()
            .Build();

        await ReplyAsync(embed: embed);
    }

    private static Color GetLatencyColor(int latency) => latency switch
    {
        < 100 => Color.Green,
        < 250 => Color.Gold,
        _ => Color.Red
    };

    private static string FormatUptime(TimeSpan uptime)
    {
        var parts = new List<string>();

        if (uptime.Days > 0)
            parts.Add($"{uptime.Days} day{(uptime.Days != 1 ? "s" : "")}");
        if (uptime.Hours > 0)
            parts.Add($"{uptime.Hours} hour{(uptime.Hours != 1 ? "s" : "")}");
        if (uptime.Minutes > 0)
            parts.Add($"{uptime.Minutes} minute{(uptime.Minutes != 1 ? "s" : "")}");
        if (parts.Count == 0 || uptime.Seconds > 0)
            parts.Add($"{uptime.Seconds} second{(uptime.Seconds != 1 ? "s" : "")}");

        return string.Join(", ", parts);
    }
}