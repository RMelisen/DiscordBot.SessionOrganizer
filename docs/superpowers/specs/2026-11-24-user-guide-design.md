# User guide for S.Y.N.C.S. — French website on GitHub Pages

## Context

`/help` (`ProjectSYNCS/Commands/Core/HelpModule.cs`) is French but confined to three embeds near
Discord's 6000-char cap: one line per command, no room to explain how things work. `README.md` is
complete (78 KB) but English and developer-facing. Members need something in between.

Owner's decisions:
- **Purpose: both** — a « Commencer ici » page for newcomers, then detailed topic pages.
- **Format: GitHub Pages site** built from French Markdown in the repo (repo is public), linked
  from `/help`.
- **Secrets stay secret** — same rule as `/help`/README: no easter eggs, no personality triggers;
  her personality gets a light "she talks and reacts" mention only.

## Approaches (considered)

- **GitHub Pages + MkDocs Material** (picked): Markdown sources, search, mobile layout, French UI,
  free, printable to PDF from the browser.
- Markdown linked on github.com: zero setup but reads like a code repo, no cross-page search,
  weaker on phones.
- Downloadable PDF: hard to keep current, poor on phones.

## Design

### Where the files go
- `guide/` at repo root — the site sources (kept out of `docs/`, which holds contributor/agent
  notes that must not be published).
- `mkdocs.yml` at repo root: `docs_dir: guide`, `theme: material` with `language: fr`, light/dark
  toggle, search, `navigation.sections`, `content.code.copy`; `strict: true` so broken links fail
  the build. `site_url: https://rmelisen.github.io/DiscordBot.SessionOrganizer/`.
- `.github/workflows/guide.yml`: on push to `main` with paths `guide/**`, `mkdocs.yml`,
  the workflow itself (+ `workflow_dispatch`); `pip install mkdocs-material` (pinned version),
  `mkdocs build --strict`, then `actions/upload-pages-artifact` + `actions/deploy-pages`.
  Actions pinned to commit SHAs like `addon-image.yml`; `permissions: pages: write, id-token: write`.
  The existing image workflow only triggers on `ProjectSYNCS/**`, so guide edits don't rebuild
  the image.

### Pages (French, tutoiement, SYNCS speaking in first person like `/help` — my call; lines in
her voice follow `docs/syncs-voice.md`)
1. `index.md` — **Commencer ici**: who SYNCS is, the 6 things to try first (`/schedule create`,
   `/when`, `/level`, `/plynling adopt`, `/arcade`, `/tonight`), how XP/cailloux tie it together,
   where to go next.
2. `sessions.md` — `/schedule`, `/tonight`, `/teams`, `/remindme`/`/reminders`, buttons on a
   session card, reminders, weekly sessions.
3. `sondages.md` — `/when`, `/vote`, `/giveaway`.
4. `niveaux.md` — XP (messages, voice), `/level`, badges/« Succès », `/recap`, `/leaderboard`
   views and windows, `/emotestats`, `/goodbot`.
5. `honte.md` — `/shame` titles and votes.
6. `quiz.md` — pop quiz rules, `/quiz leaderboard`.
7. `cailloux.md` — earning/spending cailloux, `/inventory`, shops, items, cosmetics, trades.
8. `plynlings/` (biggest topic, split): `index.md` (adopt, needs, care basics), `soins.md`,
   `visites-et-relations.md`, `entrainement.md` (RPG, Énergie, `/plynling train`),
   `village.md` (village events, light lore from `docs/village-lore.md`), `mort.md` (death,
   tombstones, resurrection).
9. `arcade.md` — every `/arcade` game, stakes, lottery, Quokka in the bots channel.
10. `minecraft.md` — `/minecraft link · invite · status…`, what she understands in game.
11. `amusement.md` — `/yesno`, `/rate`, `/ship`, `/blame`, `/wouldyourather`, `/confess`.
12. `staff.md` — `/admin`, `/config`, `/report`, `/health` (owner-only `/debug` excluded).
13. `faq.md` — short Q&A (why didn't I get XP in this channel, my button looks dead, etc.).

Sources for content, checked against code: README Features sections, `HelpModule`,
`PlynlingModule.Help.cs`, `MinecraftModule` help, `ReportModule` help, and the subsystem notes
in `docs/agents/` (read before writing each page). No screenshots in v1 (can be added later in
`guide/assets/`).

### Link from Discord
- `HelpModule`: a `const string GuideUrl`, and a 4th button in `BuildButtons` —
  `ButtonBuilder.CreateLinkButton("Guide complet", GuideUrl, emote 📖)` (link buttons carry no
  custom-id, so no `COMPONENT_CUSTOM_ID_DUPLICATED` risk; row stays ≤ 5). Also one line in the
  embed footer or description pointing to it.
- Bump `ProjectSYNCS/config.yaml` 8.13.1 → **8.13.2** (patch: a tweak) + `CHANGELOG.md` entry
  ("/help links to the full online user guide").

### Keeping it current
- `CLAUDE.md` *Docs* section: "`/help`, `README.md` and `guide/` are hand-maintained: a new
  user-facing command updates all three"; add `guide/` and `mkdocs.yml` to the **no bump** list.
- New check in `tests/ProjectSYNCS.Checks` (`GuideChecks.cs`): reflect over the bot assembly's
  `[SlashCommand]`/`[Group]` attributes, build each full command path, and fail if one isn't
  mentioned in some `guide/**/*.md` — with an explicit exclusion list for owner-only (`/debug …`)
  and aliases (`/pl`). Wire it into `Program.cs` like the other sections.
- README: one line near the top linking to the guide.

### Owner's manual step
Enable Pages once: repo **Settings → Pages → Source: GitHub Actions** (a repo setting — yours to
change). Until then the deploy job fails; the build step still validates.

## Steps
1. Write this design to `docs/superpowers/specs/2026-11-24-user-guide-design.md`.
2. `mkdocs.yml`, `guide/` skeleton, workflow.
3. Write pages (one at a time, each checked against its subsystem notes and module code).
4. `HelpModule` link button + version bump + CHANGELOG.
5. `GuideChecks` in the Checks project; CLAUDE.md and README updates.

## Verification
- `dotnet build` in `ProjectSYNCS` (warnings as errors).
- `dotnet run --project tests/ProjectSYNCS.Checks` — new guide check passes (and fails if a
  command line is removed from a page).
- Local preview if Python works here: `py -m pip install mkdocs-material` then
  `py -m mkdocs build --strict` / `mkdocs serve` and open it in the browser pane (desktop + mobile
  viewport). Otherwise the CI build after the owner pushes, then open the Pages URL.
- `/help` in the dev guild: the « 📖 Guide complet » button opens the site.
- Nothing committed (owner commits by hand).
