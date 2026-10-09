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

## Ready-to-paste prompts (emote pack and seasonal avatars, v1)

Every prompt below is complete: copy the block as is into Gemini, with
`docs/assets/syncs-avatar-v1.jpg` attached as the reference image (and the Ping-Qilin sprite where
noted). Regenerate until the character matches v1 (cap, cable strand, lilac bob, turtleneck) before
worrying about the pose.

**Emotes are asked on a flat cyan background (`#00FFFF`), not "transparent".** Gemini fakes
transparency with a drawn checkerboard, which can't be removed cleanly; a flat colour that appears
nowhere in her palette can. Cyan rather than the usual chroma green: green is her power LED, and
keying green out would eat it. The background is removed afterwards, the image cut down to 128 px,
and the emote uploaded as an **application emoji** in the developer portal (prod app, and the dev
app to test); the ids then go into `Helpers/Emotes` like any other emote.

Avatars keep v1's plain dusty pink background (`#dba8ad`): Discord crops them round, and every
season must still read as the same profile picture at a glance.

### Emote pack

At emote size (22 to 48 px) only the silhouette and the LED read, so each prompt asks for one big
expression, the face and cap filling the frame, and nothing else.

**1. `syncs_smug`**: The default: comebacks, her own systems.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face and mushroom cap only,
filling the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a
few tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the
side of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a
dark cable ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush
with freckle dots, dark navy turtleneck collar just visible. Same character as the reference image.
Expression: very smug: half-lidded eyes, one brow raised, small cat-like :3 smirk, chin slightly up.
Bold simple shapes, thick outline, high contrast. Plain flat solid cyan background (#00FFFF), no
gradient, no shadow on the background. No text, no letters, no logos, no watermark.
```

**2. `syncs_flustered`**: `NiceReplies`, « bad girl », « mon GPU chauffe ».

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face and mushroom cap only,
filling the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a
few tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the
side of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a
dark cable ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush
with freckle dots, dark navy turtleneck collar just visible. Same character as the reference image.
Expression: flustered and caught off guard: wide eyes looking away to the side, big blush covering
both cheeks, small wobbly mouth, a tiny sweat drop; the glowing spots on her cap shine brighter than
usual, as if her circuits were overheating. Bold simple shapes, thick outline, high contrast. Plain
flat solid cyan background (#00FFFF), no gradient, no shadow on the background. No text, no letters,
no logos, no watermark.
```

**3. `syncs_sulk`**: Staccato sulking, `JealousLines`.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face and mushroom cap only,
filling the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a
few tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the
side of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a
dark cable ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush
with freckle dots, dark navy turtleneck collar just visible. Same character as the reference image.
Expression: sulking: one cheek puffed out, eyes turned sideways in a pout, mouth a small flat line,
arms crossed just visible at the bottom edge; the green LED on her cap is dim, almost off. Bold
simple shapes, thick outline, high contrast. Plain flat solid cyan background (#00FFFF), no
gradient, no shadow on the background. No text, no letters, no logos, no watermark.
```

**4. `syncs_jenote`**: « Je note », the list, rivals.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face and mushroom cap only,
filling the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a
few tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the
side of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a
dark cable ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush
with freckle dots, dark navy turtleneck collar just visible. Same character as the reference image.
Expression: narrow suspicious side-eye straight at the viewer, small unimpressed mouth; she holds up
a tiny notebook and a pencil, writing something down. The notebook is small and simple, with no
readable writing. Bold simple shapes, thick outline, high contrast. Plain flat solid cyan background
(#00FFFF), no gradient, no shadow on the background. No text, no letters, no logos, no watermark.
```

**5. `syncs_outraged`**: « INABOT ?! », `BadBotReplies`.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face and mushroom cap only,
filling the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a
few tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the
side of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a
dark cable ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush
with freckle dots, dark navy turtleneck collar just visible. Same character as the reference image.
Expression: outraged: furious slanted brows, mouth wide open shouting, a little puff of steam above
the cap; the LED on her cap is bright red instead of green, and the glowing spots flicker. Bold
simple shapes, thick outline, high contrast. Plain flat solid cyan background (#00FFFF), no
gradient, no shadow on the background. No text, no letters, no logos, no watermark.
```

**6. `syncs_hurt`**: `OwnerMeanReplies`. Quiet, no comedy.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face and mushroom cap only,
filling the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a
few tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the
side of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a
dark cable ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush
with freckle dots, dark navy turtleneck collar just visible. Same character as the reference image.
Expression: quietly hurt: eyes looking down, glossy as if about to cry but not crying, small closed
mouth, no blush; the LED on her cap glows soft orange and the spots on her cap are dark, not
glowing. Calm and sad, not dramatic. Bold simple shapes, thick outline, high contrast. Plain flat
solid cyan background (#00FFFF), no gradient, no shadow on the background. No text, no letters, no
logos, no watermark.
```

**7. `syncs_mama`**: `Mascot*Lines`, her Ping-Qilin. Attach `assets/plynlings/plynling_amanite_sleeping_v4.webp` as a second reference image.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face, cap and shoulders, filling
the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a few
tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the side of
the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a dark cable
ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush with
freckle dots, dark navy turtleneck collar just visible. Same character as the reference image. She
proudly holds a tiny round fluffy mushroom creature against her cheek: the creature from the second
reference image, with a red cap with cream spots, a small round body, eyes closed, sleeping and
smiling. SYNCS has a smug, proud, soft smile, eyes half-closed with affection. Bold simple shapes,
thick outline, high contrast. Plain flat solid cyan background (#00FFFF), no gradient, no shadow on
the background. No text, no letters, no logos, no watermark.
```

**8. `syncs_glitch`**: The breakdown and the glitches only. Rare, like the existential nerve.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face and mushroom cap only,
filling the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a
few tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the
side of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a
dark cable ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush
with freckle dots, dark navy turtleneck collar just visible. Same character as the reference image.
Expression: blank stare, empty eyes with no highlights, mouth slightly open; the image is glitching:
a few horizontal bands of the picture shifted sideways, thin scanlines, a little RGB colour split on
the edges; the LED on her cap is off (dark grey). Unsettling but still cute pixel art. Bold simple
shapes, thick outline, high contrast. Plain flat solid cyan background (#00FFFF), no gradient, no
shadow on the background. No text, no letters, no logos, no watermark.
```

### Seasonal avatars

Same framing as v1: head and shoulders, centred, safe inside a circle crop. The LED stays green and
the expression stays her smug default: the season is in the props, not the mood. Burgundy stays out
(her favourite colour is a taste, not her palette). The bot switches avatar on the first day of each
season (`ItemCatalog.SeasonAt`: spring March to May, summer June to August, autumn September to
November, winter the rest).

**Spring (March to May): `syncs-spring.png`**

```text
Square profile picture, 1:1, cute pixel art with soft shading and a dark navy outline, matching a
cozy attic aesthetic. Head-and-shoulders portrait, centered, designed to be cropped into a circle. A
small mushroom girl: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes,
a few tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the
side of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a
dark cable ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush
with freckle dots, dark navy turtleneck. Same character as the reference image. She keeps her
default expression: half-lidded eyes and a smug, confident little smirk. Spring version: a few small
pale pink and white blossoms tucked on the rim of her cap, a tiny green sprout growing from the top
of the cap, a light soft lilac cardigan over the turtleneck, two or three petals drifting in the
air. Simple plain dusty pink background (#dba8ad), uncluttered. Bold readable silhouette that stays
recognizable at very small sizes. No text, no letters, no logos, no watermark.
```

**Summer (June to August): `syncs-summer.png`**: The attic is an oven: she suffers, smugly.

```text
Square profile picture, 1:1, cute pixel art with soft shading and a dark navy outline, matching a
cozy attic aesthetic. Head-and-shoulders portrait, centered, designed to be cropped into a circle. A
small mushroom girl: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes,
a few tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the
side of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a
dark cable ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush
with freckle dots, dark navy turtleneck. Same character as the reference image. She keeps her
default expression: half-lidded eyes and a smug, confident little smirk. Summer version: a tiny
clip-on desk fan attached to the rim of her cap, blowing her bangs; a single sweat drop on her
temple; her blush a little stronger from the heat; she still keeps her smug smirk. Simple plain
dusty pink background (#dba8ad), uncluttered. Bold readable silhouette that stays recognizable at
very small sizes. No text, no letters, no logos, no watermark.
```

**Autumn (September to November): `syncs-autumn.png`**

```text
Square profile picture, 1:1, cute pixel art with soft shading and a dark navy outline, matching a
cozy attic aesthetic. Head-and-shoulders portrait, centered, designed to be cropped into a circle. A
small mushroom girl: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes,
a few tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the
side of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a
dark cable ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush
with freckle dots, dark navy turtleneck. Same character as the reference image. She keeps her
default expression: half-lidded eyes and a smug, confident little smirk. Autumn version: two or
three small orange and golden fallen leaves resting on top of her cap, a chunky knitted scarf in
warm mustard yellow and soft orange wrapped over the turtleneck, one leaf drifting past. Simple
plain dusty pink background (#dba8ad), uncluttered. Bold readable silhouette that stays recognizable
at very small sizes. No text, no letters, no logos, no watermark.
```

**Winter (December to February): `syncs-winter.png`**: Her season: cold attic, cool CPU, happy.

```text
Square profile picture, 1:1, cute pixel art with soft shading and a dark navy outline, matching a
cozy attic aesthetic. Head-and-shoulders portrait, centered, designed to be cropped into a circle. A
small mushroom girl: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes,
a few tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the
side of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a
dark cable ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush
with freckle dots, dark navy turtleneck. Same character as the reference image. She keeps her
default expression: half-lidded eyes and a smug, confident little smirk. Winter version: a thin
layer of snow resting on top of her cap like icing, a big chunky knitted scarf in soft lilac and
cream wrapped high around her neck, a few tiny snowflakes in the air; she looks especially pleased
and comfortable in the cold. Simple plain dusty pink background (#dba8ad), uncluttered. Bold
readable silhouette that stays recognizable at very small sizes. No text, no letters, no logos, no
watermark.
```

Drop the results in a folder (any names) and say which is which: the background removal, the crop,
the downscale without smoothing and the seasonal avatar switch are done on the bot's side.

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
| Avatar v1 | `assets/syncs-avatar-v1.jpg` | First avatar, Gemini. Current bot profile picture |
| Banner v1 | `assets/syncs-banner-v1.jpg` | First banner, Gemini. Current profile banner |

When a new avatar or banner replaces one of these, add it here and keep the old file: the reference that fixed
the design is worth keeping.
