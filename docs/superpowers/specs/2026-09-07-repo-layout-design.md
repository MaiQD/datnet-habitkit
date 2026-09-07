# Design: Repo layout (src/api + src/web)

**Status:** Approved (conversation 2026-09-07)  
**Scope:** Repository folder layout only — not internal API layering or Svelte folder conventions.  
**Related:** ADR-0005, ADR-0007, ADR-0009, ADR-0010; `docs/ci.yml`, `docs/deploy.yml`

## Goal

Define where product code, Docker, CI, and docs live so scaffolding and pipelines have a single agreed tree. Old Blazor code is gone; `src/` is empty except placeholders.

## Decisions

| Choice | Decision |
|---|---|
| Layout | Monorepo with `src/api` + `src/web` |
| Frontend folder name | `web` (not `client`) — matches ADR-0007 and CI wording |
| .NET | One solution under `src/api`: `Streak.sln`, `Streak.Api`, `Streak.Api.Tests` |
| Frontend | npm/Vite project at `src/web` (nested `src/web/src` is normal) |
| Docker context | Repo root; single multi-stage `Dockerfile` |
| CI paths | `./src/api`, `./src/web` (update when moving workflows out of `docs/`) |
| Docs in main git | Product docs may stay untracked until explicitly committed; design/plans under `docs/superpowers/` |

## Target tree

```
/
  src/
    api/
      Streak.sln
      Streak.Api/                 # ASP.NET Core host
      Streak.Api.Tests/
    web/
      package.json
      vite.config.ts
      svelte.config.js
      index.html
      public/
      src/                        # Svelte app source
  docs/                           # PRD, ADR, SCHEMA, ERD, HTML prototypes, example compose/Caddy
  docs/superpowers/specs/         # design specs
  docs/superpowers/plans/         # implementation plans
  docs/cli/                       # project CLI / session notes
  .github/workflows/
    ci.yml
    deploy.yml
  Dockerfile
  .env.example
  .gitignore
  README.md
```

Remove empty `src/client` when scaffolding starts (rename path → `web`).

## Docker / CI / ops placement

- **In-repo (live):** root `Dockerfile`; `.github/workflows/ci.yml` and `deploy.yml` when wired.
- **In-repo (examples only):** `docs/docker-compose.streak.example.yml`, `docs/docker-compose.proxy.yml`, `docs/Caddyfile` — VPS-wide proxy is not owned long-term by this app repo (ADR-0010).
- **On VPS:** `/opt/streak` compose + `.env`; shared proxy under e.g. `/opt/proxy`.
- **Until wired:** keep draft workflows under `docs/`; when promoting, fix `working-directory` to `./src/api` and `./src/web`.

## Out of scope

- Clean Architecture / FluentValidation / mapping layout inside `Streak.Api`
- Svelte routes, stores, sync module layout inside `src/web/src`
- Scaffolding projects, Dockerfile implementation, or moving CI files (follow-up plan)

## Success criteria

- A new contributor can tell where API, web, Docker, and CI live from this doc alone.
- CI and Dockerfile paths match this tree with no `api/` or `web/` at repo root.
- No second frontend name (`client` vs `web`) in the tree.
