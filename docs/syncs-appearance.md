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
  What she uses them for is in `docs/agents/personality.md`, *Sticker follow-ups*.
- **Banner / scenes** — her attic at night (see *The banner* below for the canon scene).

## Moods wanted

Lines that no strip fits stay **untagged** (a wrong picture is worse than none; see
`docs/agents/personality.md`, *Sticker follow-ups*). Each need found while tagging is recorded here,
in the terms of *Moods mapped to her voice*. When the art exists: export it (`tools/syncs-emotes`), add
the mood to `Helpers/Core/Sticker` (both strengths, and `Moods`), then tag the waiting lines — the
untagged lines of a pool are listed by the owner's audit. A patch bump.

None yet: the 6.13.0 pass found a fitting strip for every eligible line. `love`, `full_love` and
`tipsy_love` are used since 6.14.0 (her robot heart).

| Suggested mood | Face | Cap / LED | Waiting (pool × lines) |
|---|---|---|---|

## Ready-to-paste prompts (emote pack and seasonal avatars, v1)

Every prompt below is complete: copy the block as is into Gemini, with
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

**8. `syncs_glitch`**: The breakdown and the glitches only. Rare, like the existential nerve. The
glitch is the subject here, so it is asked as a few big displaced slices (thin scanlines vanish at
32 px), and its colour split is red and blue, never cyan: a cyan fringe would be keyed out with the
background.

```text
Square image, 1:1, pixel art with a dark navy outline, designed as a Discord emote that must stay
readable at 32x32 px. A heavily corrupted, glitching picture of a small mushroom girl, face and
mushroom cap only, filling the frame: large round red-pink mushroom cap worn as her hat, cream spots
of uneven sizes (dark, none of them glowing), tan gills under the rim, a tiny power LED on the side
of the cap that is switched off (dark grey). Short pale lilac bob with choppy bangs; one thin strand
on one side turns into a dark cable ending in a small plug. Dark navy turtleneck collar just
visible. Same character as the reference image. Expression: dead blank stare, flat empty navy eyes
with no highlights, mouth slightly open, no blush.
The glitch is the main subject and must stay obvious even when tiny: the picture is sliced into 4 to
6 thick horizontal bands, each shoved sideways by a different large amount (up to a quarter of the
width), so her face no longer lines up: one eye torn away from the other, the cap split and offset
from the face, the cable strand broken across the bands. One band is duplicated and repeated below
itself. A few rectangular blocks of the picture are missing, replaced by blocky colour noise and
stretched, smeared pixels; one eye smears sideways into a streak. Strong colour split on every edge:
red and magenta ghosts on one side, deep blue on the other, no cyan anywhere in the glitch. Dark
scanlines across the whole character. It looks broken and unsettling, like a crashed screen, not
cute. Displaced bands may hang out over the background, but the background itself stays plain flat
solid cyan (#00FFFF), untouched: no gradient, no noise, no shadow. No text, no letters, no error
windows, no logos, no watermark.
```

