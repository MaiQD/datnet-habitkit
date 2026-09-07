# Repo layout scaffold Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Scaffold the approved monorepo tree (`src/api` + `src/web`), a root multi-stage Dockerfile that serves the SPA + API, and live GitHub Actions with correct paths.

**Architecture:** Empty placeholders become a minimal ASP.NET Core Web API (healthz + static SPA hosting) and a plain Svelte + Vite TypeScript SPA (no SvelteKit). One Docker image builds web → api → runtime. CI builds/tests both; deploy workflow is promoted from docs with unchanged VPS SSH steps.

**Tech Stack:** .NET 10, ASP.NET Core, xUnit, Svelte 5 + Vite + TypeScript, Node 22, Docker Buildx (linux/arm64), GitHub Actions.

## Global Constraints

- Spec: `docs/superpowers/specs/2026-09-07-repo-layout-design.md`
- Paths: product code only under `src/api` and `src/web` — never root-level `api/` or `web/`
- Frontend folder name: `web` (delete empty `src/client`)
- No SvelteKit; Vite SPA only (ADR-0007)
- No Identity, EF, sync, FluentValidation, or Svelte feature folders in this plan (YAGNI)
- TypeScript strict; no `any`
- Do **not** `git commit` unless the user explicitly asks in the session
- Do **not** commit `docs/` to main git unless the user explicitly asks
- Port: API listens on `8080` in containers (matches compose healthcheck)

## File map

| Path | Role |
|---|---|
| `src/api/Streak.sln` | Solution |
| `src/api/Streak.Api/` | Host: healthz, wwwroot SPA |
| `src/api/Streak.Api.Tests/` | Smoke tests for healthz |
| `src/web/` | Svelte + Vite SPA |
| `Dockerfile` | Multi-stage image |
| `.env.example` | Placeholder env keys |
| `README.md` | How to run locally |
| `.github/workflows/ci.yml` | CI with `./src/api`, `./src/web` |
| `.github/workflows/deploy.yml` | Deploy from docs, same SHA/ARM64 flow |
| `docs/cli/README.md` | Update after scaffold |
| `docs/ci.yml`, `docs/deploy.yml` | Leave as drafts or add a one-line “superseded by .github” note — do not delete without user ask |

---

### Task 1: Clear placeholder and create API solution

**Files:**
- Delete: `src/client/` (empty dir)
- Create: `src/api/Streak.sln`, `src/api/Streak.Api/`, `src/api/Streak.Api.Tests/`

**Interfaces:**
- Produces: solution builds; `Streak.Api` is a Web API project targeting `net10.0`

- [ ] **Step 1: Remove empty client placeholder**

```bash
rmdir /Users/datmai/Code/streak-habit/src/client 2>/dev/null || rm -rf /Users/datmai/Code/streak-habit/src/client
mkdir -p /Users/datmai/Code/streak-habit/src/api
```

Expected: `src/` contains only `api` (and later `web`).

- [ ] **Step 2: Scaffold Web API + xUnit test project**

```bash
cd /Users/datmai/Code/streak-habit/src/api
dotnet new sln -n Streak
dotnet new webapi -n Streak.Api -o Streak.Api --no-openapi false
dotnet new xunit -n Streak.Api.Tests -o Streak.Api.Tests
dotnet sln Streak.sln add Streak.Api/Streak.Api.csproj Streak.Api.Tests/Streak.Api.Tests.csproj
dotnet add Streak.Api.Tests/Streak.Api.Tests.csproj reference Streak.Api/Streak.Api.csproj
dotnet add Streak.Api.Tests/Streak.Api.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing
```

If `dotnet new webapi` prompts interactively, pass flags to skip auth and use minimal APIs (defaults on .NET 10 are fine). Remove WeatherForecast sample files if present (`WeatherForecast.cs`, `Controllers/WeatherForecastController.cs`, or equivalent minimal map samples) so the host starts clean.

- [ ] **Step 3: Verify restore/build**

```bash
cd /Users/datmai/Code/streak-habit/src/api
dotnet restore
dotnet build --configuration Release
```

Expected: `Build succeeded` with 0 errors.

---

### Task 2: Healthz endpoint + failing then passing test

**Files:**
- Modify: `src/api/Streak.Api/Program.cs`
- Create: `src/api/Streak.Api.Tests/HealthzTests.cs`
- Create: `src/api/Streak.Api/Streak.Api.csproj` may need `<InternalsVisibleTo>` or public `Program` partial — use the standard WebApplicationFactory pattern with `public partial class Program { }` at bottom of `Program.cs`

