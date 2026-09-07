# Streak

Offline-first habit / streak PWA (Svelte) + ASP.NET Core API.

## Layout

- `src/api` — .NET 10 solution (`Streak.Api`, `Streak.Api.Tests`)
- `src/web` — Svelte + Vite SPA
- `docs/` — PRD, ADR, schema, deploy examples

## Local development

```bash
# API
cd src/api && dotnet run --project Streak.Api

# Web (separate terminal)
cd src/web && npm run dev
```

## Verify

```bash
cd src/api && dotnet test
cd src/web && npm run build && npm run check
```

## Docker

```bash
docker build -t streak:local .
docker run --rm -p 8080:8080 streak:local
# GET http://localhost:8080/healthz → ok
```
