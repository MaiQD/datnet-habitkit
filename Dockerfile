# Use the official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy everything at once to avoid layer caching issues
COPY . .

# Restore, build, and publish in one step
RUN dotnet publish HabitKitClone/HabitKitClone.csproj -c Release -o /app/publish --self-contained false

# Use the official .NET 9 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create data directory for SQLite database
RUN mkdir -p /data

# Copy published application
COPY --from=build /app/publish .

# Create database file only if it doesn't exist (preserves existing data)
RUN if [ ! -f /data/app.db ]; then touch /data/app.db; fi

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__DefaultConnection=DataSource=/data/app.db;Cache=Shared

# Expose port 8080 (Fly.io standard)
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "HabitKitClone.dll"]
