# Database Schema — Streak (ASP.NET Core + EF Core + ASP.NET Core Identity)

**Last updated:** 2026-09-06

## 1. Conventions

- **Identity tables** (`AspNetUsers`, `AspNetRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`, `AspNetUserRoles`) are the default EF Core Identity scaffold — reused as-is, not reinvented. `ApplicationUser : IdentityUser` is extended with a small number of app-specific columns (see §2.1) rather than creating a separate "profile" table, since the 1:1 relationship needs nothing more than a few extra columns.
- Identity's primary keys are `string` (a GUID stored as text), per the default `IdentityUser` template. All custom tables use `Guid` (`uniqueidentifier` in SQL Server, `TEXT` in SQLite) primary keys, and foreign keys to Identity users are `string` to match `AspNetUsers.Id` exactly.
- Every custom table that participates in sync (ADR-0002) carries `UpdatedAt` for last-write-wins reconciliation.
- Roles/claims/logins/tokens exist because Identity scaffolds them by default; the app doesn't currently assign custom roles (single-tier user model) but the tables are kept for future use (e.g. an "admin" role) at no extra cost.

## 2. Tables

### 2.1 `AspNetUsers` (Identity default + app extensions)

Default Identity columns (unchanged): `Id`, `UserName`, `NormalizedUserName`, `Email`, `NormalizedEmail`, `EmailConfirmed`, `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `PhoneNumber`, `PhoneNumberConfirmed`, `TwoFactorEnabled`, `LockoutEnd`, `LockoutEnabled`, `AccessFailedCount`.

App-specific additions on `ApplicationUser`:

| Column | Type | Notes |
|---|---|---|
| `TimeZoneId` | `nvarchar(64)`, nullable | IANA timezone id; used to decide the user's local "day boundary" (see PRD §9, timezone handling). |
| `RemindersEnabled` | `bit`, default `0` | Whether the user has opted into push reminders. |
| `ReminderTime` | `time`, nullable | Local time-of-day to check "did they log today" and send a reminder if not. |
| `CreatedAt` | `datetime2` | Account creation timestamp. |
| `SyncVersion` | `bigint`, default `0` | Monotonic counter, incremented atomically by the server on every accepted `POST /sync`. Powers the optimistic-concurrency check that prevents a stale device flush from overwriting newer changes (ADR-0002) — never set from client input. |

### 2.2 `AspNetRoles`, `AspNetRoleClaims`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`

Unmodified Identity defaults, reused directly:

- `AspNetRoles`: `Id`, `Name`, `NormalizedName`, `ConcurrencyStamp`.
- `AspNetRoleClaims`: `Id`, `RoleId` (FK → `AspNetRoles.Id`), `ClaimType`, `ClaimValue`.
- `AspNetUserRoles`: `UserId` (FK → `AspNetUsers.Id`), `RoleId` (FK → `AspNetRoles.Id`) — composite PK, join table for the many-to-many user↔role relationship.
- `AspNetUserClaims`: `Id`, `UserId` (FK), `ClaimType`, `ClaimValue`.
- `AspNetUserLogins`: `LoginProvider`, `ProviderKey` (composite PK), `ProviderDisplayName`, `UserId` (FK) — this is where Google OAuth's external login record lives (`LoginProvider = "Google"`).
- `AspNetUserTokens`: `UserId`, `LoginProvider`, `Name` (composite PK), `Value` — used internally by Identity (e.g. external login tokens); WebAuthn/passkey credentials are stored in Identity's own passkey table, **not** here (see §2.6).

### 2.3 `Subjects`

| Column | Type | Notes |
|---|---|---|
| `Id` | `uniqueidentifier` PK | |
| `UserId` | `string(450)` FK → `AspNetUsers.Id` | Not nullable. |
| `Name` | `nvarchar(40)` | |
| `Type` | `tinyint` | `0 = Check`, `1 = Quantity`. Mapped to a C# enum via `HasConversion`. |
| `Unit` | `nvarchar(12)`, nullable | Only meaningful when `Type = Quantity`. |
| `Goal` | `decimal(10,2)`, nullable | Daily goal; a day only counts toward the streak once `Value >= Goal`, if set. |
| `SortOrder` | `int` | User-controlled ordering (reorder feature). |
| `Fires` | `tinyint` | 0–3, current banked fires. Server-authoritative mirror of the client's fire state. |
| `LastMilestone` | `int` | Last streak-length multiple-of-5 at which a fire was awarded; prevents re-awarding on recompute. |
| `LongestStreak` | `int` | Best-ever streak, retained across resets. |
| `CreatedAt` | `datetime2` | |
| `UpdatedAt` | `datetime2` | Used for sync reconciliation (ADR-0002). |

**Indexes:** `IX_Subjects_UserId`; `IX_Subjects_UserId_SortOrder` (composite, supports ordered list retrieval).

### 2.4 `Entries`

One row per subject per logged (or fire-filled) day.

