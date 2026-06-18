# Navigation & Information-Architecture Proposal

> **Status: PROPOSED — awaiting user sign-off.** This is a design proposal, not built
> functionality. `docs/ARCHITECTURE.md` remains the source of truth for what exists.
> Once signed off, the build items below (WORKLOG `P-1a/b/c`, `2-16`, `2-17`) move from
> `Blocked` to `Available`. Agents: do not build from this doc until the board says so.

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

Group the flat links into purpose clusters (MudNavGroup), and introduce a **Player Profile
hub** that consolidates the per-player views.

```
Dashboard                     (landing — unchanged)
Play
  ├── Record Round
  └── Rounds                  (history; Live Round resumes from here)
My Golf
  ├── My Profile              (NEW hub — see §3)
  └── AI Coach
Directory
  ├── Clubs & Courses
  ├── Players
  └── Societies
```

`My Stats` and `My Handicap` stop being separate top-level links — they become tabs/sections
inside **My Profile**.

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

**v2**: compute net double bogey using the player's Course Handicap and `Hole.StrokeIndex`.
Complications to handle (documented for whoever picks it up):
- Course Handicap needs an index, but the index is what we're computing → process rounds
  **oldest-first using the running index**, and fall back to the v1 par+5 cap for the earliest
  rounds before an index is established.
- Needs per-hole `StrokeIndex` (already on `Hole`) and the tee's rating/slope (already stored).
- This is a behaviour change to everyone's computed index (generally lowering higher handicaps),
  so it should ship with a backfill re-run and a clear changelog note.

Tracked as WORKLOG `2-17`. Independent of the IA decision — can proceed on its own.

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

## 7. Build decomposition (seeded on the board, Blocked pending sign-off)

| Item | Scope |
|------|-------|
| `P-1a` | Player Profile hub (§3) — consolidate stats + handicap + recent rounds; per-player incl. managed; replaces My Stats + My Handicap nav |
| `P-1b` | Player nav grouping (§2) — MudNavGroup clusters |
| `P-1c` | Admin dashboard grouping (§6) |
| `2-16` | Handicap transparency (§4) — qualifying/excluded rounds + explainer + expectation note (supersedes 2-15) |
| `2-17` | WHS v2 adjusted gross = net double bogey (§5) — independent; not gated on sign-off |

## 8. Open questions for sign-off

1. **Profile hub URL/shape** — one page with tabs (Overview/Stats/Handicap/Rounds), or sections
   on a single scroll? Tabs proposed.
2. **Managed players** — reach a managed player's profile via the Players list → their row →
   Profile? (Proposed.)
3. **Nav cluster names** — "Play / My Golf / Directory" working titles; happy to rename.
4. **v2 adjusted gross (2-17)** — proceed now (more accurate, lowers your index toward 27), or
   hold until the transparency UI lands so the change is visible/explained? Recommend doing 2-17
   *with* 2-16 so the more-accurate number ships alongside its explanation.
