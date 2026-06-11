# Running AI Agent Sessions — Owner's Guide

How to get a new agent session (Claude Code, OpenAI Codex, or Gemini) productive in one prompt, and what to check when it finishes. The agents read their own instruction files automatically (`CLAUDE.md` for Claude, `AGENTS.md` for Codex, `GEMINI.md` for Gemini — they're identical); your job is just to point them at the right work.

## The kickoff prompt

Paste this to start a session, choosing one of the three bracketed options:

```
Read your agent instructions file and docs/CONTRIBUTING-AGENTS.md, then open docs/WORKLOG.md.

[Pick up the next Available item that isn't Blocked.]
[Pick up item <ID>.]
[Item <ID> is in Handoff from a previous session — read its Handoff Notes and continue it.]

Follow the Working Agreement: claim the item on the board (Status=In Progress, Owner=your
agent name) and commit that before anything else; stay within that one item; read the
relevant section of docs/ARCHITECTURE.md before coding; run `dotnet test` before every
commit. When finished, set the item to Done (or Handoff with notes), make sure everything
is committed and tests are green, then summarise what you did and what you'd pick next.
```

If you just want a status overview before deciding:

```
Read docs/WORKLOG.md and summarise the board: what's Done, In Progress, Handoff,
Available and Blocked. Recommend what to pick up next and why, then wait for my go-ahead.
```

## While the agent works

- One item per session. If you think of new work mid-session, ask the agent to **add it to WORKLOG.md as a new Available item**, not to do it now.
- If the agent proposes expanding scope, the right answer is almost always "add it to the board".

## When the agent says it's finished — your checklist

1. `git status` — working tree clean, everything committed.
2. `dotnet test GolfTrackerApp.Web.Tests/GolfTrackerApp.Web.Tests.csproj` — green.
3. `docs/WORKLOG.md` — the item's row says Done (or Handoff with a proper Handoff Notes entry).
4. If architecture or functionality changed: `docs/ARCHITECTURE.md` updated in the same work (including the §12.0 status table).
5. Happy? `git push` — that runs CI, and deploys to Azure if web code changed.

## If a session dies mid-task (token limit, crash)

Start a new session — same agent or a different one — with:

```
Item <ID> in docs/WORKLOG.md was In Progress in a previous session that ended early.
Check git status and git log to see what was actually done, update the board and
Handoff Notes to match reality, then continue the item.
```

## Branching — keep it simple (solo developer)

- **Day-to-day: work directly on `main`, locally.** Commit small, test before pushing, push when happy. No PRs — pushing `main` is the release step (CI runs, and the Azure deploy fires only when `GolfTrackerApp.Web/` changed, now gated on tests passing).
- **Risky or multi-session work only** (production DB baselining, the namespace-rename PR, big design passes): use a short-lived local branch, merge it into `main` locally when green, delete the branch. Still no PR needed.
- Never let a branch live longer than the piece of work it exists for.
