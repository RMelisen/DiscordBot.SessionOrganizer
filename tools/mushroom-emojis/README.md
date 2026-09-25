# Mushroom emojis

The Champignons collection's pictures: 30 pixel-art sprites from the *[ValleyFriends] Edible
Mushrooms* pack (`Items.txt` is the pack's own list, with the Latin names). Each file is named
after its item key — `girolle.png` is `col.girolle` — which the scratch harness checks.

`upload.py` scales them up ×8 and uploads them as the bot's application emojis, then writes
`ProjectSYNCS/Helpers/Emotes.Mushrooms.cs`. Run it once with the production bot's token:

```powershell
$env:DISCORD_TOKEN = "<prod bot token>"
python tools/mushroom-emojis/upload.py
```

It reuses emojis that already exist, so it is safe to run again (after adding a sprite, say).
`--offline` writes the 🍄 fallback file without touching Discord.
