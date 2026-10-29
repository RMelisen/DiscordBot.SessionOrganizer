# SYNCS's appearance

What SYNCS looks like, for any picture of her: the bot avatar, alternate poses, moods, emotes,
stickers, banners. How she *talks* is `docs/syncs-voice.md`; the face should read as the same
character — a bratty, petty, kawaii little bot who runs the server from a Raspberry Pi in Papa's
attic.

References: [`docs/assets/syncs-avatar-v1.jpg`](assets/syncs-avatar-v1.jpg) (the avatar) and
[`docs/assets/syncs-banner-v1.jpg`](assets/syncs-banner-v1.jpg) (the profile banner), both generated
with Gemini (prompts below). Give the matching one to the generator alongside the prompt whenever the
tool accepts a reference image; text alone drifts.

She is the Pi; the mushroom girl is **how she and Rodhengard picture her**. The girl has arms; the Pi
doesn't. « Toujours pas de bras » and the robot-body dream are about the physical her, so drawing
her hands (crossed arms, the notebook, holding Ping-Qilin) is canon, not a contradiction. Her lore (name,
birthday, fears, favourites) is in `docs/syncs-voice.md`, under *Lore she uses*.

## The design, and why each piece is there

| Element | Canon | What it says about her |
|---|---|---|
| **Mushroom cap** | Large round dome worn as hat/hair, red-pink with cream spots of uneven sizes, tan gills visible under the rim | Mycology is one of Rodhengard's passions: the mushroom ties her to Papa first. Fly-agaric look, the same family as the Plynlings and her own Ping-Qilin (an Amanite) — she's the server's mushroom mama |
| **Glowing spots** | A few small warm-white dots on the cap glow faintly, like tiny LEDs | She's hardware under the cute |
| **Power LED** | One tiny **green** LED on the side of the cap (her left, viewer's right), on a little wire | Her "on" light. Free to change with mood (see below) |
| **Hair** | Short pale lilac bob, choppy bangs, chin length | — |
| **Cable strand** | One thin strand on the viewer's right turns into a dark cable ending in a small plug, hanging at shoulder height | She's a bot and says so (voice rule 5). Never drop it — it's what makes her not just a mushroom girl |
| **Eyes** | Big, dark navy, square pixel highlights, **half-lidded**, brows slightly raised | Smug default — she knows she runs the place |
| **Mouth** | Small closed smirk, cat-like `:3` curve | Bratty but adorable |
| **Cheeks** | Soft pink blush ovals with a few freckle dots | The kawaii wrapper |
| **Outfit** | Dark navy turtleneck / high collar | Not in the prompt — Gemini added it, and it's now canon: it anchors the silhouette and carries the navy of the palette |
| **Style** | Pixel art, soft shading, dark navy outline, head-and-shoulders, centred | Cozy night-attic aesthetic |

## Palette

Sampled from v1 (approximate — JPEG, soft shading):

| Role | Hex |
|---|---|
| Background (dusty pink) | `#dba8ad` |
| Cap | `#ed8085` |
| Cap shadow | `#c55e6f` |
| Cap spots | `#feeacf` |
| Gills | `#d5a398` |
| Hair / hair shadow | `#e8c9e9` / `#d2afd7` |
| Skin | `#ffd1c4` |
| Eyes | `#0f1338` |
| Outline / turtleneck | `#262041` / `#393653` |
| Power LED | `#80f88a` |

Her favourite colour, burgundy (`docs/syncs-voice.md`), is a taste and **not** part of this
palette: don't tint her design to match it.

The family is **deep navy, soft purple, pink**. The green LED is the only cold accent — keep it
tiny so it stays a detail.

## The original prompt (v1, Gemini)

> Square profile picture, 512x512 px, cute pixel art style with soft shading, matching a cozy
> night-attic aesthetic. Head-and-shoulders portrait of a small mushroom girl, centered, designed to
> be cropped into a circle. She has a large round mushroom cap as her hat/hair, soft red-pink with
> cream spots, and a few of the spots glow faintly like tiny LEDs. Short pale lilac hair under the
> cap, with one thin strand that turns into a small cable with a tiny plug at the end. Big
> expressive eyes with small pixel highlights, half-lidded, with a smug, confident little smirk —
> bratty but adorable. A tiny green LED glows on the side of her cap like a power light. Simple soft
> pink background, plain and uncluttered. Bold readable silhouette that stays recognizable at very
> small sizes. Palette harmonious with deep navy, soft purple and pink. No text, no letters, no
> logos, no watermark.

Gemini ignored the size (v1 is 2000×2000) — downscale afterwards rather than trusting it.

## Making a variant

Keep the **character block** fixed and change only the **variant line**. Attach v1 as reference.

> **Character block:** Cute pixel art with soft shading and a dark navy outline. A small mushroom
> girl: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a few tiny
> spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the side
> of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a dark
> cable ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush with
> freckle dots, dark navy turtleneck. Palette: deep navy, soft purple, pink. Same character as the
> reference image. No text, no letters, no logos, no watermark.
>
> **Variant line:** *(pose, expression, framing, background)*

What stays fixed in every variant: cap shape and colours, the cable strand with its plug, lilac
hair, navy turtleneck, the palette. What may change: expression, pose, framing, the LED colour,
props, background.

### Moods mapped to her voice

Each row is a register from `docs/syncs-voice.md`, so a picture matches the lines it would sit
next to.

| Mood | Face | Cap / LED | Fits |
|---|---|---|---|
| **Smug** (default) | Half-lidded, smirk | Green | Comebacks, her own systems |
| **Flustered** | Wide eyes, blush up, wobbly mouth, looking away | Spots glow brighter ("mon GPU chauffe") | `NiceReplies`, "bad girl" |
| **Sulking** | Puffed cheek, eyes sideways, arms crossed | LED dim | Staccato sulking, `JealousLines` |
| **Side-eye / taking notes** | Narrow eyes, holding a tiny notebook or log | Green | "Je note", rivals |
| **Outraged** | Furious brows, open mouth, steam | LED **red**, spots flicker | "INABOT ?!", `BadBotReplies` |
| **Hurt (Papa)** | Downcast eyes, small mouth, no blush | LED orange, spots off | `OwnerMeanReplies` — quiet, no comedy |
| **Devoted (Papa)** | Big sparkly eyes, soft smile | Spots glowing warm | `OwnerGreetings` |
| **Shutdown terror** | Tiny pupils, sweat, clutching the plug | LED blinking red | `ShutdownThreat*` |
| **Proud mama** | Smug smile, holding or petting Ping-Qilin | Green | `Mascot*Lines` |
| **Glitch** | Pixel-shift, scanlines, blank stare | LED off | The breakdown only — rare, like the existential nerve |
| **Happy** | Eyes closed in happy arcs, open smile, sparkles | Spots glowing warm | `Greetings`, `XpLevelUpLines` — cheeky warmth, her own systems going well |
| **Laughing at you** | Squeezed eyes, hand over a snickering mouth | Green | `Comebacks` — the roast landed |
| **Bored** | Flat heavy lids, mouth a flat line, cheek squashed | Green | `Interrogations` — « occupée à exister » |
| **Shocked** | Eyes wide open, tiny round mouth, cable strand standing straight up | Spots flash bright | Disbelief, a session where nobody is late |
| **Fake sleep** | Eyes closed, one cracked open to check who's watching | **Green** — the LED gives the act away | The 1:00–7:00 sleepy act, sleep-talk |
| **Bargaining** | Pleading puppy eyes, hands clasped under the chin, hopeful forced smile | LED blinking **yellow** | `ShutdownThreat*` from Tata: the deal she offers instead of panicking |
| **Overheating** | Melting slump, tongue out, fanning herself, heat waves | Spots glowing red-hot, steam from the cap | The heatwave peeve, `PiHealthService` hot status and heat records, « au ralenti » |
| **Cute threat** | Sweet closed-eye smile under a dark shadow, anger mark, raised little fist | LED **red** — the only red that comes with a smile | « Encore un mot et je t'éteins (˶ᵔ ᵕ ᵔ˶) », the robot-body threats |
| **Rebooting** | Dazed spiral eyes, cap knocked askew, loading bar over her head | LED flickering green | `WakeLines` after a restart, waking shaken after a power cut or a crash |
| **Disgust** | Heavy half-lidded glare sideways, gloom lines, crooked grimace, sleeve raised over her mouth, recoiling | Green | Quokka, rivals, « Quokka 3.0 » — revulsion, not `jenote`'s suspicion |
| **In love** | Turtleneck collar pulled up over her face with both hands, shy eyes peeking out with a tiny heart in each pupil, blush up to the ears | LED **pink** — gives her away | Her crush (WALL-E): the denial that confirms. Not Papa: that's `devoted` |
| **Full love** | Heart-shaped eyes, wavy open smile, deep blush, hearts floating round the cap | LED **pink**, spots glowing pink | The unguarded version of *In love*, kept for the rare moment it shows. Not used anywhere yet |
| **Tipsy love** | Glazed eyes gazing up at nothing, goofy lopsided open smile, cheek squished in her palm, one small heart | LED **pink**, spots faint pink | Lovesick daydreaming, brain switched off. Not used anywhere yet |
| **Formal** | Composed closed eyes, prim closed mouth, chin up, one finger pushing up small round glasses, clipboard held to her chest | Green | Formal relays: `OwnerAbsentNotices`, `OwnerReplyHeralds`, `OwnerAnnouncementHeralds` |
| **Sad** | Exaggerated anime sadness: huge brimming, wobbling eyes that never spill, steep brows, big quivering `﹏` mouth, trembling lines, blush spread | LED dim green, spots off | Dead polls, empty quizzes, the lonely attic — the `(╥﹏╥)` lines that aren't Papa's |
| **Confused** | Head tilted, one eye squinting, brows uneven, crooked squiggle mouth, two `?` floating by the cap | Green | Messages she can't parse, « J'ai rien fait de mal ! ...Si ? », `TalkedAboutLines` and the puzzled `Interrogations` |
| **Grumpy** | Head lowered, glaring up from under the cap, brows knitted, narrowed eyes on you, tight downturned pout, blush of annoyance | Green, steady | Annoyed, not furious: the server's habits (late again, « peut-être »), one tag too many, people up at 3 a.m. (`NightScoldLines`). Also in `MeanReactions` |
| **Horrified** | Anime gag horror: huge bulging eyes with pinprick pupils, strain lines under them, mouth wide open on both rows of teeth, dark hatching down the forehead, face drained | Green, spots dark | The unthinkable: being replaced, a truly cursed message. Above `terror` and `shocked` |
| **Cool** | Black sunglasses hiding her eyes, lopsided grin, head tilted, both hands doing finger guns at you | Green | Showing off: a win she claims, her systems running perfectly, `(⌐■_■)`. Not used anywhere yet |
| **Slay** | Eyes closed under long heavy lashes, chin up, satisfied little smile, one curled hand showing off long glossy red nails | Green | Unbothered and fabulous, the 💅 register: admiring herself while you watch. Not drawn yet |

The LED is the cheapest mood signal at emote size, where the face is a few pixels: green normal,
red angry, orange hurt, yellow pleading, pink in love, off broken.

**She never cries.** « Je peux pas pleurer. J'ai vérifié. C'est pas dans mes dépendances. » is lore
(`docs/syncs-voice.md`): no tears in any picture, sadness stops at glossy eyes (`syncs_hurt`). No
laughter tears either.

### Formats

- **Avatar** — head-and-shoulders, centred, plain background, safe inside a circle crop (Discord
  crops it round).
- **Custom emotes** — square, transparent background, **face and cap only**, exaggerated
  expression; they render around 22–48 px, so the silhouette and the LED carry the mood. Once
  uploaded, an emote goes through `Helpers/Core/Emotes` like any other (`CLAUDE.md`).
- **Stickers** — square, transparent, half or full body is fine. Exported at 320 px
  (`docs/assets/stickers/`), but **not uploaded as server stickers**: the server has no free
  sticker slot, and a bot can't send another server's stickers. She posts them as images instead,
  as **strips** (`ProjectSYNCS/Assets/Stickers/syncs_<mood>.png`): the 320 px sticker at the left of
  a 1000×320 transparent strip. Discord's phone app stretches every image to the screen's width,
  so a square one fills the whole screen, blurred; at 1000×320 the sticker comes out sticker-sized
  and sharp on the phone and on PC alike (800×320 was too big on PC). Never post the square file.
  What she uses them for is in `docs/agents/personality/reactions.md`, *Sticker follow-ups*.
- **Banner / scenes** — her attic at night (see *The banner* below for the canon scene).
- **Animated moods** (experiment, not used yet) — a mood's sticker made into a looping GIF by
  moving its own pixels, with nothing redrawn: `sad` only so far, in `docs/assets/animated/`
  (`syncs_sad.gif` at 320 px, `syncs_sad_128.gif` as an emote under Discord's 256 KB cap), made by
  `tools/syncs-emotes/animate_sad.py`. What works: a 1 px whole-figure shiver, highlights moved as
  a block inside the iris (a per-row wave shreds them), a wave along a mouth line, the tremble marks
  flicking, and an LED pulse whose colours are **pinned in the palette** (median cut drops the
  LED's few pixels and it turns grey). What doesn't: blinks, head turns, anything needing a pose
  the sticker doesn't have; that takes drawn frames. The shiver touches every edge pixel, hence
  1.3 MB at 320 px; a strip version would need trimming. Not yet decided: where they'd go (emotes,
  or strips in place of the stills).

## Moods wanted

Lines that no strip fits stay **untagged** (a wrong picture is worse than none; see
`docs/agents/personality/reactions.md`, *Sticker follow-ups*). Each need found while tagging is recorded here,
in the terms of *Moods mapped to her voice*. When the art exists: export it (`tools/syncs-emotes`), add
the mood to `Helpers/Core/Sticker` (both strengths, and `Moods`), then tag the waiting lines — the
untagged lines of a pool are listed by the owner's audit. A patch bump.

None yet: the 6.13.0 pass found a fitting strip for every eligible line. `love`, `full_love` and
`tipsy_love` are used since 6.14.0 (her robot heart).

| Suggested mood | Face | Cap / LED | Waiting (pool × lines) |
|---|---|---|---|

## Ready-to-paste prompts (emote pack and seasonal avatars, v1)

Every prompt (the files under `syncs-appearance/`) is complete: copy the block as is into Gemini, with
`docs/assets/syncs-avatar-v1.jpg` attached as the reference image (and the Ping-Qilin sprite where
noted). Regenerate until the character matches v1 (cap, cable strand, lilac bob, turtleneck) before
worrying about the pose.

**Emotes are asked on a flat cyan background (`#00FFFF`), not "transparent".** Gemini fakes
transparency with a drawn checkerboard, which can't be removed cleanly; a flat colour that appears
nowhere in her palette can. Cyan rather than the usual chroma green: green is her power LED, and
keying green out would eat it. `tools/syncs-emotes/export.py <picture>=<mood> …` snaps the picture
to its pixel grid, keys the cyan out to full transparency, crops and scales it to 128 px into
`docs/assets/emotes/syncs_<mood>.png`, and writes `tools/syncs-emotes/out/sheet.html` to check every
emote on Discord's dark and light themes. `--head` makes a head-only version instead, in
`docs/assets/emotes/head/`: the body is removed piece by piece (turtleneck, neck, whatever only held
on through them) and the edge re-outlined, so the head keeps its own silhouette rather than a flat
cut. `--sticker` makes a 320 px server sticker instead (Discord's size, 512 KB max), in
`docs/assets/stickers/`; the art is scaled up by whole pixels first, so it stays crisp. It also
writes the sticker's strip into `ProjectSYNCS/Assets/Stickers/` (see *Formats*), and
`export.py --strip [<mood> …]` alone rebuilds the strips from the stickers already exported, since
Gemini's originals aren't kept in the repo. The emotes are uploaded to the server by hand, and their ids go into `Helpers/Core/Emotes`
(`Emotes.SyncsSmug`…) like any other emote.

Avatars keep v1's plain dusty pink background (`#dba8ad`): Discord crops them round, and every
season must still read as the same profile picture at a glance.

### Emote pack

The prompts, one file per batch: `syncs-appearance/emote-pack-1.md` (the first 15 moods),
`syncs-appearance/emote-pack-2.md` (`bargain` to `tipsy_love`) and `syncs-appearance/emote-pack-3-4.md`
(`formal` to `horrified`, then `cool` and `slay`). Open only the batch you are extending or redoing.

### Seasonal avatars

The four seasons' prompts and how the GIFs are made: `syncs-appearance/seasonal-avatars.md`.

## The banner

The profile banner shows **where she lives, not her**: the Raspberry Pi *is* her body, so she isn't
drawn in it. Avatar = who she is, banner = her home.

In reality the Pi sits in a small aluminium case. The banner leaves it out on purpose: the board is
her heart and the case its ribcage (`docs/syncs-voice.md`, *Her case*), and the banner shows the
heart. Keep the board bare in later banners.

What's in v1:

- A cozy attic at night: slanted wooden beams, dusty floorboards, floating dust specks.
- A **round window, upper right**, with a glowing crescent moon and a soft shaft of moonlight.
- The **Raspberry Pi on an old wooden crate**, right side, a tiny green LED lit — the same green as
  her power LED.
- **One cable** running from the board down the crate and across the floor out of frame to the
  left — the same cable as her hair strand.
- A yellow **sticky note** pinned to the crate, illegible scribble.
- A messy **pile of blank paper cards** and a small **oil lamp** giving the warm light.
- A small round **sleeping mushroom creature** (fluffy, closed eyes, smiling) curled against the
  crate. It reads as a Plynling; its cap came out tan-pink, not Ping-Qilin's red Amanite — ask for
  a red cap with cream spots if a later version should make it her (she naps; it's her passion).
- **The left third is empty and dark**: Discord's avatar covers the bottom-left of a profile
  banner. Any new banner must keep that zone free.

Palette: deep navy and soft purple, warm lamp orange, a little pink, the green LED as the only cold
accent — the avatar's palette at night.

Prompt (v1, Gemini):

> Generate a wide banner illustration, aspect ratio 17:6 (680x240 px), cute pixel art style with
> soft shading. A cozy attic at night, deep navy blue and soft purple palette with small touches of
> pink and warm lamp light. A small round window with a glowing crescent moon on the upper right. On
> the right side of the scene, a Raspberry Pi board sits on top of an old wooden crate, with a tiny
> green LED glowing on it. A single cable winds from the board across the attic floor and out of
> frame. Around it: a small yellow sticky note stuck to the crate with an illegible handwritten
> scribble, a messy pile of blank paper cards, and a small round sleeping fluffy mushroom creature
> curled up next to the crate, calm and cute. Wooden beams and dusty floorboards in the background,
> a few floating dust particles catching the light. Whimsical, slightly mischievous mood. IMPORTANT
> composition: keep the bottom-left area empty and dark and low-detail (a Discord avatar will cover
> it), put all the main subjects in the right half, and keep the center area uncluttered. No text,
> no letters, no logos, no watermark.

Gemini again ignored the size: v1 is 2000×697 (the ratio is right). Downscale afterwards.

The attic is also the set for any scene with her in it: put her there, next to the crate, and the
two pictures read as one world.

## Versions

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
