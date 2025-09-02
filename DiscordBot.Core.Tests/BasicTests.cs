using DiscordBot.Core.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Core.Tests;

/// <summary>
/// Basic tests to ensure the Discord bot functionality works
/// </summary>
public class BasicTests
{
    [Fact]
    public void Configuration_ShouldLoadCorrectly()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Bot:Discord:Token"] = "test-token-123",
            ["Bot:Settings:CommandPrefix"] = "!",
            ["Bot:Features:EnableSlashCommands"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        var botConfig = new BotConfiguration();
        configuration.GetSection(BotConfiguration.SectionName).Bind(botConfig);

        // Assert
        botConfig.Should().NotBeNull();
        botConfig.Discord.Token.Should().Be("test-token-123");
        botConfig.Settings.CommandPrefix.Should().Be("!");
        botConfig.Features.EnableSlashCommands.Should().BeTrue();
    }

    [Fact]
    public void ServiceContainer_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        var testConfig = new BotConfiguration
        {
            Discord = new DiscordSettings { Token = "test" },
            Settings = new BotSettings(),
            Features = new FeatureSettings()
        };
        
        services.AddSingleton(testConfig);

        // Act
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<ILoggerFactory>().Should().NotBeNull();
        provider.GetService<BotConfiguration>().Should().NotBeNull();
    }

    [Fact]
    public void BotConfiguration_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var config = new BotConfiguration();

        // Assert
        config.Settings.CommandPrefix.Should().Be("!");
        config.Settings.CommandTimeout.Should().Be(3000);
        config.Features.EnableSlashCommands.Should().BeTrue();
        config.Features.EnablePrefixCommands.Should().BeTrue();
        config.Features.EnableAutoReconnect.Should().BeTrue();
        config.Logging.MinimumLevel.Should().Be("Information");
    }

    [Fact]
    public void ActivitySettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new ActivitySettings();

        // Assert
        settings.Type.Should().Be("Playing");
        settings.Name.Should().Be("with .NET 8");
        settings.StreamUrl.Should().BeNull();
    }

    [Fact]
    public void DiscordSettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new DiscordSettings();

        // Assert
        settings.Token.Should().BeEmpty();
        settings.GuildId.Should().BeNull();
        settings.MessageCacheSize.Should().Be(100);
        settings.AlwaysDownloadUsers.Should().Be(250);
        settings.GatewayIntents.Should().Be("AllUnprivileged");
    }

    [Theory]
    [InlineData("!", true, true)]
    [InlineData("?", false, true)]
    [InlineData("/", true, false)]
    public void BotConfiguration_ShouldAcceptDifferentSettings(string prefix, bool slashCommands, bool prefixCommands)
    {
        // Arrange
        var config = new BotConfiguration
        {
            Settings = new BotSettings { CommandPrefix = prefix },
            Features = new FeatureSettings 
            { 
                EnableSlashCommands = slashCommands,
                EnablePrefixCommands = prefixCommands
            }
        };

        // Assert
        config.Settings.CommandPrefix.Should().Be(prefix);
        config.Features.EnableSlashCommands.Should().Be(slashCommands);
        config.Features.EnablePrefixCommands.Should().Be(prefixCommands);
    }

    [Fact]
    public void LoggingSettings_ShouldHaveValidDefaults()
    {
        // Arrange & Act
        var settings = new LoggingSettings();

        // Assert
        settings.EnableConsoleLogging.Should().BeTrue();
        settings.EnableFileLogging.Should().BeTrue();
        settings.LogFilePath.Should().Be("logs/bot-.log");
        settings.RetainedFileCountLimit.Should().Be(7);
        settings.MinimumLevel.Should().Be("Information");
    }

    [Theory]
    [InlineData("AllUnprivileged")]
    [InlineData("All")]
    [InlineData("Guilds,GuildMessages")]
    public void DiscordSettings_ShouldAcceptDifferentIntents(string intents)
    {
        // Arrange & Act
        var settings = new DiscordSettings
        {
            Token = "test-token",
            GatewayIntents = intents
        };

        // Assert
        settings.GatewayIntents.Should().Be(intents);
    }
} 