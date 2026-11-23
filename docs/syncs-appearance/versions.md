# Versions

Part of `docs/syncs-appearance.md`. Every picture of her that ships gets a row here.

| Version | File | Notes |
|---|---|---|
| Avatar v1 | `assets/syncs-avatar-v1.jpg` | First avatar, Gemini. Profile picture until the seasonal ones (6.8.0); still the reference for every new picture |
| Banner v1 | `assets/syncs-banner-v1.jpg` | First banner, Gemini. Current profile banner |
| Emote pack v1 | `assets/emotes/syncs_<mood>.png`, head-only in `assets/emotes/head/` | Gemini, from the prompts in `syncs-appearance/`, all 15 moods; made transparent with `tools/syncs-emotes`. Server emotes, `Emotes.Syncs*` (6.8.1); `sleepy`, `shocked`, `mama` and `jenote` taken off the server in 6.12.0 (assets kept) |
| Emote pack, second batch | same folders, plus 320 px stickers in `assets/stickers/` | Gemini, from the prompts in `syncs-appearance/`, the 8 moods `bargain` to `tipsy_love`; same pipeline. Not uploaded yet |
| Emote pack, third batch | same folders | Gemini, from the prompts in `syncs-appearance/`, the 5 moods `formal` to `horrified`; same pipeline. Gemini drew these without a pixel grid, which is what the head-only neck steps for JPEG blends exist for. `horrified`'s head-only is exported with `--no-neck` (her jaw sits on the collar, and the neck steps would empty her mouth); `formal`'s had the stray sleeve outline under the chin cleared by hand (everything below the hand). Server emotes and sticker tags (6.17.1) |
| Emote pack, fourth batch | same folders | Gemini, from the prompts in `syncs-appearance/`, `cool` and `slay`; same pipeline, no pixel grid detected (`slay`'s background snapped to `#00FFFF` first, see the batch). Emote, sticker and strip only (no head-only). Server emotes and sticker tags (6.17.7) |
| Sticker strips v1 | `../ProjectSYNCS/Assets/Stickers/syncs_<mood>.png` | Every sticker in `assets/stickers/` (30 moods) as a 1000×320 strip, `export.py --strip`. Shipped with the bot; posted after tagged lines since 6.13.0 (all but `glitch`, posted by hand at the breakdown's cut and the glitched wake since 6.17.4; the third batch since 6.17.1) |
| Seasonal avatars v1 | `../ProjectSYNCS/Assets/Avatars/syncs-{spring,summer,autumn,winter}.png` | Gemini, from the prompts in `syncs-appearance/`. Rotate with the seasons (6.8.0); v1 is the reference they were drawn from. Still the bot's fallback when a GIF is missing; a copy of the four stills is kept in `assets/avatars/classic/` |
| Animated seasonal avatars | `../ProjectSYNCS/Assets/Avatars/syncs-<season>.gif`, full-size in `assets/avatars/` | `tools/syncs-avatar/animate.py`, from the stills. What the bot uploads since 6.17.3 |
| Animated sad (experiment) | `assets/animated/syncs_sad.gif`, `syncs_sad_128.gif` | `tools/syncs-emotes/animate_sad.py`, from the sad sticker. Not uploaded, not used (see *Formats*) |

When a new avatar or banner replaces one of these, add it here and keep the old file: the reference that fixed
the design is worth keeping.
