# --- web ---
FROM node:22-bookworm AS web
WORKDIR /src/web
COPY src/web/package.json src/web/package-lock.json ./
RUN npm ci
COPY src/web/ ./
RUN npm run build

# --- api build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src/api
COPY src/api/Streak.sln ./
COPY src/api/Streak.Api/Streak.Api.csproj Streak.Api/
COPY src/api/Streak.Api.Tests/Streak.Api.Tests.csproj Streak.Api.Tests/
RUN dotnet restore
COPY src/api/ ./
COPY --from=web /src/web/dist/ Streak.Api/wwwroot/
RUN dotnet publish Streak.Api/Streak.Api.csproj -c Release -o /app/publish --no-restore

# --- runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
COPY --from=api-build /app/publish .
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD curl -f http://localhost:8080/healthz || exit 1
ENTRYPOINT ["dotnet", "Streak.Api.dll"]
