# Product Requirements Document — Streak

**Status:** Draft
**Owner:** (you)
**Last updated:** 2026-09-06

## 1. Summary

Streak is a lightweight, mobile-first habit tracker. Users define "subjects" (things they want to do daily — either a simple check-off or a measured quantity like hours or steps), log them once a day, and maintain a visual streak. Missing a day breaks the streak unless the user spends a banked "fire" — a limited, earned resource — to cover the gap. The app is delivered as an installable PWA with its own backend for account sync and push-based reminders.

## 2. Problem Statement

Most habit trackers are either too heavyweight (goal templates, social features, subscriptions) or too disconnected from daily use (no reminder, no visual payoff for consistency). This product intentionally optimizes for one thing: making the daily check-in fast, visual, and slightly game-ified — without turning into a full gamification/social product.

## 3. Goals

- A daily check-in should take under 3 seconds from opening the app.
- The GitHub-style contribution grid should make consistency (and gaps) immediately visible.
- The "fire" mechanic should reward real consistency without letting one missed day destroy months of history.
- The app should work offline and feel instant — no action should visibly wait on a network call.
- The app should be usable across devices via a real account (not locked to one browser's local storage).

## 4. Non-Goals (for this version)

- No social features (following, sharing, leaderboards).
- No habit "templates" or coaching content.
- No complex analytics dashboards beyond the grid and basic stats.
- No real-time multi-user collaboration on the same subject.

## 5. Target Users

Primarily a single user (or a small handful of people, each with their own private account) who wants a private, fast, no-friction daily tracker — not a social or team product.

## 6. Core Concepts

| Concept | Definition |
|---|---|
| Subject | A thing being tracked daily (e.g. "Guitar practice", "Steps"). Has a type: `check` or `quantity`. |
| Check subject | Logged as done/not-done for a given day. |
| Quantity subject | Logged with a numeric value per day (e.g. hours, steps). Optionally has a daily goal; if set, a day only counts once the value meets the goal. |
| Streak | Consecutive days (ending today, with a same-day grace period) where a subject was logged (or fire-filled). |
| Fire | An earned "streak save." One fire is banked per 5-day streak, capped at 3 per subject. Spending a fire retroactively marks a missed day as covered, preserving the streak, without fabricating a real log entry. |
| Best streak | The longest streak ever achieved for a subject, retained even after the current streak resets. |

## 7. Functional Requirements

### 7.1 Subjects
- Users can create a subject with a name and a type (`check` or `quantity`).
- Quantity subjects have an optional unit label (e.g. "hrs", "steps") and an optional daily goal.
- Users can edit a subject's name, unit, and goal after creation. Type is not editable after creation.
- Users can delete a subject (with confirmation), which removes all its history.
- Users can reorder subjects (move up/down).

### 7.2 Daily logging
- Check subjects: single tap marks today done/undone.
- Quantity subjects: tapping opens a numeric input for today's value; existing values can be edited or cleared.
- Any past day (not just today) can be logged or edited directly.

### 7.3 Streak & grid
- Each subject displays a GitHub-style contribution grid (default: last 7 days compact view, expandable to a scrollable ~20-week history).
- Grid cell intensity for quantity subjects scales relative to the maximum value logged in the **trailing 60 days** (not all-time), so one outlier day doesn't visually flatten future entries.
- Current streak and best-ever streak are both displayed per subject.
- A day counts toward the streak if: a check subject was marked done, a quantity subject's value is greater than 0 (or ≥ goal, if a goal is set), or the day was covered by a fire.

### 7.4 Fires
- Every full 5-day streak banks 1 fire, up to a maximum of 3 per subject.
- A missed (empty) past day can be covered by spending 1 fire, marking it visually distinct (flame icon) from a real log entry.
- The first time a user ever earns a fire, a one-time explanatory sheet is shown (not repeated). Subsequent fire-earns show a brief toast only.

### 7.5 Onboarding
- First-time users (no existing data, first-ever load) see a short welcome flow explaining: add a subject, log daily, don't break the chain (fires). A CTA leads directly into subject creation. Dismissible; never shown again once seen.

### 7.6 Authentication
- Users can sign in via: Google OAuth, email/username + password, or a passkey (WebAuthn).
- Accounts allow the same data to sync across devices.

### 7.7 Sync
- All actions apply instantly to local storage; no user-facing action waits on a network round trip.
- Local state syncs to the backend in the background (debounced), and is pulled on app open.
- The app remains fully usable offline; sync resumes when connectivity returns.

### 7.8 Notifications
- Users can opt in to a daily reminder if they haven't logged a subject by a given time.
- Reminders are delivered via Web Push so they work even when the app is closed, on supported platforms.
- Users can disable reminders at any time.

## 8. Non-Functional Requirements

- **Performance:** No heavy frameworks or client-side libraries; the app must render and respond instantly on a mid-range mobile browser.
- **Offline-first:** Core logging functionality must work with no network connection.
- **Installability:** Must meet PWA installability criteria (manifest, service worker, HTTPS) on both Android and iOS.
- **Privacy:** Each user's data is private to their account; no data is shared between accounts.
- **Data portability:** Users can export their full data as JSON, and import it back.

## 9. Platform Constraints (known, accepted)

- iOS only supports Web Push for PWAs installed to the home screen (iOS 16.4+); it does not work in a regular Safari tab. This will be communicated in-app.
- SQLite (the chosen database, see ADR) does not support multiple concurrently-writing app server replicas; this is acceptable at current expected scale (single VPS, single backend instance).

## 10. Success Metrics (directional, not instrumented yet)

- % of days with at least one log, per active user.
- Fire usage rate (are fires being used as intended, or never touched/always exhausted?).
- Day-7 and day-30 retention after first subject creation.

## 11. Open Questions

- Should quantity-subject daily goals be editable retroactively in a way that recalculates past streaks, or only apply going forward?
- Should there be a limit on the number of subjects per user, given grid rendering cost?
- Do we need a "pause" state for a subject (e.g. traveling, injured) that doesn't break a streak but also doesn't require fires?
- Multi-device push: if a user is logged in on two devices, do both receive every reminder, or only one "primary" device?

## 12. Future Considerations (explicitly out of scope now)

- Multi-device real-time sync/collaboration.
- Home-screen widgets for one-tap logging.
- Weekly/monthly summary views beyond the contribution grid.
