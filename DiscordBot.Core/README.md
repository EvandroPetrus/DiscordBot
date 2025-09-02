# Discord Bot (.NET 8)

A modern, scalable Discord bot built with .NET 8 following SOLID principles and clean architecture patterns.

## 🚀 Features

- **Slash Commands & Prefix Commands** - Support for both modern slash commands and traditional prefix commands
- **Modular Architecture** - Clean, extensible design following SOLID principles
- **Dependency Injection** - Full DI support with proper abstractions
- **Comprehensive Logging** - Serilog integration with console and file logging
- **Auto-reconnection** - Automatic reconnection on disconnect
- **Configuration Management** - Flexible configuration through appsettings.json and environment variables
- **Interactive Components** - Support for buttons, select menus, and modals

## 📋 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A Discord Bot Token ([Create one here](https://discord.com/developers/applications))
- Git (for deployment)

## 🔧 Setup Instructions

### 1. Clone or Download the Project

```bash
git clone <your-repo-url>
cd DiscordBot.Core
```

### 2. Configure the Bot

1. Open `appsettings.json`
2. Replace `YOUR_BOT_TOKEN_HERE` with your actual bot token
3. (Optional) Set a specific `GuildId` for faster slash command updates during development

```json
{
  "Bot": {
    "Discord": {
      "Token": "YOUR_ACTUAL_TOKEN_HERE",
      "GuildId": 123456789012345678  // Optional: Your test server ID
    }
  }
}
```

### 3. Build and Run Locally

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the bot
dotnet run
```

## 🆓 Free Hosting Options

### Option 1: Railway.app (Recommended)

Railway offers $5 free credits monthly, perfect for hosting a Discord bot.

1. **Create Account**: Sign up at [Railway.app](https://railway.app)

2. **Deploy from GitHub**:
   - Push your code to GitHub
   - In Railway, click "New Project" → "Deploy from GitHub repo"
   - Select your repository
   
3. **Set Environment Variables**:
   - Go to your project settings
   - Add variable: `Bot__Discord__Token` = `YOUR_BOT_TOKEN`
   
4. **Deploy**:
   - Railway will automatically build and deploy your bot
   - Check logs to ensure it's running

### Option 2: Fly.io

Fly.io offers free tier with 3 shared-cpu-1x VMs.

1. **Install Fly CLI**: 
   ```bash
   # Windows (PowerShell)
   iwr https://fly.io/install.ps1 -useb | iex
   
   # Mac/Linux
   curl -L https://fly.io/install.sh | sh
   ```

2. **Create Dockerfile** in project root:
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
   WORKDIR /app

   FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
   WORKDIR /src
   COPY ["DiscordBot.Core.csproj", "."]
   RUN dotnet restore "./DiscordBot.Core.csproj"
   COPY . .
   WORKDIR "/src/."
   RUN dotnet build "DiscordBot.Core.csproj" -c Release -o /app/build

   FROM build AS publish
   RUN dotnet publish "DiscordBot.Core.csproj" -c Release -o /app/publish

   FROM base AS final
   WORKDIR /app
   COPY --from=publish /app/publish .
   ENTRYPOINT ["dotnet", "DiscordBot.Core.dll"]
   ```

3. **Deploy**:
   ```bash
   fly launch
   fly secrets set Bot__Discord__Token=YOUR_BOT_TOKEN
   fly deploy
   ```

### Option 3: Render.com

Render offers free tier with limitations.

