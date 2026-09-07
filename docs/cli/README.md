# Streak — project CLI & session notes

Commands and layout notes for local work. Update after each session.

## Repo layout (locked 2026-09-07)

```
src/api/     # .NET: Streak.sln, Streak.Api, Streak.Api.Tests
src/web/     # Svelte + Vite SPA
docs/        # product docs + examples (not wired CI)
```

Design: `docs/superpowers/specs/2026-09-07-repo-layout-design.md`

## Expected local commands (after scaffold)

```bash
# API
cd src/api && dotnet restore && dotnet build && dotnet test

# Web
cd src/web && npm ci && npm run build && npm run check

# From repo root (once Dockerfile exists)
docker build -t streak .
```

## Session log

| Date | Notes |
|---|---|
| 2026-09-07 | Approved repo layout `src/api` + `src/web`. Design written. No scaffold yet. |
| 2026-09-07 | Implementation plan: `docs/superpowers/plans/2026-09-07-repo-layout-scaffold.md`. |
| 2026-09-07 | SDD scaffold done on `feat/repo-layout-scaffold` (uncommitted). Docker smoke still needs daemon. |
| 2026-09-07 | Ran full stack via Docker: `docker build -t streak:local .` then `docker run -d --name streak -p 8080:8080 streak:local`. Open http://localhost:8080 — `/healthz` → `ok`. Stop: `docker rm -f streak`. |
| 2026-09-07 | Scaffolded src/api + src/web, Dockerfile, .github workflows (layout plan). |
