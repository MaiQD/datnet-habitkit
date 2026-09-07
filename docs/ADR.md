# Architecture Decision Records — Streak

Each record follows: Context → Decision → Consequences → Alternatives Considered.

---

## ADR-0001: Single-Page App (SPA), not Server-Side Rendering (SSR)

**Status:** Accepted

**Context**
The app has no public, indexable content — everything meaningful happens after login. There's no SEO requirement and no need for fast first-paint of unique per-URL content.

**Decision**
Build the client as a single-page app: plain HTML/CSS/JS (no framework), installable as a PWA via a manifest and service worker. No server-side rendering.

**Consequences**
- Simpler deployment: the client is static files, servable by the reverse proxy or the API server directly.
- No hydration complexity, no server rendering runtime to run or scale.
- First-load performance depends on client-side JS execution rather than server-rendered HTML — acceptable given the app's small size and lack of heavy frameworks.
- *Amended by ADR-0007:* the "no framework" implementation detail was revisited once the app grew past one page; the SPA-not-SSR decision itself is unchanged.

**Alternatives Considered**
- SSR (e.g. Next.js): rejected — adds a Node runtime, build pipeline, and hydration complexity with no corresponding benefit for a private, authenticated, non-indexed app.

---

## ADR-0002: The app has a backend API; sync is local-first and debounced, not per-action

**Status:** Accepted

**Context**
Once real accounts exist (Google/password/passkey login), data can no longer live only in browser local storage — it must be recoverable across devices. But calling the API on every single tap would make the UI feel network-bound and fragile, especially offline or on mobile networks.

**Decision**
- All writes apply immediately to local storage (or IndexedDB); the UI never waits on a network call to reflect a change.
- Changes are queued and synced to the backend on a short debounce (~2–3 seconds after the last change), plus forced flushes on `visibilitychange` (backgrounding) and `pagehide`/`beforeunload`.
- On app open, the client pulls the latest server state (`GET /sync`) and reconciles with local state using last-write-wins by an `updatedAt` timestamp.
- The API surface is intentionally small: `GET /sync` and `POST /sync`, operating on the user's full state blob, rather than granular per-entry CRUD endpoints.
- **Optimistic concurrency control guards the blob-level write** (see "Stale blob overwrite" below): every `GET /sync` response includes the server's current `syncVersion` for that user — a monotonic integer, incremented by the server on every accepted write, *not* a client-supplied timestamp (avoids cross-device clock skew entirely). The client stores this and must echo it back as `baseVersion` on the next `POST /sync`. The server accepts the write only if `baseVersion` matches its current `syncVersion` for that user; otherwise it rejects with **HTTP 409 Conflict** and makes no change. On a 409, the client must `GET /sync` to pull the authoritative latest state, reconcile any of its own not-yet-synced local changes on top of it (per-row last-write-wins still applies at this stage, using each row's own `UpdatedAt`), and retry the `POST` with the new `baseVersion`.

**Stale blob overwrite — identified risk and mitigation**
Without the check above, a real edge case exists even for a single user across two devices: edit on phone → lock it (edit debounced but not yet flushed) → edit on desktop, which updates the server → unlock the phone → its `visibilitychange` handler flushes the phone's now-stale local blob, overwriting the desktop's newer changes, since the phone never pulled the desktop's update before pushing. Per-row `UpdatedAt` timestamps inside the blob don't protect against this on their own if the sync endpoint doesn't explicitly check the state hasn't moved since the client last read it — the `baseVersion`/409 mechanism above is what closes this gap, by making "the server changed since I last synced" a rejected write rather than a silent overwrite.

**Consequences**
- UI stays instant and fully usable offline.
- The 409-and-retry flow means a genuine concurrent-edit conflict surfaces as one extra round trip (pull, reconcile, retry) rather than silent data loss — a small added complexity in the client's sync logic, in exchange for closing a real correctness gap.
- Reconciliation after a 409 still relies on per-row `UpdatedAt` last-write-wins once the client has the fresh server state — this is an accepted simplification given the expected usage pattern (one person, occasional multi-device use), not a full CRDT-style merge.
- The "whole blob" sync model is simple to implement and reason about, but doesn't scale well to large numbers of entries or to true real-time multi-user collaboration — acceptable given current non-goals.

