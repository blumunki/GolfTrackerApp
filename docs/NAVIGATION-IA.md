# Navigation & Information-Architecture Proposal

> **Status: APPROVED & BUILT (2026-06-15).** `P-1a/b/c`, `2-16`, `2-17` are Done, plus the
> `P-3` review fixes (Overview home, embedded Stats, 18-hole transparency denominator,
> Club-Course labels, grouped admin side-nav, player tiles → profiles, connected-tile
> handicaps). `docs/ARCHITECTURE.md` is the source of truth for what now exists; this doc is
> kept as the design rationale. **Remaining:** `P-2` — Record Round in the top app bar +
> mobile bottom-nav parity (§2). This doc no longer gates work; follow the WORKLOG board.

## 1. Why

Two related problems have emerged as the app has grown:

1. **Page sprawl.** The player-facing nav has 9 flat links and the admin dashboard has 13
   flat tiles. Related things aren't grouped, and useful tools get lost — e.g. the Handicap
   Backfill tile exists but is one of 13, so it reads as "missing."
2. **The handicap page shows numbers without meaning.** Real example from production data
   (a 20-round player): the page shows **13** differentials with no explanation that **7
   rounds were excluded** (played on Chalgrave Manor, Tilsworth and Belton Woodside, whose
   tees have no slope rating yet). It shows an index of **31.5** with no explanation of how
   it's derived (average of the lowest 4 of those 13) or why it differs from the player's
   society handicap of 27. The audience is **amateur golfers**, so the calculation must be
   legible and set expectations.

## 2. Player-facing navigation

**Recording a round is the app's primary, most-frequent action (~90%+ of visits — it's why
users return).** So the IA is organised by **frequency, not by symmetric categories**: Record
Round is a standout primary action that is never nested, the everyday views sit flat beneath
it, and only the lower-frequency reference data is grouped.

```
┌────────────────────────────┐
│  ➕  Record Round          │   primary CTA — highlighted, pinned to the top of the nav AND
└────────────────────────────┘   present in the top app bar, so it's one tap from every page
  Dashboard
  Rounds                        your history; resume an in-progress live round from here
  My Profile                    stats + handicap, tabbed (§3)
  AI Coach
  ── Directory ──               collapsible group — reference data, visited less often
    Clubs & Courses
    Players
    Societies
```

Why this shape (addressing the earlier Play / My Golf / Directory objection):

- **Record Round stays top-level and prominent** — never buried under a group. A persistent
  Record Round action in the top app bar keeps the #1 feature one tap away everywhere. When a
  round is already in progress it reads **"Resume Live Round"** and the Dashboard surfaces it,
  so getting back to live scoring is immediate. (Live vs after-the-fact entry are two ways into
  the *same* recording workflow, not separate nav items.)
- **Everyday views are flat** (Dashboard, Rounds, My Profile, AI Coach) — no extra clicks for
  things people use often.
- **Only one group** (Directory) for the reference data a user touches rarely. We deliberately
  do *not* force every link into a category — the symmetric three-group scheme demoted Record
  Round and was tidy at the expense of the core journey.
- `My Stats` and `My Handicap` stop being separate top-level links — they fold into **My
  Profile** (§3).
- **Mobile parallel**: the MAUI bottom nav mirrors this — Record Round as the prominent centre
  action with Dashboard / Rounds / Profile around it. Mobile nav is a separate follow-up, noted
  here so web and mobile stay consistent.

## 3. Player Profile hub