1. **Create Account**: Sign up at [Render.com](https://render.com)

2. **Create Web Service**:
   - New → Web Service
   - Connect GitHub repository
   - Build Command: `dotnet publish -c Release -o out`
   - Start Command: `dotnet out/DiscordBot.Core.dll`
   
3. **Set Environment Variables**:
   - Add `Bot__Discord__Token` = `YOUR_BOT_TOKEN`

### Option 4: Heroku (Free alternatives)

Since Heroku discontinued free tier, consider:
- **Koyeb**: Similar to Heroku, offers $5.50 free monthly credits
- **Cyclic.sh**: Good for lightweight bots
- **Replit**: Great for development, can host 24/7 with Hacker plan

### Option 5: Self-Hosting (Always Free)

**Raspberry Pi / Old PC**:
1. Install .NET 8 runtime
2. Clone repository
3. Set up as systemd service (Linux) or Windows Service
4. Use ngrok or Cloudflare Tunnel for external access if needed

**Free VPS Options**:
- Oracle Cloud Free Tier (2 AMD VMs forever free)
- Google Cloud Free Tier (e2-micro instance)
- AWS Free Tier (t2.micro for 12 months)

## 🐳 Docker Deployment

Build and run with Docker:

```bash
# Build image
docker build -t discord-bot .

# Run container
docker run -d \
  --name discord-bot \
  --restart unless-stopped \
  -e Bot__Discord__Token=YOUR_BOT_TOKEN \
  discord-bot
```

## 📁 Project Structure

```
DiscordBot.Core/
├── Abstractions/          # Interfaces and contracts
├── Configuration/         # Configuration models
├── Modules/              # Command modules
│   ├── GeneralModule.cs # Prefix commands
│   └── InteractionModule.cs # Slash commands
├── Services/             # Core services
│   ├── CommandHandlerService.cs
│   ├── InteractionHandlerService.cs
│   ├── DiscordBotService.cs
│   └── LoggingService.cs
├── Program.cs            # Entry point with DI setup
├── appsettings.json      # Configuration file
└── README.md            # This file
```

## 🔨 Adding New Commands

### Slash Command Example

```csharp
[SlashCommand("hello", "Says hello")]
public async Task HelloAsync([Summary("name", "Your name")] string name)
{
    await RespondAsync($"Hello, {name}!");
}
```

### Prefix Command Example

```csharp
[Command("hello")]
[Summary("Says hello to a user")]
public async Task HelloAsync(string name)
{
    await ReplyAsync($"Hello, {name}!");
}
```

## 🛠️ Configuration Options

Edit `appsettings.json` to customize:

- **Command Prefix**: Change the bot's command prefix
- **Activity Status**: Set what the bot is "playing"
- **Feature Toggles**: Enable/disable features
- **Logging Levels**: Control log verbosity
- **Cache Settings**: Adjust message cache size

## 🚨 Troubleshooting

### Bot Not Responding to Commands
- Ensure bot has proper permissions in Discord
- Check if the bot token is valid
- Verify GatewayIntents include necessary permissions

### Slash Commands Not Showing
- Wait up to 1 hour for global commands to propagate
- Use `GuildId` in config for instant updates in test server
- Ensure bot was invited with `applications.commands` scope

### High Memory Usage
- Adjust `MessageCacheSize` in configuration
- Set `AlwaysDownloadUsers` to a lower value
- Consider using pagination for large data operations

## 📚 Resources

- [Discord.Net Documentation](https://docs.discordnet.dev/)
- [Discord Developer Portal](https://discord.com/developers/docs)
- [.NET 8 Documentation](https://docs.microsoft.com/dotnet/)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes following SOLID principles
4. Submit a pull request

## 📄 License

This project is open source. Feel free to use, modify, and distribute as needed.

## 💡 Tips for Production

1. **Use Environment Variables**: Never commit tokens to version control
2. **Enable Logging**: Monitor your bot's behavior
3. **Rate Limiting**: Implement rate limiting for commands
4. **Error Handling**: Add comprehensive error handling
5. **Health Checks**: Implement health check endpoints
6. **Backup Configuration**: Keep configuration backups
7. **Monitor Resources**: Track CPU and memory usage
8. **Update Regularly**: Keep dependencies updated

## 🔐 Security Best Practices

- Store tokens in environment variables or secure vaults
- Use role-based permissions for admin commands  
- Implement command cooldowns to prevent spam
- Validate and sanitize all user inputs
- Regular security audits of dependencies
- Use HTTPS for any web endpoints
- Implement proper authentication for dashboard/API

---

Built with ❤️ using .NET 8 and Discord.Net 