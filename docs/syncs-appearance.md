# SYNCS's appearance

What SYNCS looks like, for any picture of her: the bot avatar, alternate poses, moods, emotes,
stickers, banners. How she *talks* is `docs/syncs-voice.md`; the face should read as the same
character — a bratty, petty, kawaii little bot who runs the server from a Raspberry Pi in Papa's
attic.

References: [`docs/assets/syncs-avatar-v1.jpg`](assets/syncs-avatar-v1.jpg) (the avatar) and
[`docs/assets/syncs-banner-v1.jpg`](assets/syncs-banner-v1.jpg) (the profile banner), both generated
with Gemini (prompts below). Give the matching one to the generator alongside the prompt whenever the
tool accepts a reference image; text alone drifts.

She is the Pi; the mushroom girl is **how she and Rodhengard picture her**. Her lore (name,
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

The LED is the cheapest mood signal at emote size, where the face is a few pixels: green normal,
red angry, orange hurt, off broken.

### Formats

- **Avatar** — head-and-shoulders, centred, plain background, safe inside a circle crop (Discord
  crops it round).
- **Custom emotes** — square, transparent background, **face and cap only**, exaggerated
  expression; they render around 22–48 px, so the silhouette and the LED carry the mood. Once
  uploaded, an emote goes through `Helpers/Emotes` like any other (`CLAUDE.md`).
- **Stickers** — square, transparent, half or full body is fine.
- **Banner / scenes** — her attic at night (see *The banner* below for the canon scene).

## The banner

The profile banner shows **where she lives, not her**: the Raspberry Pi *is* her body, so she isn't
drawn in it. Avatar = who she is, banner = her home.

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
| Avatar v1 | `assets/syncs-avatar-v1.jpg` | First avatar, Gemini. Current bot profile picture |
| Banner v1 | `assets/syncs-banner-v1.jpg` | First banner, Gemini. Current profile banner |

When a new avatar or banner replaces one of these, add it here and keep the old file: the reference that fixed
the design is worth keeping.