**Interfaces:**
- Produces: `GET /healthz` → `200` with body `Healthy` (or default HealthChecks JSON/text — prefer plain `Results.Ok()` text `"ok"` for simplest docker HEALTHCHECK; **standardize on:** `MapGet("/healthz", () => Results.Text("ok"));` so curl `-f` works without registering full health checks package)
- Consumes: `WebApplicationFactory<Program>`

- [ ] **Step 1: Write the failing test**

Create `src/api/Streak.Api.Tests/HealthzTests.cs`:

```csharp
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Streak.Api.Tests;

public sealed class HealthzTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthzTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Healthz_returns_ok()
    {
        var response = await _client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("ok", body);
    }
}
```

Ensure `Program` is visible: at end of `Program.cs` add `public partial class Program { }`.

- [ ] **Step 2: Run test — expect fail**

```bash
cd /Users/datmai/Code/streak-habit/src/api
dotnet test --filter FullyQualifiedName~HealthzTests --configuration Release
```

Expected: FAIL (404 or missing endpoint), not compile errors related to `Program` visibility. If compile fails on `Program`, fix InternalsVisibleTo / partial class first, then re-run until the failure is assertion/404.

- [ ] **Step 3: Implement minimal host**

Replace `Program.cs` with:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://0.0.0.0:8080");

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/healthz", () => Results.Text("ok"));

app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
```

Create empty `src/api/Streak.Api/wwwroot/.gitkeep` so static middleware has a folder (SPA assets arrive via Docker copy later).

- [ ] **Step 4: Run test — expect pass**

```bash
cd /Users/datmai/Code/streak-habit/src/api
dotnet test --configuration Release
```

Expected: all tests pass.

- [ ] **Step 5: Commit only if user asked** — otherwise skip.

---

### Task 3: Scaffold Svelte + Vite (TypeScript) under `src/web`

**Files:**
- Create: `src/web/**` via Vite scaffold

**Interfaces:**
- Produces: `npm run build` writes to `src/web/dist`; `npm run check` available

- [ ] **Step 1: Create project non-interactively**

```bash
cd /Users/datmai/Code/streak-habit
npm create vite@latest src/web -- --template svelte-ts
cd src/web
npm install
```

Expected: `src/web/package.json`, `src/web/src/App.svelte`, `src/web/tsconfig.json`.

- [ ] **Step 2: Ensure `check` script exists**

In `src/web/package.json`, scripts must include:

```json
"scripts": {
  "dev": "vite",
  "build": "vite build",
  "preview": "vite preview",
  "check": "svelte-check --tsconfig ./tsconfig.app.json"
}
```

If `svelte-check` is missing:

```bash
cd /Users/datmai/Code/streak-habit/src/web
npm install -D svelte-check typescript
```

Adjust `--tsconfig` path to match whatever Vite generated (`tsconfig.json` or `tsconfig.app.json`).

- [ ] **Step 3: Build and type-check**

```bash
cd /Users/datmai/Code/streak-habit/src/web
npm run build
npm run check
```

Expected: `dist/` created; check exits 0.

- [ ] **Step 4: Commit only if user asked** — otherwise skip.

---

### Task 4: Root Dockerfile (multi-stage)

**Files:**
- Create: `/Users/datmai/Code/streak-habit/Dockerfile`
- Create: `/Users/datmai/Code/streak-habit/.dockerignore`

**Interfaces:**
- Consumes: `src/web` build output → copied into `Streak.Api/wwwroot`; published API listens on 8080
- Produces: image that answers `GET /healthz` with `ok`

- [ ] **Step 1: Write `.dockerignore`**

```
**/.git
**/bin
**/obj
**/node_modules
**/dist
**/.vs
**/.DS_Store
docs
.env
*.db
*.db-shm
*.db-wal
```

- [ ] **Step 2: Write `Dockerfile`**

```dockerfile
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
```

If `package-lock.json` does not exist yet, run `npm install` in `src/web` once so lockfile is created before Docker `npm ci`.

- [ ] **Step 3: Build image locally (optional architecture)**

```bash
cd /Users/datmai/Code/streak-habit
docker build -t streak:local .
```

Expected: build succeeds.

- [ ] **Step 4: Smoke-run container**

```bash
docker run --rm -d --name streak-smoke -p 8080:8080 streak:local
sleep 2
curl -sf http://localhost:8080/healthz
docker stop streak-smoke
```

Expected: prints `ok`.

---

### Task 5: `.env.example` + `README.md`

**Files:**
- Create: `.env.example`
- Create: `README.md`
- Modify: `docs/cli/README.md` (session note)

- [ ] **Step 1: Write `.env.example`**

```env
# Used on VPS alongside docker-compose (ADR-0008). Never commit a real .env.
IMAGE_TAG=latest
ASPNETCORE_ENVIRONMENT=Production
# ConnectionStrings__Default=Data Source=/app/data/streak.db
# Authentication__Google__ClientId=
# Authentication__Google__ClientSecret=
# Vapid__PublicKey=
# Vapid__PrivateKey=
# Vapid__Subject=mailto:you@example.com
```

- [ ] **Step 2: Write `README.md`**

```markdown
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
```

- [ ] **Step 3: Append session line to `docs/cli/README.md`**

Add under Session log:

`| 2026-09-07 | Scaffolded src/api + src/web, Dockerfile, .github workflows (layout plan). |`

---

### Task 6: Promote CI/CD workflows with `src/` paths

**Files:**
- Create: `.github/workflows/ci.yml`
- Create: `.github/workflows/deploy.yml`

**Interfaces:**
- Consumes: Task 1–3 layout; Dockerfile from Task 4
- Produces: CI working-directory `./src/api` and `./src/web`; artifact path `src/api/**/test-results.trx`; npm cache path `src/web/package-lock.json`

- [ ] **Step 1: Write `.github/workflows/ci.yml`**

```yaml
name: CI