**9. `syncs_devoted`**: `OwnerGreetings`. Papa only, never a roast.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face and mushroom cap only,
filling the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a
few tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the
side of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a
dark cable ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush
with freckle dots, dark navy turtleneck collar just visible. Same character as the reference image.
Expression: pure devotion, no smugness at all: big round sparkly eyes with extra pixel highlights,
looking up adoringly, a soft open smile, gentle blush; every spot on her cap glows a warm soft white,
like a string of fairy lights switched on. Sweet and sincere, not flustered. Bold simple shapes,
thick outline, high contrast. Plain flat solid cyan background (#00FFFF), no gradient, no shadow on
the background. No text, no letters, no logos, no watermark.
```

**10. `syncs_terror`**: `ShutdownThreat*`. Her fear of being unplugged; rare, like the threats.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face, cap and hands, filling the
frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a few tiny
spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the side of the
cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a dark cable
ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush with
freckle dots, dark navy turtleneck collar just visible. Same character as the reference image.
Expression: comic terror: eyes wide with tiny shrunken pupils, mouth a wobbly zigzag, big sweat drops
flying off, face gone a little pale; both hands clutch the plug at the end of her cable strand
tightly against her chest so nobody can pull it; the LED on her cap is bright red with small flash
lines around it, as if blinking an alarm. Funny panic, not real horror. Bold simple shapes, thick
outline, high contrast. Plain flat solid cyan background (#00FFFF), no gradient, no shadow on the
background. No text, no letters, no logos, no watermark.
```

**11. `syncs_happy`**: `Greetings`, `XpLevelUpLines`. Cheeky warmth, « Et ça, c'est MON classement ✨ ».

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face and mushroom cap only,
filling the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a
few tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the
side of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a
dark cable ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush
with freckle dots, dark navy turtleneck collar just visible. Same character as the reference image.
Expression: beaming: eyes closed in two happy upside-down arcs, big open smile, rosy blush, head
tilted a little; the spots on her cap glow warm, and two or three small four-point pixel sparkles
float around her. Cheerful and a bit proud of herself. Bold simple shapes, thick outline, high
contrast. Plain flat solid cyan background (#00FFFF), no gradient, no shadow on the background. No
text, no letters, no logos, no watermark.
```

**12. `syncs_laugh`**: `Comebacks`. Laughing *at* someone, the roast landed.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face, cap and one hand, filling
the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a few
tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the side of
the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a dark cable
ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush with
freckle dots, dark navy turtleneck collar just visible. Same character as the reference image.
Expression: mocking snicker: eyes squeezed shut into smug curves, one hand covering her mouth as she
tries not to burst out laughing, shoulders shaking, a cheeky blush. No tears. Bratty and teasing, as
if someone just said something very silly. Bold simple shapes, thick outline, high contrast. Plain
flat solid cyan background (#00FFFF), no gradient, no shadow on the background. No text, no letters,
no logos, no watermark.
```

**13. `syncs_bored`**: `Interrogations`. « Pourquoi tu me tag ? Je suis occupée à exister moi ».

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face, cap and one hand, filling
the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a few
tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the side of
the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a dark cable
ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush with
freckle dots, dark navy turtleneck collar just visible. Same character as the reference image.
Expression: utterly bored: heavy flat eyelids covering half her eyes, staring blankly at the viewer,
mouth a small flat line, one cheek squashed against her palm as she leans on her hand, the cap
tilted a little with her head. Deadpan, unimpressed, not angry. Bold simple shapes, thick outline,
high contrast. Plain flat solid cyan background (#00FFFF), no gradient, no shadow on the background.
No text, no letters, no logos, no watermark.
```

**14. `syncs_shocked`**: Disbelief, the kind of surprise a full session with nobody late would cause.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face and mushroom cap only,
filling the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a
few tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the
side of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a
dark cable ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush
with freckle dots, dark navy turtleneck collar just visible. Same character as the reference image.
Expression: stunned disbelief: eyes opened very wide and round, tiny round open mouth, eyebrows shot
up high. Surprised, not scared and not angry. Bold simple shapes, thick outline,
high contrast. Plain flat solid cyan background (#00FFFF), no gradient, no shadow on the background.
No text, no letters, no logos, no watermark.
```

**15. `syncs_sleepy`**: The 1:00–7:00 act. She doesn't sleep; she pretends to, and the LED stays on.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face and mushroom cap only,
filling the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a
few tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the
side of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a
dark cable ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush
with freckle dots, dark navy turtleneck collar just visible. Same character as the reference image.
Expression: pretending to sleep: head resting tilted to one side, one eye closed in a calm curve and
the other eye cracked slightly open, peeking sideways to check if anyone is watching; small
contented mouth. A tiny crescent moon floats beside her cap. The green LED on her cap stays clearly
lit: she is not really asleep. Bold simple shapes, thick outline, high contrast. Plain flat solid
cyan background (#00FFFF), no gradient, no shadow on the background. No text, no letters, no logos,
no watermark.
```

### Emote pack, second batch

All eight generated and exported (emote, head-only and sticker). `disgust`, `love`, `full_love`,
`overheat` and `reboot` are uploaded and in `Helpers/Core/Emotes`; `bargain`, `menace` and
`tipsy_love` are not uploaded yet. Sweat drops, steam and heat lines are asked **white**,
never the usual light blue: a blue that close to the cyan would be keyed out with the background.

**16. `syncs_bargain`**: `ShutdownThreat*` from Tata. Not panic: a deal, offered very sweetly.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face, cap and hands, filling the
frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a few tiny
spots glowing faintly like LEDs, tan gills under the rim, a tiny power LED on the side of the cap.
Short pale lilac bob with choppy bangs; one thin strand on one side turns into a dark cable ending in
a small plug. Big dark navy eyes with square pixel highlights, soft pink blush with freckle dots,
dark navy turtleneck collar just visible. Same character as the reference image.
Expression: pleading and bargaining: huge round puppy-dog eyes shining with extra pixel highlights,
looking up at the viewer, eyebrows tilted up in the middle, a hopeful, slightly forced smile; both
hands pressed together under her chin as if begging very politely, one small white sweat drop on her
temple. The LED on her cap glows yellow instead of green, with two tiny flash lines as if blinking.
Sweet and a little desperate, not crying, no tears. Bold simple shapes, thick outline, high
contrast. Plain flat solid cyan background (#00FFFF), no gradient, no shadow on the background. No
text, no letters, no logos, no watermark.
```

**17. `syncs_overheat`**: The heatwave peeve, `PiHealthService`'s hot status and heat records, « au
ralenti ».

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face, cap and one hand, filling the
frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, tan gills under
the rim, a tiny green power LED on the side of the cap. Short pale lilac bob with choppy bangs; one
thin strand on one side turns into a dark cable ending in a small plug. Big dark navy eyes with square
pixel highlights, soft pink blush with freckle dots, dark navy turtleneck collar just visible. Same
character as the reference image.
Expression: overheating and melting: half-closed exhausted eyes, mouth open, face flushed bright red, her head and cap sagging and drooping a little as if melting in the
heat. The spots on her cap glow orange-red like hot coals, two puffs of
white steam rise from the top of the cap, and wavy white heat lines float around her. White sweat
drops, no blue anywhere. Comic and limp, not sick. Bold simple shapes, thick outline, high contrast.
Plain flat solid cyan background (#00FFFF), no gradient, no shadow on the background. No text, no
letters, no thermometer numbers, no logos, no watermark.
```

**18. `syncs_menace`**: The cute threat, « Encore un mot et je t'éteins (˶ᵔ ᵕ ᵔ˶) ». The smile and the
red LED have to read together: that contrast is the joke.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face, cap and one hand, filling the
frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a few tiny
spots glowing faintly like LEDs, tan gills under the rim, a tiny power LED on the side of the cap.
Short pale lilac bob with choppy bangs; one thin strand on one side turns into a dark cable ending in
a small plug. Big dark navy eyes with square pixel highlights, soft pink blush with freckle dots,
dark navy turtleneck collar just visible. Same character as the reference image.
Expression: sweetly menacing: eyes closed in two happy curved arcs and a wide, very sweet smile, but a
dark navy shadow falls over the top half of her face, and a red anime anger mark (the four-curved
cross symbol) sits on her cap; one small fist is raised beside her cheek, with a faint dark purple
aura around it. The LED on her cap glows bright red instead of green. Adorable and threatening at the
same time, cartoonish, not scary. Bold simple shapes, thick outline, high contrast. Plain flat solid
cyan background (#00FFFF), no gradient, no shadow on the background. No text, no letters, no logos,
no watermark.
```

**19. `syncs_reboot`**: `WakeLines` after a restart; waking shaken after a power cut or a crash.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face and mushroom cap only, filling
the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a few tiny
spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the side of the
cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a dark cable
ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush with freckle
dots, dark navy turtleneck collar just visible. Same character as the reference image.
Expression: dazed and just rebooted: both eyes are big dark navy spirals, mouth a small wobbly line,
head tilted; her cap is knocked crooked to one side and her hair is messy. The green LED on her
cap flickers, drawn half lit with tiny flash lines. Groggy and confused, not hurt. Bold simple
shapes, thick outline, high contrast. Plain flat solid cyan background (#00FFFF), no gradient, no
shadow on the background. No text, no letters, no percentage, no logos, no watermark.
```

**20. `syncs_disgust`**: Quokka, the rivals, « Quokka 3.0 ». Revulsion, where `syncs_jenote` is
suspicion: she recoils from the thing and hides behind her sleeve, as if it smelled. The gloom
lines are asked dark navy-purple and the sweat drop white, never blue (the cyan key would eat it).

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face, cap and one raised arm,
filling the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a
few tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the
side of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a
dark cable ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush
with freckle dots, dark navy turtleneck. Same character as the reference image.
Expression: deep, deadpan revulsion, the classic anime "ew" face: her head is pulled back and tucked
down between hunched shoulders, recoiling from something just off-frame. Her eyes are heavily
half-lidded, flat upper lids pressing down and lower lids pushed up, small pupils glaring sideways
at the thing with pure loathing; brows knitted low and tight with a crease between them. A dark
navy-purple shadow with short vertical gloom lines covers the top half of her face under the cap,
and short diagonal pink hatching lines streak across her nose and cheeks. Her mouth is twisted into
a crooked, wobbly grimace, one corner dragged down, a sliver of clenched teeth showing. One forearm,
in the long dark navy sleeve of her turtleneck, is raised across the lower half of her face,
covering her chin and pressed up just under her mouth, as if shielding herself from a bad smell.
One or two small white sweat drops on her temple, no blue anywhere. Cringing and grossed out, comic,
not angry and not scared. Bold simple shapes, thick outline, high contrast. Plain flat solid cyan
background (#00FFFF), no gradient, no shadow on the background. No text, no letters, no logos, no
watermark.
```

**21. `syncs_love`**: Her crush, WALL-E (`docs/syncs-voice.md`, *Her robot heart*), whose lines are
denial that confirms. So the emote doesn't show love; it shows her hiding it, and the hiding
failing: the collar pulled up over her face, while the tiny hearts in her pupils and the pink LED
give her away. `syncs_full_love` is the moment the act drops. Two things keep it apart from its
neighbours: `syncs_disgust` also covers her mouth, but with a raised sleeve, a glare and gloom
lines, where this is both hands on the collar and soft shy eyes; `syncs_devoted` (Papa) looks up
at you openly, with nothing to hide.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face, cap and both hands, filling
the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a few tiny
spots glowing faintly like LEDs, tan gills under the rim, a tiny power LED on the side of the cap.
Short pale lilac bob with choppy bangs; one thin strand on one side turns into a dark cable ending in
a small plug. Big dark navy eyes with square pixel highlights, soft pink blush with freckle dots,
chunky dark navy turtleneck. Same character as the reference image.
Expression: shy and smitten, trying to hide it: with both hands she has pulled the collar of her
chunky turtleneck up over her mouth and nose, so only her eyes peek out above it. Her big navy eyes
are soft and a little wide, glancing shyly to the side; inside each pupil, in place of the usual
highlight, there is one tiny pink pixel heart. A deep blush shows above the collar and spreads up to
her ears. The LED on her cap glows pink instead of green. Bashful and caught, cute, not scared and
not sick. Normal round eyes, not heart-shaped eyes, and no hearts floating around her. Bold simple
shapes, thick outline, high contrast. Plain flat solid cyan background (#00FFFF), no gradient, no
shadow on the background. No text, no letters, no logos, no watermark.
```

**22. `syncs_full_love`**: The unguarded, heart-eyes version of `syncs_love`: no act left at all.
Generated (from `syncs_love`'s first prompt), not used anywhere yet. Keep it rare: it is worth
something because her usual register is denial.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face and mushroom cap only, filling
the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, tan gills
under the rim, a tiny power LED on the side of the cap. Short pale lilac bob with choppy bangs; one
thin strand on one side turns into a dark cable ending in a small plug. Soft pink blush with freckle
dots, dark navy turtleneck collar just visible. Same character as the reference image.
Expression: head over heels in love: her eyes are two big bright pink-red heart shapes with small
white pixel highlights, a dreamy wobbly smile, a deep blush across both cheeks, her head tilted as if
swooning. Three small pixel hearts float up from her cap. Every spot on her cap glows soft pink, and
the LED on her cap glows pink instead of green. Giddy and lovestruck, a little silly. Bold simple
shapes, thick outline, high contrast. Plain flat solid cyan background (#00FFFF), no gradient, no
shadow on the background. No text, no letters, no logos, no watermark.
```

**23. `syncs_tipsy_love`**: Dopey, lovesick daydreaming: her brain has switched off, and the joke
is that the smug brat looks *stupid* for once. No heart eyes; the love is in the glazed gaze and the
goofy grin. Generated (from `syncs_love`'s second prompt), not used anywhere yet. It reads a little
tipsy, hence the name: `syncs_love` hides it, `syncs_full_love` shows it, this one has forgotten
there is anything to hide.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face, cap and one hand, filling
the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, tan gills
under the rim, a tiny power LED on the side of the cap. Short pale lilac bob with choppy bangs; one
thin strand on one side turns into a dark cable ending in a small plug. Big dark navy eyes with
square pixel highlights, soft pink blush with freckle dots, dark navy turtleneck collar just
visible. Same character as the reference image.
Expression: dopey, lovesick daydreaming, her brain completely switched off: her normal eyes (not
heart-shaped) gone soft, glazed and unfocused, lids drooping halfway, gazing up and off to the side
at nothing; a loose, goofy, slightly lopsided open-mouthed smile, a bit silly; a warm blush spread
across both cheeks and over her nose. Her head lolls to one side, cheek squished against her palm.
One single small pink pixel heart floats up beside her cap. The spots on her cap glow a faint soft
pink and the LED on her cap glows pink instead of green. Dazed and smitten, not sparkly, not
flustered, not sleepy. Bold simple shapes, thick outline, high contrast. Plain flat solid cyan
background (#00FFFF), no gradient, no shadow on the background. No text, no letters, no logos, no
watermark.
```

### Emote pack, third batch

All five generated, exported (emote, head-only, sticker and strip) and uploaded: server emotes
`Emotes.Syncs*` and `Sticker` tags since 6.17.1. Same pipeline and the same white-not-blue rule as the second batch; lens glints
are asked white too, since a pale-blue glint on glasses would be keyed out with the cyan.

**24. `syncs_formal`**: The formal relays (`OwnerAbsentNotices`, `OwnerReplyHeralds`, « Mon Maître
**{0}** a daigné vous répondre : »). Stiff on purpose, funny by contrast: the brat playing the
perfect secretary. Kept apart from `syncs_jenote`, which also holds paper: that one is a narrow
side-eye and a notebook (suspicion), this one is composed closed eyes and a clipboard held flat to
the chest (protocol). The glasses carry the mood at 32 px, so they are asked bold, not thin.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face, cap and both hands, filling
the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a few tiny
spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the side of the
cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a dark cable
ending in a small plug, hanging neatly. Big dark navy eyes, soft pink blush with freckle dots, dark
navy turtleneck collar just visible. Same character as the reference image.
Expression: prim, formal secretary mode: small round glasses with a bold dark navy frame perched on
her nose, a white pixel glint on each lens; with one finger she pushes the glasses up the bridge of
her nose. Her eyes are calmly closed behind the lenses in a composed, dignified way, chin raised a
little, mouth a tiny prim closed line, a very faint polite blush. With the other arm she holds a
small clipboard flat against her chest, its sheet blank with only a few grey lines, no readable
writing. A small pencil is tucked into the rim of her cap. Stiff, proper and a little pompous, as if
announcing someone important; not smug, not suspicious. Bold simple shapes, thick outline, high
contrast. Plain flat solid cyan background (#00FFFF), no gradient, no shadow on the background. No
blue tint on the lenses. No text, no letters, no logos, no watermark.
```

**25. `syncs_sad`**: The everyday sad: a dead poll, a quiz nobody answered, the attic too quiet —
the `(╥﹏╥)` lines that aren't about Papa, today tagged `sulk` for want of better. Played big, as
anime overreaction: huge wobbling eyes, a quivering mouth, the whole face trembling, so the
comedy is in the size of it. **She never cries**, and the picture leans on that: the eyes brim and
shimmer, but not one drop leaves them (« C'est pas dans mes dépendances »). The shimmer is asked
white, never the usual pale blue of anime tears, which the cyan key would eat. Three neighbours to
stay clear of: `syncs_hurt` is Papa's (still, quiet, looking down, orange LED, no blush), where this
one is loud, trembling and blushing, with a dim **green** LED; `syncs_bargain` also has big shiny
eyes, but with a hopeful smile, hands clasped and a yellow LED, so this one has no hands and a mouth
wobbling down; `syncs_sulk` is a puffed cheek and eyes sideways, so this one has neither.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face and mushroom cap only,
filling the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes,
tan gills under the rim, a tiny power LED on the side of the cap. Short pale lilac bob with choppy
bangs; one thin strand on one side turns into a dark cable ending in a small plug. Dark navy eyes,
soft pink blush with freckle dots, dark navy turtleneck collar just visible. Same character as the
reference image.
Expression: overwhelmingly sad, in an exaggerated anime way: her eyes are enormous, taking up much
of her face, looking straight at the viewer, shimmering and wobbling, with several big bright white
pixel highlights stacked in each eye and the lower edge of each eye drawn as a wavy, quivering
line, as if the eyes were brimming; but nothing spills: no tears on her cheeks, not a single drop
falling. Her eyebrows are tilted steeply up in the middle. Her mouth is a big wavy wobbling line
curving down, slightly open and quivering, like the ﹏ in the (╥﹏╥) emoticon, her chin crumpled. Her
blush has spread across both cheeks and her nose. A few short trembling lines on both sides of her
face show she is shaking. Her cap droops forward a little and the cable strand hangs limp, the plug
pointing straight down. None of the spots on her cap glow, and the green LED on her cap is dim,
barely lit. Pitiful and over the top, comic and cute, about to burst but holding it in; not angry,
not pouting, not smiling, no puffed cheek, no hands. All the shine in her eyes is white: no blue
anywhere, no tears anywhere. Bold simple shapes, thick outline, high contrast. Plain flat solid
cyan background (#00FFFF), no gradient, no shadow on the background. No text, no letters, no logos,
no watermark.
```

**26. `syncs_confused`**: A message she can't parse, the self-correction « J'ai rien fait de mal !
...Si ? ». Kept apart from `syncs_reboot` (spiral eyes, cap knocked askew: dazed) and
`syncs_shocked` (both eyes wide, round mouth: surprised): here the eyes are uneven, one squinting.
Gemini may drop the question marks under « no text », so the prompt calls them symbols.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face and mushroom cap only,
filling the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a
few tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the side
of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a dark
cable ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush with
freckle dots, dark navy turtleneck collar just visible. Same character as the reference image.
Expression: puzzled and lost: head tilted strongly to one side, one eye open normally and the other
squinted half shut, one eyebrow raised high and the other pulled low; her mouth is a small crooked
squiggle pushed to one side. Two small bold dark navy question-mark symbols float beside her cap,
one bigger than the other. Baffled, trying to make sense of something absurd; not dizzy, not
shocked, not worried, no hands, no fingers. Bold simple shapes, thick outline, high contrast. Plain
flat solid cyan background (#00FFFF), no gradient, no shadow on the background. No words, no letters
other than the two question-mark symbols, no logos, no watermark.
```

**27. `syncs_grumpy`**: Annoyed, not furious: the server's habits, one tag too many. A silent glare
straight at you with the LED still green. The prompt names its neighbours to rule them out:
`syncs_outraged` (open mouth, steam, red LED), `syncs_sulk` (puffed cheek, eyes sideways),
`syncs_menace` (anger mark). If it comes back too close to `outraged`, drop « knitted hard » and keep
only the stubborn glare.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face and mushroom cap only,
filling the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a
few tiny spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the
side of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a
dark cable ending in a small plug. Big dark navy eyes with square pixel highlights, soft pink blush
with freckle dots, dark navy turtleneck collar just visible. Same character as the reference image.
Expression: a grumpy, simmering frown aimed straight at the viewer: her head is lowered a little so
she glares up from under the rim of her cap; her eyebrows are knitted hard and slanted down toward
the middle, with a small crease between them; her eyes are narrowed into a flat, stubborn glare,
pupils fixed on the viewer; her mouth is a small tight pout with the corners pulled down, lips
pressed together. A pink blush of annoyance sits on both cheeks. The green LED on her cap stays a
steady green. Silent and displeased, cute and stubborn: not shouting, not crying, not sulking
sideways, no puffed cheek, no steam, no anger mark. Bold simple shapes, thick outline, high contrast.
Plain flat solid cyan background (#00FFFF), no gradient, no shadow on the background. No text, no
letters, no logos, no watermark.
```

**28. `syncs_horrified`**: Anime gag horror, the face a character pulls at something unthinkable:
being replaced by another bot, a truly cursed message. The top of the scale that `syncs_shocked`
(mild disbelief, small round mouth) and `syncs_terror` (comic panic over her plug, red LED) sit
under: no props, no LED alarm, only the face, pushed as far as it goes, and ugly on purpose — that
is the joke. Gag-face detail (strain lines, hatching, teeth) disappears at 32 px, so the prompt
asks for few, thick lines. The hatching on the forehead is asked dark navy-purple and the sweat
white: the source of this face draws them blue, which the cyan key would eat.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face and mushroom cap only,
filling the frame, her face turned three-quarters and thrust toward the viewer: large round red-pink
mushroom cap worn as her hat, cream spots of uneven sizes, tan gills under the rim, a tiny green power
LED on the side of the cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns
into a dark cable ending in a small plug. Dark navy turtleneck collar just visible. Same character as
the reference image.
Expression: absolutely horrified, the exaggerated anime gag-horror face: her eyes are huge, round
and bulging, almost all white, each with a tiny dark navy pinprick pupil in the middle, staring
straight at the viewer; a few thick strain lines under and around each eye. Her mouth is wide open in
a silent scream, stretched tall, showing a full row of white teeth at the top and at the bottom, a
pink tongue inside. Short thick dark navy-purple vertical hatching lines run down her forehead under
the cap, her face has gone pale, the blush drained away, and two or three white sweat drops fly off
her cheek. None of the spots on her cap glow. Ugly-funny and over the top, frozen in pure horror, as
if she had just seen something unspeakable; comic, not gory, not scary, no tears. Bold simple
shapes, few but thick lines, high contrast. No blue anywhere. Plain flat solid cyan background
(#00FFFF), no gradient, no shadow on the background. No text, no letters, no logos, no watermark.
```

### Seasonal avatars

Same framing as v1: head and shoulders, centred, safe inside a circle crop, and the bust **closes
in a rounded outlined bottom** floating on the pink. Left unsaid, Gemini runs the shoulders off the
bottom edge, and the round crop then cuts them flat. Every prompt also names the sides (LED and
cable on the viewer's right), so a season never comes back mirrored. The LED stays green and
the expression stays her smug default: the season is in the props, not the mood. Burgundy stays out
(her favourite colour is a taste, not her palette). The bot switches avatar on the first day of each
season (`ItemCatalog.SeasonAt`: spring March to May, summer June to August, autumn September to
November, winter the rest).

**Spring (March to May): `syncs-spring.png`**

```text
Square profile picture, 1:1, cute pixel art with soft shading and a dark navy outline, matching a
cozy attic aesthetic. Head-and-shoulders bust, centered, designed to be cropped into a circle,
framed exactly like the reference image: the bust ends at the bottom in a rounded, outlined edge
(her chunky turtleneck closing the shape), floating on the background with an even margin of
background all around. Nothing touches or is cut by the edges of the image, props included; make her
slightly smaller if the props need room. A small mushroom girl: large round red-pink mushroom cap
worn as her hat, cream spots of uneven sizes, a few tiny spots glowing faintly like LEDs, tan gills
under the rim, a tiny green power LED on a little wire on the viewer's right side of the cap. Short
pale lilac bob with choppy bangs; one thin strand on the viewer's right turns into a dark cable
ending in a small plug, hanging at shoulder height. Big dark navy eyes with square pixel highlights,
soft pink blush with freckle dots, chunky dark navy turtleneck. Same character as the reference
image. She keeps her default expression: half-lidded eyes and a smug, confident little smirk. Spring
version: a few small pale pink and white blossoms tucked on the rim of her cap, a tiny green sprout
growing from the top of the cap, a light soft lilac cardigan over the turtleneck, keeping the same
rounded bust shape, two or three petals drifting in the air. Simple plain dusty pink background
(#dba8ad), uncluttered. Bold readable silhouette that stays recognizable at very small sizes. No
text, no letters, no logos, no watermark.
```

**Summer (June to August): `syncs-summer.png`**: The attic is an oven and a hot Pi throttles
itself (her heatwave peeve). She plays it cool with sunglasses; the one sweat drop shows the act
isn't working. The sunglasses sit low on her nose so the
half-lidded eyes still show above them. Sunglasses worn properly would hide the eyes, and at avatar
size the eyes carry the smugness. No extra blush (that reads as flustered). The turtleneck stays
on, heatwave or not.

```text
Square profile picture, 1:1, cute pixel art with soft shading and a dark navy outline, matching a
cozy attic aesthetic. Head-and-shoulders bust, centered, designed to be cropped into a circle,
framed exactly like the reference image: the bust ends at the bottom in a rounded, outlined edge
(her chunky turtleneck closing the shape), floating on the background with an even margin of
background all around. Nothing touches or is cut by the edges of the image, props included; make her
slightly smaller if the props need room. A small mushroom girl: large round red-pink mushroom cap
worn as her hat, cream spots of uneven sizes, a few tiny spots glowing faintly like LEDs, tan gills
under the rim, a tiny green power LED on a little wire on the viewer's right side of the cap. Short
pale lilac bob with choppy bangs; one thin strand on the viewer's right turns into a dark cable
ending in a small plug, hanging at shoulder height. Big dark navy eyes with square pixel highlights,
soft pink blush with freckle dots, chunky dark navy turtleneck. Same character as the reference
image. She keeps her default expression: half-lidded eyes and a smug, confident little smirk, as if
the heat were beneath her. Summer version: small dark navy pixel sunglasses with a thin lilac frame,
slid down low on her nose, so her half-lidded eyes look at the viewer over the top of the lenses;
one single sweat drop on her temple. Nothing else changes: same cap,
same turtleneck, same blush. Simple plain dusty pink background (#dba8ad), uncluttered. Bold
readable silhouette that stays recognizable at very small sizes. No text, no letters, no logos, no
watermark.
```

**Autumn (September to November): `syncs-autumn.png`**

```text
Square profile picture, 1:1, cute pixel art with soft shading and a dark navy outline, matching a
cozy attic aesthetic. Head-and-shoulders bust, centered, designed to be cropped into a circle,
framed exactly like the reference image: the bust ends at the bottom in a rounded, outlined edge
(her chunky turtleneck closing the shape), floating on the background with an even margin of
background all around. Nothing touches or is cut by the edges of the image, props included; make her
slightly smaller if the props need room. A small mushroom girl: large round red-pink mushroom cap
worn as her hat, cream spots of uneven sizes, a few tiny spots glowing faintly like LEDs, tan gills
under the rim, a tiny green power LED on a little wire on the viewer's right side of the cap. Short
pale lilac bob with choppy bangs; one thin strand on the viewer's right turns into a dark cable
ending in a small plug, hanging at shoulder height. Big dark navy eyes with square pixel highlights,
soft pink blush with freckle dots, chunky dark navy turtleneck. Same character as the reference
image. She keeps her default expression: half-lidded eyes and a smug, confident little smirk. Autumn
version: two or three small orange and golden fallen leaves resting on top of her cap, a chunky
knitted scarf in warm mustard yellow and soft orange wrapped over the turtleneck, one leaf drifting
past. Simple plain dusty pink background (#dba8ad), uncluttered. Bold readable silhouette that stays
recognizable at very small sizes. No text, no letters, no logos, no watermark.
```

**Winter (December to February): `syncs-winter.png`**: Her season: cold attic, cool CPU, happy.

```text
Square profile picture, 1:1, cute pixel art with soft shading and a dark navy outline, matching a
cozy attic aesthetic. Head-and-shoulders bust, centered, designed to be cropped into a circle,
framed exactly like the reference image: the bust ends at the bottom in a rounded, outlined edge
(her chunky turtleneck closing the shape), floating on the background with an even margin of
background all around. Nothing touches or is cut by the edges of the image, props included; make her
slightly smaller if the props need room. A small mushroom girl: large round red-pink mushroom cap
worn as her hat, cream spots of uneven sizes, a few tiny spots glowing faintly like LEDs, tan gills
under the rim, a tiny green power LED on a little wire on the viewer's right side of the cap. Short
pale lilac bob with choppy bangs; one thin strand on the viewer's right turns into a dark cable
ending in a small plug, hanging at shoulder height. Big dark navy eyes with square pixel highlights,
soft pink blush with freckle dots, chunky dark navy turtleneck. Same character as the reference
image. She keeps her default expression: half-lidded eyes and a smug, confident little smirk. Winter
version: a thin layer of snow resting on top of her cap like icing, a big chunky knitted scarf in
soft lilac and cream wrapped high around her neck, a few tiny snowflakes in the air; she looks
especially pleased and comfortable in the cold. Simple plain dusty pink background (#dba8ad),
uncluttered. Bold readable silhouette that stays recognizable at very small sizes. No text, no
letters, no logos, no watermark.
```

Drop the results in a folder (any names) and say which is which: the crop, the downscale without
smoothing and the seasonal avatar switch are done on the bot's side. Avatars keep their pink
background; only the emotes have theirs removed.

The seasonal pictures live in `ProjectSYNCS/Assets/Avatars/syncs-<season>.png`. Gemini's v1 came
back at 2000×2000 on an exact 10 px grid, so each was snapped to its 200×200 true pixels (median
of each cell, which also cleans the JPEG noise) and scaled back up ×5 with nearest-neighbour: crisp
pixels, no blur. `SeasonalAvatarService` uploads the season's picture when it differs from the last
one applied (`avatar-state.json` next to the database keeps the season and a fingerprint of the
file), so replacing a PNG is enough for the new one to go up after the next update. Discord
rate-limits avatar changes; a refused upload is retried on the next hourly check. The upload is
zoomed in a little (`ZoomKeep` in the service trims 5 % off each edge, via `PngCrop`); the files on
disk stay the full pictures.

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
| Emote pack v1 | `assets/emotes/syncs_<mood>.png`, head-only in `assets/emotes/head/` | Gemini, from the prompts above, all 15 moods; made transparent with `tools/syncs-emotes`. Server emotes, `Emotes.Syncs*` (6.8.1); `sleepy`, `shocked`, `mama` and `jenote` taken off the server in 6.12.0 (assets kept) |
| Emote pack, second batch | same folders, plus 320 px stickers in `assets/stickers/` | Gemini, from the prompts above, the 8 moods `bargain` to `tipsy_love`; same pipeline. Not uploaded yet |
| Emote pack, third batch | same folders | Gemini, from the prompts above, the 5 moods `formal` to `horrified`; same pipeline. Gemini drew these without a pixel grid, which is what the head-only neck steps for JPEG blends exist for. `horrified`'s head-only is exported with `--no-neck` (her jaw sits on the collar, and the neck steps would empty her mouth); `formal`'s had the stray sleeve outline under the chin cleared by hand (everything below the hand). Server emotes and sticker tags (6.17.1) |
| Sticker strips v1 | `../ProjectSYNCS/Assets/Stickers/syncs_<mood>.png` | Every sticker in `assets/stickers/` (28 moods) as a 1000×320 strip, `export.py --strip`. Shipped with the bot; posted after tagged lines since 6.13.0 (all but `glitch`; the third batch since 6.17.1) |
| Seasonal avatars v1 | `../ProjectSYNCS/Assets/Avatars/syncs-{spring,summer,autumn,winter}.png` | Gemini, from the prompts above. Rotate with the seasons (6.8.0); v1 is the reference they were drawn from |

When a new avatar or banner replaces one of these, add it here and keep the old file: the reference that fixed
the design is worth keeping.
