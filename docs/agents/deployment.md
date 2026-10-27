# Deployment: the Home Assistant add-on, the image and `/data`

The version-bump and changelog rules apply to every change and stay in the root `CLAUDE.md`. Read
this file before touching `config.yaml` (beyond the version), `run.sh`, the `Dockerfile`, the
csproj's `<Version>` logic, `.github/workflows/addon-image.yml`, or anything that writes a file to
disk.

## The add-on

The bot ships as a Home Assistant add-on: the `Dockerfile` publishes a self-contained
`linux-arm64` build, and `run.sh` maps add-on options to `Discord__Token`,
`Discord__RegisterCommandsGlobally` and `Database__Path=/data/ProjectSYNCS.db`.

**Only `/data` is persisted**, so the SQLite file must stay under it — and so do the state files
written next to it:

| File | Written by |
|---|---|
| `health-state.json` | `PiHealthService` |
| `ambient-state.json` | `AmbientService` |
| `morning-state.json` | `MorningGreetingService` |
| `avatar-state.json` | `SeasonalAvatarService` |
| `report-state.json` | `MonthlyDigestService` |
| `minecraft-state.json` | `MinecraftBodyService` |
| `silence-state.json` | `SilenceService` |
| `freegames-state.json` | `SteamFreeGamesService` |

A new state file goes next to the database too, and gets a row here.

The database runs in **WAL mode** (`PRAGMA journal_mode=WAL` after `MigrateAsync()` in `Program.cs`), so
`ProjectSYNCS.db-wal` and `ProjectSYNCS.db-shm` sit beside it. They are part of the database: while
the bot runs, the `.db` file alone can miss the latest writes. To copy the database out, stop the
bot first or take all three files. WAL needs a local filesystem; never move `/data` to a network share.

## The image is built by CI, never on the Pi

Building on the Pi left about 1 GB of .NET layers in Docker's build cache per version, until the
disk filled. Now `.github/workflows/addon-image.yml` builds it on a native arm64 runner for every
push to `main` touching `ProjectSYNCS/`, and pushes
`ghcr.io/rmelisen/aarch64-addon-projectsyncs:<version>`; `image:` in `config.yaml` makes the
Supervisor pull that tag. Consequences:

- **Never remove `image:`** from `config.yaml`: the Supervisor would go back to building on the Pi.
- **A published tag is never overwritten.** The workflow skips a version that already exists, so an
  unbumped push ships nothing.
- **Auto-update is off on the Pi.** The update can show up before CI has pushed the image; the owner
  waits for the workflow to go green, then clicks Update. A failed run means no update, not a broken
  bot.
- **The Dockerfile restores from the csproj alone** before copying the source, so the NuGet layer
  stays cached. That is why `<Version>` reads `config.yaml` only when it exists — keep that
  condition, and never `COPY` `config.yaml` before the restore.
- **The GHCR package must stay public** (the Supervisor pulls anonymously), and so must the repo:
  arm64 runners are free only for public repositories.
- **Never change the add-on repository URL in Home Assistant** (for example to `…#branch`): the
  add-on's identity is a hash of that URL, so it would become a new add-on with an empty `/data`.