on:
  pull_request:
  push:
    branches-ignore: [main]

jobs:
  api:
    name: Build & test API
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: ./src/api
    steps:
      - uses: actions/checkout@v4

      - name: Set up .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Test
        run: dotnet test --no-build --configuration Release --logger "trx;LogFileName=test-results.trx"

      - name: Publish test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: api-test-results
          path: src/api/**/test-results.trx

  web:
    name: Build frontend
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: ./src/web
    steps:
      - uses: actions/checkout@v4

      - name: Set up Node
        uses: actions/setup-node@v4
        with:
          node-version: '22'
          cache: 'npm'
          cache-dependency-path: src/web/package-lock.json

      - name: Install dependencies
        run: npm ci

      - name: Build
        run: npm run build

      - name: Type-check
        run: npm run check --if-present
```

- [ ] **Step 2: Write `.github/workflows/deploy.yml`**

Copy `docs/deploy.yml` content unchanged except ensure `file: ./Dockerfile` and `context: .` remain at repo root (already correct). Create `.github/workflows/deploy.yml` with that content.

- [ ] **Step 3: Sanity-check paths in-repo**

```bash
test -f /Users/datmai/Code/streak-habit/.github/workflows/ci.yml
test -f /Users/datmai/Code/streak-habit/.github/workflows/deploy.yml
rg "working-directory: \\./src/(api|web)" /Users/datmai/Code/streak-habit/.github/workflows/ci.yml
```

Expected: both files exist; two working-directory matches.

- [ ] **Step 4: Optional note in draft docs** — at top of `docs/ci.yml` add:

`# SUPERSEDED by .github/workflows/ci.yml (paths are ./src/api and ./src/web).`

Same one-liner for `docs/deploy.yml` pointing at `.github/workflows/deploy.yml`.

---

### Task 7: Final verification (layout plan done)

**Files:** none new

- [ ] **Step 1: Tree check**

```bash
cd /Users/datmai/Code/streak-habit
test ! -e src/client
test -f src/api/Streak.sln
test -f src/api/Streak.Api/Program.cs
test -f src/web/package.json
test -f Dockerfile
test -f .github/workflows/ci.yml
```

Expected: all succeed.

- [ ] **Step 2: Full local verify**

```bash
cd /Users/datmai/Code/streak-habit/src/api && dotnet test --configuration Release
cd /Users/datmai/Code/streak-habit/src/web && npm run build && npm run check
```

Expected: both green.

- [ ] **Step 3: Stop** — ask user before any git commit or PR.

---

## Spec coverage (self-review)

| Spec item | Task |
|---|---|
| `src/api` + `src/web`, remove `client` | 1, 3 |
| `Streak.sln`, `Streak.Api`, `Streak.Api.Tests` | 1–2 |
| Vite nested `src/web/src` | 3 |
| Root Dockerfile multi-stage | 4 |
| CI paths `./src/api`, `./src/web` | 6 |
| `.env.example`, README, docs/cli | 5 |
| Proxy compose stays examples in docs | (no move — satisfied by omission) |
| No internal Clean Arch / Svelte feature layout | (omitted) |

No TBD placeholders; health response contract is `"ok"` everywhere (tests + HEALTHCHECK).
