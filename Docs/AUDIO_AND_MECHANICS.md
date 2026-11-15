# Audio, robot announcer, frenzy, rings, meteors

## Audio
- `Core/SoundManager`: SFX by name (`PlaySfx`), engine loop, crossfading music (`PlayMusic(worldId|"menu")`), announcer (`Say(key)`).
  Clips load lazily from `Resources/RoadBuilder/Audio/{Sfx,Music,VO}`; a missing clip is skipped silently.
- `Game/Gameplay/AudioDirector`: decides what plays. Music by screen (menus = `menu`, world door + race = the world's track, game over ducks),
  sound + robot line for each `RunDirector.Cue`, low-ink warning, meteor warning and impact. No game rules inside.
- Announcer: a line is skipped while another plays unless important; per-key cooldown; variants `key_0..n` never repeat back to back. Music ducks while it talks.
- `Core/Editor/AudioImportRules` sets import: mono Vorbis, music streams, VO compressed in memory, SFX decompress on load.
- Regenerate: `python3 tools/audio/make_music.py|make_sfx.py OUT`, `python3 -I tools/audio/make_vo.py OUT`. Edit `S` in make_music.py to retune a world.
- Open: VO uses macOS TTS (see ledger); music is synth chiptune, swap in licensed tracks by dropping a file with the world id as its name.

## Mechanics (numbers in `GameplayTuning`)
- **Frenzy**: meter fills from coins (x combo), flips, air time, rings, near misses. Full = FRENZY for `frenzyDuration`: +speed, x2 coins, free ink, rainbow trail, music speeds up.
- **Boost rings**: appear over jump-coin arcs. Fly through: free boost burst, coins, frenzy charge.
- **Meteor storms**: after `meteorStartDistance`, a red marker warns, a rock strikes where the car will be. In blast radius = crash (shield absorbs); within `nearMissRadius` = NEAR MISS bonus + frenzy charge.
- Camera shake (`GameCameraRig.Shake`) on crash, impact, boost, ring, frenzy start.
- Not done yet: HUD frenzy meter (the toast and PowerText show it), per-world hazards, new UI animations.