**Alternatives Considered**
- Per-action API calls (call the server on every tap): rejected — poor offline behavior, higher latency perception, higher server load for no real benefit at this usage scale.
- Full granular REST CRUD (per-entry endpoints): rejected for now as premature complexity; can be introduced later if genuine multi-device real-time collaboration becomes a goal.
- Using the client-supplied `updatedAt` timestamp itself as the concurrency check (instead of a server-assigned `syncVersion` integer): rejected — vulnerable to clock drift/skew between devices, which could cause both false conflicts and, worse, false non-conflicts (a fast-forward-clocked device silently "winning" a race it shouldn't have).

---

## ADR-0003: SQLite, not Postgres, for the database

**Status:** Accepted

**Context**
Expected usage is a single user or a small handful of users, each syncing their own private data across their own devices, on a single VPS running a single backend instance.

**Decision**
Use SQLite as the database, with the database file on a persistent Docker volume. Use [Litestream](https://litestream.io/) (or an equivalent) to continuously replicate the SQLite file to off-box object storage for disaster recovery.

**Consequences**
- No extra database container/process to run, configure, or monitor.
- Backups are trivial (copy the file, or rely on continuous Litestream replication).
- Does not support multiple concurrently-writing backend replicas — acceptable since the deployment is a single backend instance on a single VPS.

**Alternatives Considered**
- Postgres: rejected for now — its concurrency and scaling strengths solve a problem (many concurrent writers, large multi-tenant scale) this app doesn't have yet. Revisit if the user base or write concurrency grows meaningfully.

---

## ADR-0004: Reminders use Web Push (server-triggered), not client-only local notifications

**Status:** Accepted

**Context**
Reminders are core to the product's value ("don't forget today"), but browsers do not provide a reliable way to schedule a local notification that fires while the app is fully closed, across platforms.

**Decision**
Implement reminders via Web Push:
- Generate one VAPID keypair for the app; the public key ships to the client, the private key stays server-side.
- The client subscribes via `pushManager.subscribe(...)` after an explicit, contextual permission prompt (not on cold app load), and sends the resulting subscription to the backend.
- A server-side scheduled job checks, per user, whether they've logged today; if not and it's near their reminder time, it sends an encrypted push via a library (e.g. `web-push`/`pywebpush`) using the stored subscription and the VAPID private key.
- The service worker implements `push` (show notification) and `notificationclick` (focus/open the app) event handlers.
- The service worker also handles `pushsubscriptionchange` to re-subscribe and re-register with the backend when a subscription rotates.

**Consequences**
- Reminders work even when the app is fully closed, on supported platforms — this requires the backend to exist and run a scheduler, which is already justified by ADR-0002.
- On iOS, Web Push only works if the user has installed the PWA to the home screen (iOS 16.4+); it does not work in a normal Safari tab. This is a known, accepted platform limitation, to be surfaced in-app (e.g. prompting install for iOS users who want reminders).
- Subscriptions are stored per device, not per user, since a user may be logged in on multiple devices.

**Alternatives Considered**
- Client-only local notifications (e.g. scheduled via a background timer while a tab is open): rejected — unreliable once the app/tab is closed, which is the primary case a reminder needs to cover.

---

## ADR-0005: Deployment via Docker Compose with a reverse proxy for automatic HTTPS

**Status:** Accepted

**Context**
Deployment target is a single existing VPS. The goal is to expose the app over HTTPS with minimal ongoing operational overhead.

**Decision**
Deploy via Docker Compose with roughly this shape:
- A reverse proxy (Caddy) handling automatic HTTPS (Let's Encrypt) and routing.
- One backend API container (Node/Fastify or Python/FastAPI — implementation choice, not yet finalized) serving `/sync`, auth, and push-subscription endpoints, and the scheduled reminder job.
- SQLite database file on a mounted volume, replicated off-box via Litestream.
- Static SPA assets served either directly by Caddy or by the backend container.

**Consequences**
- Minimal moving parts: one proxy container, one app container, one volume.
- Automatic certificate renewal with very little config, satisfying the HTTPS requirement PWAs need for installability and Web Push.
- Single point of failure (one VPS, one backend instance) is accepted given current scale; revisit if uptime requirements or load increase.

**Alternatives Considered**
- Managed platform-as-a-service (e.g. Fly.io, Render): rejected for now since a VPS is already available and Docker Compose meets the need with no added cost.
- Nginx instead of Caddy: viable, but Caddy's automatic HTTPS reduces config surface for a single-operator deployment.

---

## ADR-0006: Support multiple authentication methods (Google OAuth, password, passkey)

**Status:** Accepted (client UI built; backend integration pending)

**Context**
Real accounts are required for cross-device sync (ADR-0002) and for scoping reminders/push subscriptions per user.

**Decision**
Offer three login paths on the same login screen: Google OAuth, traditional email/username + password, and WebAuthn-based passkeys. The frontend UI shell for all three has been built; each currently requires backend wiring:
- Google: an OAuth redirect flow or Google Identity Services SDK integration.
- Password: ASP.NET Core Identity's built-in password hashing and credential checking (`UserManager`/`SignInManager`) — no custom implementation needed.
- Passkey: **ASP.NET Core Identity's built-in passkey support** (available as of .NET 10), rather than a custom WebAuthn implementation or a third-party library. Identity provides `SignInManager` methods for the full registration ("attestation") and authentication ("assertion") ceremonies — `MakePasskeyCreationOptionsAsync`, `PerformPasskeyAttestationAsync`, `MakePasskeyRequestOptionsAsync`, `PasskeySignInAsync` — and persists credentials itself via `UserManager.AddOrUpdatePasskeyAsync`, with no custom credential table required (see SCHEMA.md §2.6, updated).

**Consequences**
- Gives users a low-friction option (Google, passkey) alongside a fallback (password) for users who prefer it.
- Passkey backend work is substantially lighter than originally scoped: Identity handles challenge generation, attestation/assertion verification, sign-counter replay protection, and credential storage. Remaining app-side work is mostly plumbing two endpoints (creation options + registration, request options + sign-in) and a small amount of client-side JavaScript to call `navigator.credentials.create()`/`.get()`.
- A few constraints to carry into implementation:
  - All passkey operations require HTTPS (already satisfied by ADR-0005's Caddy setup).
  - `IdentityPasskeyOptions.ServerDomain` should be set explicitly rather than relying on inferred host headers, to avoid subdomain-scoping surprises.
  - Identity does **not** validate attestation statements by default — acceptable for a consumer app, but worth knowing if security requirements tighten later (custom validation is supported via `VerifyAttestationStatement`).
  - Since passkey-capable accounts still need a recovery path, users should be able to fall back to password or Google sign-in rather than relying on passkey as the sole credential — already satisfied by offering all three methods.
- A third-party library (`Fido2NetLib`) remains a documented option for apps needing full general-purpose WebAuthn support (e.g. custom attestation trust stores) beyond what Identity's authentication-scoped implementation covers — not needed for this app's requirements.

**Alternatives Considered**
- Password-only: simpler backend, but worse UX and weaker default security posture than passkeys/OAuth.
- Passkey-only: rejected — not all users have compatible devices/browsers yet; a fallback is needed.
- Third-party WebAuthn library (`Fido2NetLib`) or a fully custom WebAuthn implementation: rejected in favor of ASP.NET Core Identity's built-in passkey support, which covers this app's scenarios (passwordless sign-in, adding a passkey to an existing account) without the extra integration surface.

---

## ADR-0007: Frontend framework — Svelte + Vite (no SvelteKit), not React/Vue

**Status:** Accepted

**Context**
The app started as hand-written vanilla HTML/CSS/JS per page (ADR-0001), which was appropriate at one page. Past two pages (login, main app) with shared state (auth, sync), shared UI components (modals, toasts, sheets), and more pages likely on the way (e.g. settings), maintaining everything as copy-pasted inline scripts across separate HTML files becomes error-prone and hard to reuse. A framework is warranted; the choice of *which* one still needs to respect the app's existing constraints: SPA-only (ADR-0001), lightweight/fast on mobile, and PWA-installable.

**Decision**
Adopt **Svelte + Vite**, used as a plain SPA (no SvelteKit), with:
- A lightweight client-side router (e.g. `svelte-spa-router`) for navigation between login, the main tracker, and future pages — no server routing, no SSR.
- `vite-plugin-pwa` to generate the web app manifest and service worker (including offline caching and the push/`notificationclick` handlers from ADR-0004), rather than hand-maintaining `sw.js`.
- The existing CSS custom-property design system (colors, spacing, type) carried over unchanged into Svelte components.

**Consequences**
- Svelte compiles away at build time with no virtual DOM and a very small runtime, keeping bundle size and mobile performance close to the original vanilla-JS baseline while gaining component reuse and structure.
- Shared components (modal/sheet, toast, card, grid) can be written once and reused across pages, removing the copy-paste risk that caused earlier bugs.
- Introduces a build step (Vite) where previously there was none — acceptable, since it's a one-time dev-environment cost, not a runtime cost, and doesn't touch the "no SSR" decision in ADR-0001.
- SvelteKit was deliberately not adopted, since its default conventions lean SSR-first, which would work against ADR-0001; plain Vite + Svelte avoids that entirely.

**Alternatives Considered**
- **React or Vue (with their typical toolchains):** rejected — larger runtime and more ceremony (routing libraries, state management patterns) than this app's scope currently justifies; better suited to larger teams or more complex shared-state needs than exist here.
- **Preact + `htm` (no build step):** a valid lighter-weight alternative if avoiding a build step entirely is ever a priority — noted as a fallback option, not chosen now because Svelte's DX and `vite-plugin-pwa` tooling are a better fit for the PWA requirements already committed to.
- **Continuing with hand-written vanilla JS per page:** rejected — the original approach that prompted this ADR; doesn't scale cleanly past a couple of pages with shared components and state.

---

## ADR-0008: Secrets management via `.env` file, not a dedicated vault service

**Status:** Accepted

**Context**
The app has several secrets to protect: the DB connection string, Google OAuth client ID/secret, the VAPID private key (ADR-0004), and ASP.NET Core Identity's Data Protection key ring (which encrypts auth cookies and the passkey attestation/assertion state from ADR-0006). Deployment is a single VPS, single operator, single Docker Compose stack (ADR-0005) — not a multi-team or multi-environment setup.

**Decision**
- Store secrets in a `.env` file, permissioned `chmod 600`, excluded from version control, and referenced by `docker-compose.yml` via `env_file`.
- Persist ASP.NET Core Identity's Data Protection key ring to a mounted volume (`PersistKeysToFileSystem`) and protect it at rest with a certificate (`.ProtectKeysWithCertificate(...)`), rather than leaving it as in-memory (the default) or unprotected on disk. This is treated as a required fix, not optional hardening: without it, every container restart/redeploy invalidates all Data Protection–encrypted state, silently logging out every user and breaking any in-flight passkey registration.
- Revisit this decision (see Alternatives Considered) if the operational context changes — e.g. multiple collaborators needing independent access to the same secrets, or a need for rotation history/audit trail.

**Consequences**
- Minimal operational overhead: no extra service to run, configure, or keep patched.
- Weaker than a dedicated vault on: secret rotation tooling, access audit trail, and env-var visibility (env vars are readable via `docker inspect` and process listings) — all accepted trade-offs at current scale.
- The Data Protection key-ring fix is deployment-critical regardless of which secrets approach is chosen, since it's about *where keys persist*, not *how secrets are injected*.

**Alternatives Considered**
- **Docker Compose native `secrets:` support** (file-based secrets under `/run/secrets/` instead of env vars): a reasonable incremental improvement over plain env vars, avoiding the `docker inspect`/process-listing exposure — worth adopting if/when the marginal hardening is wanted, without materially changing the overall approach.
- **Infisical (self-hosted, open-source secrets manager):** rejected for now, kept as the natural next step if rotation history, audit, or multi-collaborator access becomes a real need — it has its own docker-compose deployment, so it fits this stack without a platform change.
- **HashiCorp Vault:** rejected — brings real operational weight (unsealing, its own storage backend, access policies) that solves problems this single-operator VPS deployment doesn't have.

---

## ADR-0009: CI/CD via GitHub Actions, building an ARM64 image pushed to GHCR, deployed by SSH

**Status:** Accepted

**Context**
Source control is GitHub; the VPS is an Oracle Cloud **Ampere A1 (ARM64)** instance — confirmed, not x86. Deployment already runs via Docker Compose on that VPS (ADR-0005). GitHub Actions' default hosted runners are x86_64, so producing a working image for this VPS requires either cross-compilation via QEMU or a native ARM64 runner.

**Decision**
- **CI** (every push/PR): run `dotnet build && dotnet test` for the API and `npm ci && npm run build` for the Svelte frontend (ADR-0007), on standard hosted runners. This gates merges but does not touch the VPS.
- **CD** (on push to `main` only):
  1. Build the Docker image via `docker buildx`, targeting **`linux/arm64` only** (not multi-platform), since the VPS is confirmed single-architecture and building only the needed platform avoids paying multi-arch build time for a target that's never deployed. Cross-compilation is done via `docker/setup-qemu-action` on the standard x86_64 hosted runner.
  2. Push the image to **GitHub Container Registry (ghcr.io)**, tagged with both the git SHA and `latest` — the SHA tag is what actually gets deployed and referenced for rollback; `latest` is kept only as a convenience pointer.
  3. SSH into the VPS (credentials from GitHub Actions repo secrets, distinct from the app's own `.env` secrets in ADR-0008) and run `docker compose pull && docker compose up -d --remove-orphans`, with the VPS's `docker-compose.yml`/`.env` referencing the image by the newly built SHA tag.
- Database migrations run via `db.Database.Migrate()` at API startup rather than as a separate pipeline step — safe here specifically because there is exactly one backend instance and SQLite (ADR-0003), so there's no multi-instance migration race to guard against.
- Add a plain `/healthz` endpoint and a Docker `HEALTHCHECK` directive so a deploy's success can be verified by the container actually reporting healthy, not just by the SSH command exiting zero.
- The deploy job is restricted to the `main` branch only (never PRs, never forks) — this is what makes SSH-from-hosted-runner safe: a PR from an untrusted fork never gets a chance to run the deploy job or see its secrets.

**Consequences**
- Every deploy is a single `docker compose pull && up -d` on the VPS — a few seconds of downtime during container restart, which is accepted as fine for a personal/small-user app; no blue-green/zero-downtime complexity is introduced for this.
- Rollback is just re-pointing the compose file at a previous SHA tag and re-running the same deploy steps — no rebuild required, since every image is retained and addressable by SHA.
- Building ARM64-only (rather than multi-platform) keeps CI build time lower, at the cost of needing to revisit this if an x86 deployment target is ever added later.
- GHCR was chosen over Docker Hub purely for convenience — it's already authenticated via the repo's own `GITHUB_TOKEN`, with no separate account/credential to manage.

**Alternatives Considered**
- **Multi-platform build (`linux/amd64,linux/arm64`):** rejected for now — doubles build time for a platform that is never actually deployed, given the VPS architecture is confirmed and fixed. Revisit only if a second, x86 deployment target is ever added.
- **Self-hosted GitHub Actions runner on the VPS itself:** rejected — while it would sidestep the ARM cross-compilation question entirely (build natively on the target architecture), it means PR-triggered workflows could execute on the production VPS itself, which is a materially worse security posture than SSH-deploying from a hosted runner restricted to `main`.
- **A dedicated CD tool (e.g. ArgoCD, Coolify, or a PaaS-style deploy target):** rejected — solves problems (multi-service orchestration, GitOps reconciliation) this single-container-on-a-single-VPS deployment doesn't have; GitHub Actions + SSH is already sufficient and keeps everything in one place alongside the code.

---

## ADR-0010: Shared reverse-proxy layer for hosting multiple apps on one VPS

**Status:** Accepted

**Context**
The Oracle Cloud VPS (ADR-0005, ADR-0009) currently hosts Streak alone on `habit.datmai.net`, but is expected to host additional, unrelated apps in the future — potentially on entirely different domains, not just subdomains of `datmai.net`. Each app should be deployable and manageable independently (own repo, own CI/CD per ADR-0009, own database) without the apps interfering with each other or requiring manual port/certificate juggling per app.

**Decision**
- Run exactly **one** reverse proxy container (Caddy) that binds the VPS's ports 80 and 443. No other container maps ports directly to the host.
- Create one shared external Docker network (e.g. `web`). The proxy joins it; every app's container that needs to receive traffic also joins it — nothing else about an app's internal stack (its own DB container/volume, internal-only services) needs to touch this network.
- Each app keeps its **own** `docker-compose.yml` in its own directory (e.g. `/opt/streak`, `/opt/<app2>`), deployed independently by its own CI/CD workflow (ADR-0009) — the shared network is the only coupling point between apps.
- Routing is configured in a single `Caddyfile`, mapping each domain (or subdomain) to its app's internal service name and port, e.g. `habit.datmai.net { reverse_proxy streak-api:8080 }`. Caddy obtains and renews HTTPS certificates (Let's Encrypt) automatically per domain listed, including entirely unrelated domains — not limited to subdomains of one zone.
- DNS for every app just needs an A record pointing at the same VPS IP; Caddy dispatches by the request's `Host` header, so no per-app IP or port is exposed externally.
- Apply per-app resource limits (`mem_limit`/`cpus` in each app's compose file) so one app can't starve the others on a resource-constrained free-tier VPS.

**Consequences**
- Adding a new app in the future is: point its domain's DNS at the VPS, add one block to the shared `Caddyfile`, deploy the app's own compose stack on the `web` network — no changes needed to other apps.
- The Caddy container becomes a single point of failure for *all* hosted apps (if it's down, everything behind it is unreachable). Accepted at this scale, consistent with the single-VPS/single-operator trade-offs already accepted in ADR-0005 and ADR-0009.
- Certificate management is centralized and automatic rather than per-app — one less thing each app's deployment needs to handle.
- Requires a small amount of manual coordination (editing the shared `Caddyfile` and reloading Caddy) whenever a new app or domain is added — acceptable given how infrequently that happens compared to normal app deploys.

**Alternatives Considered**
- **A separate reverse proxy (or exposed port) per app:** rejected — leads to manual port-conflict management and duplicated TLS certificate handling per app, with no real benefit at this scale.
- **Traefik with Docker-label-based auto-discovery** (routes are declared as labels on each app's own compose file, rather than a central static config file): a legitimate alternative that avoids touching a shared config file when adding an app. Not chosen for now since a single small `Caddyfile` is simpler to reason about and audit with only a handful of apps; worth reconsidering if the number of hosted apps grows large enough that editing one shared file becomes unwieldy.
- **A cloud load balancer / API gateway product:** rejected — unnecessary cost and complexity for personal-scale hosting on a single VPS.

---

## ADR-0011: PWA over native app, with iOS Shortcuts-based quick check-in as a widget substitute

**Status:** Accepted

**Context**
The app is already an SPA (ADR-0001) built with Svelte + Vite (ADR-0007), targeting a single or small group of users rather than App Store distribution. Web Push (ADR-0004) already closes the main capability gap that usually motivates going native — reliable background notifications. However, iOS's PWA implementation has real, confirmed gaps versus native: no home screen widgets, no manifest-declared app-icon shortcuts (`shortcuts` in the web app manifest works on Android/desktop Chrome only, not iOS Safari), and no Live Activities/Dynamic Island — all native-only (WidgetKit/ActivityKit), with no web API equivalent.

**Decision**
- Continue building as a PWA, not a native or cross-platform native app (React Native/Flutter), for the current goal set (personal/small-group daily tracker, not App Store discovery).
- To close part of the "one-tap, home-screen-level convenience" gap left by the lack of real iOS widgets, implement:
  1. A lightweight `/quick/:subjectId` route (and a variant accepting a value for quantity-type subjects) that performs the check-in immediately on load and shows a minimal confirmation screen — no navigation into the full app required. Served fast via the service worker cache so it feels instant even on a cold open. Relies on iOS sharing cookie/session storage between Safari and the installed PWA for the same origin, so no separate auth flow is needed.
  2. Document/guide users to create one iOS **Shortcuts app** shortcut per frequently-tracked subject, each pointing at its `/quick/:subjectId` URL, addable to the home screen with its own icon and name — the closest available approximation to a per-habit widget without native code.
  3. Optionally attach a Siri phrase to each Shortcut (voice check-in) and/or bind one to the **Action Button** on supported iPhones.
  4. Use the **Badging API** (`navigator.setAppBadge()`), which iOS Safari does support for installed PWAs, to show a glanceable "N subjects still need logging today" count on the installed app icon — set from the service worker on push delivery, or when the app is foregrounded.

**Consequences**
- Closes most of the practical "quick access from the home screen" gap without any native code, App Store account, or second codebase/pipeline.
- Does **not** deliver a true dynamic widget — a Shortcuts-app icon is static (name/icon only, no live glanceable text or graphics); the Badging API only conveys a single number, not rich content.
- Shortcuts must be set up manually by each user, once per subject — there's no way to auto-provision or bulk-install them, since iOS doesn't expose that to web apps. This should be documented as a short in-app "add a quick check-in shortcut" guide rather than assumed to be discovered.
- If real dynamic widgets, Live Activities, or App Store discoverability become actual product goals later, the natural next step is wrapping the existing Svelte app with **Capacitor** for a native shell (adding native capabilities incrementally around the existing web codebase) rather than a full native rewrite.

**Alternatives Considered**
- **Building a native or cross-platform native app now:** rejected — no current goal (App Store distribution, dynamic widgets) justifies the cost of two additional codebases and CI/CD pipelines beyond what ADR-0009 already covers, for capability gaps that don't affect the app's core daily-check-in use case.
- **Wrapping with Capacitor now, solely to get static widget/shortcut capability:** rejected for now — introduces a native build/signing/provisioning pipeline for a capability the Shortcuts-app approach already approximates closely enough, without that overhead. Revisit if the gap becomes a real product pain point rather than a nice-to-have.