| Column | Type | Notes |
|---|---|---|
| `Id` | `uniqueidentifier` PK | |
| `SubjectId` | `uniqueidentifier` FK → `Subjects.Id` | `ON DELETE CASCADE` — deleting a subject removes its history (per PRD §7.1). |
| `Date` | `date` | The calendar day this entry represents (in the subject owner's timezone). |
| `Value` | `decimal(10,2)`, nullable | `1` for a completed check-type day; the logged number for quantity types. `NULL` when `IsFireFilled = true` and no real value was logged. |
| `IsFireFilled` | `bit`, default `0` | `true` if this day was covered by spending a fire rather than a real log. |
| `CreatedAt` | `datetime2` | |
| `UpdatedAt` | `datetime2` | Sync reconciliation. |

**Constraints:** unique index `UX_Entries_SubjectId_Date` on (`SubjectId`, `Date`) — one entry per subject per day.
**Indexes:** `IX_Entries_SubjectId_Date` (supports the range queries the grid needs, e.g. "last 60 days for this subject").

### 2.5 `PushSubscriptions`

One row per device/browser a user has enabled reminders on (ADR-0004 — subscriptions are per device, not per user).

| Column | Type | Notes |
|---|---|---|
| `Id` | `uniqueidentifier` PK | |
| `UserId` | `string(450)` FK → `AspNetUsers.Id` | |
| `Endpoint` | `nvarchar(500)` | The push service URL returned by `pushManager.subscribe()`. |
| `P256dh` | `nvarchar(200)` | Public key from the subscription, used to encrypt the push payload. |
| `Auth` | `nvarchar(100)` | Auth secret from the subscription. |
| `DeviceLabel` | `nvarchar(100)`, nullable | Optional human-readable label (e.g. "iPhone — Safari"). |
| `CreatedAt` | `datetime2` | |
| `LastSeenAt` | `datetime2` | Updated whenever the subscription is confirmed still valid (e.g. on `pushsubscriptionchange` re-registration). |

**Constraints:** unique index `UX_PushSubscriptions_Endpoint` — a given browser subscription endpoint is only ever stored once.

### 2.6 `AspNetUserPasskeys` (Identity built-in, .NET 10+)

As of .NET 10, ASP.NET Core Identity has **built-in passkey (WebAuthn) support** — no custom credential table or third-party library is needed for this app's requirements (see ADR-0006). When `AddEntityFrameworkStores<ApplicationDbContext>()` is used with a recent Identity package version, EF Core scaffolds a passkey store table automatically, roughly:

| Column | Type | Notes |
|---|---|---|
| `UserId` | `string(450)` FK → `AspNetUsers.Id` | |
| `CredentialId` | `varbinary(max)` | The WebAuthn credential ID, unique per passkey. |
| `PublicKey` | `varbinary(max)` | Stored public key used to verify future sign-in assertions. |
| `Name` | `nvarchar` | User-friendly label (e.g. "My iPhone"), settable post-registration. |
| `SignCount` | `int` | Signature counter, used to detect cloned-authenticator replay attacks. |
| `IsBackedUp` | `bit` | Whether the passkey provider reports this credential as backed up/synced. |
| `IsUserVerified` | `bit` | Whether the authenticator performed user verification (biometric/PIN) at registration. |
| `CreatedAt` | `datetime2` | |

This table is managed entirely by Identity's `UserManager.AddOrUpdatePasskeyAsync` — the app does not write migrations or repository code for it directly, only calls the `SignInManager`/`UserManager` APIs (`MakePasskeyCreationOptionsAsync`, `PerformPasskeyAttestationAsync`, `MakePasskeyRequestOptionsAsync`, `PasskeySignInAsync`) from its own two thin endpoints (creation-options + registration-submit, request-options + sign-in-submit).

**Requirements carried into implementation (not schema, but worth noting here):** all passkey operations require HTTPS (satisfied by ADR-0005); `IdentityPasskeyOptions.ServerDomain` should be set explicitly; Identity does not validate attestation statements by default (acceptable for this app's threat model).

A third-party library (`Fido2NetLib`) remains available for apps needing full general-purpose WebAuthn support beyond Identity's authentication-scoped implementation, but is not required here.

## 3. Relationship Summary

- One `AspNetUser` → many `Subjects` (owns).
- One `Subject` → many `Entries` (has), cascade delete.
- One `AspNetUser` → many `PushSubscriptions` (registers).
- One `AspNetUser` → many `AspNetUserPasskeys` (Identity built-in, .NET 10+).
- One `AspNetUser` → many `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens` (Identity defaults).
- One `AspNetRole` → many `AspNetRoleClaims`.
- `AspNetUser` ↔ `AspNetRole`: many-to-many via `AspNetUserRoles`.

## 4. Notes on sync (ADR-0002) and fire logic

- The server treats `Subjects` and `Entries` as the source of truth; `Fires`, `LastMilestone`, and `LongestStreak` are recomputed and persisted server-side whenever entries change (mirroring the same client-side logic already implemented in the PWA), so a fresh device pull always gets correct, authoritative values rather than trusting the client's math.
- `POST /sync` should accept the full local state (subjects + entries, each with `UpdatedAt`) plus a `baseVersion` integer, upsert rows where the incoming `UpdatedAt` is newer, and return the merged, authoritative state — including any server-recomputed `Fires`/`LongestStreak` values — so the client can reconcile in one round trip.
- **Concurrency check implementation:** do the compare-and-increment of `AspNetUsers.SyncVersion` as a single atomic statement rather than a separate read-then-write (which would itself have a race window), e.g. `UPDATE AspNetUsers SET SyncVersion = SyncVersion + 1 WHERE Id = @userId AND SyncVersion = @baseVersion`. Zero rows affected means the base version was stale — return `409 Conflict` without touching `Subjects`/`Entries` at all; one row affected means the write proceeds (within the same transaction) and the new `SyncVersion` is returned to the client for its next sync.
