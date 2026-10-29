# SYNCS's appearance — emote pack prompts, third and fourth batches

Part of `docs/syncs-appearance.md`: read it first (the design, the palette, *Making a variant*,
*Formats*, and how to use these prompts under *Ready-to-paste prompts*).

## Emote pack, third batch

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

## Emote pack, fourth batch

`cool` and `slay` are generated and exported (emote, sticker and strip); no head-only. Both are
server emotes (`Emotes.SyncsCool`, `Emotes.SyncsSlay`, read as praise by `BotFeedbackTracker`) and
sticker tags since 6.17.7. Same pipeline and the same white-not-blue rule. Gemini drew `slay` on a
soft cyan, (92,205,219), just past the key's reach of `#00FFFF`: it was snapped to pure cyan
(everything within 60 of that colour) before `export.py`.

**29. `syncs_cool`**: Too cool, showing off: a win she claims, her own systems going perfectly, the
« (⌐■_■) » register. Kept apart from `syncs_smug` (half-lidded eyes, :3 smirk, no props: quietly
superior) and `syncs_formal` (round glasses, closed eyes: protocol): here the eyes are hidden behind
black shades and both hands do finger guns at you. Sunglasses are usually drawn with a blue or cyan
gradient reflection, which the cyan key would eat, so the lenses are asked solid black with white
glints only. The shades and the two pointing hands carry the mood at 32 px, so both are asked big.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face, cap and both hands, filling
the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a few tiny
spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the side of the
cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a dark cable
ending in a small plug. Soft pink blush with freckle dots, dark navy turtleneck collar just visible.
Same character as the reference image.
Expression: effortlessly cool and showing off: she wears bold, chunky black sunglasses with flat
rectangular lenses, slightly too big for her face, that hide her eyes completely; the lenses are
solid black with one diagonal white pixel glint on each, and a small white four-point sparkle at
the corner of one lens. Her head is tilted a little to one side, chin up, and one eyebrow pokes
above the frame of the glasses. Her mouth is a confident lopsided grin pulled to one side, showing
a hint of teeth. Both hands are in front of her at chin height doing finger guns pointed straight at
the viewer: index fingers out, thumbs up like the hammer of a pistol, other fingers curled, the
hands big and clearly readable. The cable strand flicks up jauntily. The green LED on her cap stays
a steady green. Smooth, pleased with herself, a little ridiculous: not smug and half-lidded, not
angry, no real weapons, nothing but her hands. No blue or cyan on the lenses, no gradient
reflection, all glints white. Bold simple shapes, thick outline, high contrast. Plain flat solid
cyan background (#00FFFF), no gradient, no shadow on the background. No text, no letters, no logos,
no watermark.
```

**30. `syncs_slay`**: The « slay » meme, after the pouty-lips emote with fake
lashes and both hands up showing off long nails: serene, untouchable, the 💅 register. Kept apart
from `syncs_cool` (shades, grin, finger guns aimed at you: loud showing off) and `syncs_smug`
(half-lidded, :3, no props): here the eyes are closed under long, heavy lashes, the lips are a big
glossy pout, and both hands are raised close together under her chin, backs to the viewer and
fingers straight up, so the nails face you, as if the viewer weren't worth a look. Lashes, lips and nails
vanish at 32 px unless they're drawn thick and few, so the prompt asks for all three oversized. The
polish is a glossy deep red, close to her favourite burgundy but bright enough to read against the
navy outline (the meme's lilac would melt into her hair); the lips are a brighter glossy pink than
her blush, so they don't merge with it. Every shine is asked white, like every glint.

```text
Square image, 1:1, cute pixel art with soft shading and a dark navy outline, designed as a Discord
emote that must stay readable at 32x32 px. A small mushroom girl, face, cap and both hands, filling
the frame: large round red-pink mushroom cap worn as her hat, cream spots of uneven sizes, a few tiny
spots glowing faintly like LEDs, tan gills under the rim, a tiny green power LED on the side of the
cap. Short pale lilac bob with choppy bangs; one thin strand on one side turns into a dark cable
ending in a small plug. Soft pink blush with freckle dots, dark navy turtleneck collar just visible.
Same character as the reference image.
Expression: the « slay » meme, serene and fabulous: her face looks straight at the viewer, her
eyes calmly closed, drawn as two smooth downward arcs, each fringed with very long, thick, dramatic
dark navy false eyelashes, four or five bold spikes per eye fanning downward, big enough to read at
emote size. Her eyebrows are relaxed and slightly raised in the middle, unbothered. Her mouth is a
big, plump, glossy pout, lips pushed forward and slightly parted, painted a bright glossy pink
clearly stronger than her blush, with one or two white pixel shines on the lower lip. Both hands
are raised at the bottom of the frame, close to the center, just below and on either side of her
chin, almost touching it, with only a small gap between the two hands; the backs of the hands
face the viewer, fingers held straight up and close together, wrists at the bottom edge, so the
nails face the viewer: very long, rounded nails, painted a glossy deep red, each nail with one
white pixel shine. The hands and nails are big and clearly readable, mirroring each other. Two or
three small white four-point sparkles float near the nails. The cable strand stays exactly as in
the reference image, same position and shape, unchanged. The green LED on her cap stays a steady green. Effortless, fabulous and a little
ridiculous, completely unbothered: not sunglasses, not finger guns, not grinning, not angry. No
blue or cyan anywhere, all shines and sparkles white. Bold simple shapes, thick outline, high
contrast. Plain flat solid cyan background (#00FFFF), no gradient, no shadow on the background. No
text, no letters, no logos, no watermark.
```