A single parameterised page (`/players/{id}/profile`, with `/profile` resolving to the
current user's own player) that houses everything about one golfer:

- **Overview**: name, primary handicap (with source), headline stats.
- **Stats**: today's "My Stats" content (scoring distribution, par performance, trends).
- **Handicap**: today's "My Handicap" content (active handicaps, index history, differentials)
  **plus the transparency additions in §4**.
- **Recent rounds**.

Crucially it works for **any player the user may view**, not just themselves — this covers the
**managed-player** case (e.g. a parent who logs a child's rounds as a managed player with no
login). The handicap engine and report services already compute per-player and the API is
already owner-or-self authorised, so this is largely UI/navigation work.

Supersedes WORKLOG `2-13`.

## 4. Handicap transparency (the §1.2 problem)

On the handicap section of the profile, for amateur-golfer legibility:

1. **Qualifying vs excluded rounds.** Show a clear count ("13 of your 20 completed 18-hole
   rounds count") and an expandable list of **excluded** rounds **with the reason**:
   - *No course rating/slope* (e.g. Chalgrave, Tilsworth, Belton Woodside until rated)
   - *Not 18 holes*
   - *Incomplete scorecard*
   This directly answers "why only 13?".
2. **How the index is derived.** A plain-English explainer: "Your index is the average of
   your lowest **N** Score Differentials from your last 20 rounds. With 13 rounds counting,
   we use your lowest 4." Tie it to the "Counting" rows already shown.
3. **What a differential is.** One sentence: how a round's score becomes a differential
   (`(113 ÷ Slope) × (Adjusted Gross − Course Rating)`), in friendly terms.
4. **Expectation-setting.** A short note that this is a **WHS-style estimate** computed from
   the rounds recorded here, and may differ from an official club/society handicap (different
   round sets, and the adjusted-gross simplification in §5). This pre-empts "why isn't it 27?".

Supersedes WORKLOG `2-15`. Buildable into whichever page survives §3, so it follows the IA
decision.

## 5. Handicap accuracy — adjusted gross v1 → v2 (the 31.5-vs-27 gap)

The biggest driver of the index being higher than the player's real (society) handicap is the
**Adjusted Gross Score** method. Today (v1, per ARCHITECTURE §12.5 4.3) every hole is capped at
**par + 5** — the WHS rule for a player with *no established index*. WHS for an *established*
player caps each hole at **net double bogey** (`par + 2 + handicap strokes received`), which for
a ~27-handicap is roughly par + 3 on most holes — materially lower than par + 5. So v1
systematically inflates adjusted gross, differentials, and the index for established higher
handicaps.

**v2 (signed off): compute the handicap "properly", the way a club/association would** — net
double bogey using the player's Course Handicap and `Hole.StrokeIndex`, so the result is
comparable to an official WHS handicap. Details for whoever builds it:

- Net double bogey per hole = `par + 2 + handicap strokes received on that hole`, where strokes
  received come from the player's Course Handicap distributed by `Hole.StrokeIndex`.
- Course Handicap needs an index, but the index is what we're computing → process rounds
  **oldest-first using the running index** (the backfill already runs oldest-first). For the
  earliest rounds before any index exists, fall back to the `par + 5` cap — this matches how WHS
  treats a player with no established index, so it stays "proper".
- Inputs already exist: per-hole `StrokeIndex` on `Hole`, and tee rating/slope on `TeeSet`.

### Recalculating after v2 ships (no data reset, no migration)

v2 is a **calculation change only** — no schema change. To recompute everyone's handicaps with
the proper method:

1. Make sure tees have rating + slope (sync `TeeSets.csv`; fill any remaining `N/A` slopes for
   courses you want to count).
2. Deploy the v2 build.
3. Run **Admin → Handicap Backfill**. It re-derives every Score Differential per round
   (oldest-first, now via net double bogey) and rebuilds index history. It's idempotent and
   upserts per `(player, round)`, so it simply overwrites the old v1 values — safe to re-run.
4. Spot-check a profile (e.g. the worked 31.5 example should drop toward the ~27 mark).

Tracked as WORKLOG `2-17`. Independent of the nav decision — can proceed on its own.

## 6. Admin information architecture

Keep all admin pages, but group the dashboard (and optionally an admin sub-nav) into sections so
tools are findable:

```
People       Users · Players · Connections & Merges · Notifications
Golf Data    Content Health · Data Migration · Handicap Backfill
System       Database Migrations · System Health · Settings · Audit Trail
AI           AI Providers · AI Usage
```

Supersedes the "discoverability" half of P-1.

## 7. Build decomposition (all Done — see WORKLOG)

| Item | Scope | Status |
|------|-------|--------|
| `P-1a` | Player Profile hub (§3) — `/players/{id}/profile` + `/profile`, tabs reuse PlayerReport + HandicapPanel | ✅ Done |
| `P-1b` | Player nav (§2) — frequency-first, Record Round primary CTA, Directory group | ✅ Done |
| `P-1c` | Admin grouping (§6) — dashboard + side-nav (`P-3`) | ✅ Done |
| `2-16` | Handicap transparency (§4) — qualifying/excluded rounds + explainer + expectation note | ✅ Done |
| `2-17` | WHS v2 adjusted gross = net double bogey (§5) | ✅ Done |
| `P-2` | Record Round in the top app bar + mobile bottom-nav parity (§2) | ⏳ Available |

## 8. Sign-off status

| Decision | Outcome |
|----------|---------|
| Profile hub shape | ✅ **Tabs** (Overview / Stats / Handicap / Rounds). Revisit long-scroll if disliked. |
| Managed players | ✅ Reached via Players list → row → Profile. |
| Admin grouping (§6) | ✅ Approved. |
| v2 adjusted gross (§5) | ✅ Approved — "done properly", comparable to a club/association WHS. Ship **with** the transparency UI (`2-16`) so the more-accurate number lands with its explanation. |
| Transparency (§4) | ✅ Approved (the priority). |
| **Player nav (§2)** | ⏳ **Revised to frequency-first (Record Round as primary CTA, single Directory group) — awaiting confirmation of this revision before building `P-1b`.** |
