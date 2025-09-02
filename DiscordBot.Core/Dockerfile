# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["DiscordBot.Core.csproj", "."]
RUN dotnet restore "DiscordBot.Core.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "DiscordBot.Core.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "DiscordBot.Core.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS final
WORKDIR /app

# Install cultures (for globalization)
RUN apk add --no-cache icu-libs

# Create non-root user
RUN adduser -D -h /app -s /bin/sh botuser

# Copy published app
COPY --from=publish --chown=botuser:botuser /app/publish .

# Set environment variables
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8

# Switch to non-root user
USER botuser

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD pgrep -f "dotnet DiscordBot.Core.dll" || exit 1

# Entry point
ENTRYPOINT ["dotnet", "DiscordBot.Core.dll"] 