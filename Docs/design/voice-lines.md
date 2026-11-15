# RO-B0 voice lines, priority, cooldowns, subtitles

Owner: voice-director. Status: PROPOSED (script and spec only). Date: 2026-11-15.
Reconciled with `VERTICAL_SLICE_BRIEF.md` (source of truth) on 2026-11-15: section 3.3 is the cue id registry that `stage-events.md` and `hazards.md` point to.
Read with: `Docs/design/VERTICAL_SLICE_BRIEF.md` (F3, F4, F7, section 4, section 10 voice task), `Docs/AUDIO_AND_MECHANICS.md`.
The brief names this deliverable `VOICE_AND_CUES.md`; this file is the one requested by the lead and covers the same scope.

## 0. Audio source and licence status (no generated voice)
- **No AI voice and no new TTS output is produced here.** This file is text only.
- Licence status: NOT APPROVED, nothing logged. The existing 54 files in `Assets/Resources/RoadBuilder/Audio/VO` come from macOS `say`
  (voice Zarvox) and are flagged in `Docs/ASSET_LEDGER.md` as needing an Apple licence check or replacement before release.
- Allowed sources for final audio, each needing a ledger row first: (a) a human recording (own voice or hired actor, with a signed
  release), (b) a CC0 or licensed voice pack, (c) licensed TTS only if the user explicitly approves the specific service and the
  licence is logged. Until then: **subtitle plus banner plus SFX only** (section 6), which already works because the engine skips
  missing clips silently.
- File naming for recordings: `{key}_{n}.wav` in `Assets/Resources/RoadBuilder/Audio/VO/`, matching the existing variant rule
  (`key_0..n`), so recordings drop in with no code change.

## 1. Tone guide (kids-safe)
- RO-B0 is a pompous, fussy traffic inspector robot. He thinks the player's road is illegal and writes imaginary tickets. He is
  sarcastic about the road, never about the child. Insults target the road, rules and himself ("my sensors are embarrassed"), not the player.
- Allowed: mock-official language (permit, fine, inspection, paperwork, "illegal!"), dramatic gasps, robot noises, losing graciously.
- Banned: threats of harm, death words (die, kill, dead, crash into pain), blood, scary tone, shaming ("you suck", "loser"), real
  brands, calls to spend money, urgency pressure ("hurry or lose"), anything about ads or gems in speech.
- The Road-Eater is a "grumpy road-muncher". RO-B0 comments on it like a tired inspector ("not on my permit list").
- Delivery notes for the recording: bright, clipped, slightly nasal robot; hazard tells flat and sharp; jokes slower with a dramatic
  drop on the last word.
- Reading level: 6 to 8 words max for jokes, 1 to 3 for tells.

## 2. Length rules
- Hard cap for every line: **2.5 s** (about 7 words at a brisk robot pace, roughly 14 syllables).
- **Tells and warnings (P0, P1): 1 to 3 words, 1.2 s max.** Telegraph floors are 0.8 s for any hazard and 1.2 s or more for boss and Eater telegraphs (`hazards.md` FR-1), so a longer line would be late.
- Subtitle on screen for the audio length plus 0.3 s, minimum 1.0 s.
- QA check: measure each recorded clip; reject any over its cap. Word counts below are the pre-recording estimate, not a measurement.

## 3. Line table
Key format: `vo.rob0.<situation>.<n>`. Variants are picked at random, never the same as the last one played for that key (existing
announcer rule). "Ev" = engine trigger. Priority P0 to P4 is defined in section 4.

### 3.1 Required situations
| Situation | Ev (trigger) | Pri | Variants (text = subtitle) |
|---|---|---|---|
| First appearance (run 1 only, 0:05 after launch, once per profile) | first run start | P3 | 1 "Halt! Is that a road?" / 2 "Inspector RO-B0. Show me your permit!" / 3 "This road is totally illegal!" |
| Boss entrance (2000 m, RO-B0 arrives) | `boss_start` | P2 | 1 "Inspection time! Hold still!" / 2 "RO-B0 is on the case!" / 3 "Illegal road detected. Dramatic sigh." |
| Warning (generic hazard about to land, used when no specific tell fits) | hazard telegraph start | P0 | 1 "Heads up!" / 2 "Incoming!" / 3 "Look out!" |
| Near miss (single, chain 1) | `near_miss` chain 1 | P3 | 1 "Ooh, close!" / 2 "Whoa, that was tight!" / 3 "Almost a ticket!" |
| Dodge (projectile avoided, no near miss) | `proj_dodged` | P3 | 1 "Hmph. Lucky." / 2 "You dodged my paperwork!" / 3 "Fine. Nice move." |
| Hit (heart lost) | crash path, hearts > 0 | P2 | 1 "Ticket issued!" / 2 "Bonk! That is a fine." / 3 "Ouch! Try again?" |
| Monster appears (Road-Eater warn, 940 m) | `eater_warn` | P0 | 1 "Road-muncher!" / 2 "Something is munching!" / 3 "Keep moving!" |
| Escape (Road-Eater left behind: gap 45 m held 8 s) | `eater_escape` | P2 | 1 "You escaped! Rude." / 2 "Munchy is full of rubble!" / 3 "Whew! Road saved." |
| Boss defeated (Overload meter reaches 100) | `boss_end` outcome overload | P2 | 1 "Inspection failed! I mean passed!" / 2 "My circuits are dizzy. You win!" / 3 "Fine! The road is legal. Barely." |
| Low ink (below 20 percent, existing low-ink warning) | low ink | P1 | 1 "Ink is low!" / 2 "Running on fumes!" / 3 "Tiny ink left!" |
| Flip (full flip landed) | `Cue.Flip` | P4 | 1 "Whee! Illegal spin!" / 2 "Tumble technique!" / 3 "That is not in the manual!" |
| High combo (coin or trick combo x5 and above, chain 3+) | combo threshold | P4 | 1 "Combo overload!" / 2 "Stop! My counter is melting!" / 3 "So many points!" |

### 3.2 Readability cues (project brief section 10; never cut, see section 7)
| Situation | Ev | Pri | Max | Variants |
|---|---|---|---|---|
| Bolt tell | `proj_tell` PRJ-BOLT | P0 | 1.0 s | 1 "Bolt!" / 2 "Zap!" |
| Bomb tell | PRJ-BOMB | P0 | 1.0 s | 1 "Bomb!" / 2 "Boom!" |
| Saw tell | PRJ-SAW | P0 | 1.0 s | 1 "Saw!" / 2 "Spinner!" |
| Meteor marker | meteor warning | P0 | 1.0 s | 1 "Rock!" / 2 "Sky rock!" |
| Road-Eater danger (gap under 12 m, hazards band Danger) | gap band danger | P0 | 1.2 s | 1 "Faster!" / 2 "It's close!" |
| Road-Eater last chance (gap under 6 m) | gap band last | P0 | 1.0 s | 1 "Go go go!" / 2 "Hurry!" |
| Road-Eater close call (gap under 4 m, recovered over 12 m) | eater near miss | P3 | 2.0 s | 1 "Munchy missed you!" / 2 "That was a nibble!" |
| Eater caught you | eater_hit | P2 | 1.5 s | 1 "Chomp! Ticket!" / 2 "Nibbled!" |
| Ice event warn (S1 announcer, 5.0 s before the start) | `event_warn` EVT-ICE | P1 | 1.2 s | 1 "Slippery!" / 2 "Ice ahead!" |
| Mud event warn | EVT-MUD | P1 | 1.2 s | 1 "Mud ahead!" / 2 "Gooey road!" |
| Glass event warn | EVT-GLASS | P1 | 1.2 s | 1 "Glass road!" / 2 "Keep moving!" |
| Ring gauntlet start (S1) | EVT-RINGS | P2 | 1.2 s | 1 "Rings! Grab them!" / 2 "Bonus rings!" |
| Event clear | `event_end` | P3 | 1.5 s | 1 "All clear." / 2 "Back to normal." / 3 "Phew, done." |
| Glass crack under car | glass crack step 2 | P0 | 0.8 s | 1 "Cracking!" / 2 "Move!" |
| Mud stall | `mud_stall` | P1 | 1.2 s | 1 "Stuck! Boost!" / 2 "Too gooey!" |
| Last heart | hearts == 1 | P1 | 1.5 s | 1 "Last heart!" / 2 "One heart left!" |
| Ring pass, chain 2 / 3+ | `ring_pass` | P4 | 1.0 s | 2: "Ring! Ring!" / 3+: "Ring master!" "Ring train!" |
| Near miss chain 2 / 3+ | `near_miss` chain | P3 | 1.5 s | 2: "Again?!" "Twice!" / 3+: "Daredevil!" "Too close!" |
| Frenzy on | frenzy start | P3 | 1.5 s | 1 "Frenzy time!" / 2 "Turbo mode!" |
| Frenzy off | frenzy end | P4 | 1.2 s | 1 "Frenzy over." / 2 "Back to boring." |
| Boss retreat | RO-B0 retreat | P2 | 2.0 s | 1 "I'll be back! With forms!" / 2 "Recharging! Do not move!" |
| New record | new best distance | P3 | 2.0 s | 1 "New record! Impressive." / 2 "Record broken! Paperwork!" |
| Eaten (run end screen) | `eater_death` | P2 | 2.0 s | 1 "Munched! Try again?" / 2 "Road eaten. Oops." |
| Defeat by boss (run end) | boss death | P2 | 2.0 s | 1 "Inspection complete. Again?" / 2 "Ticket for you! Retry?" |

### 3.3 Cue id registry (every signal and telegraph referenced by the other docs resolves here)
Id form: `vo.rob0.<id>` for a voice cue (variants `.1`, `.2`, `.3` as in 3.1 and 3.2); `sfx.<id>` for a sound-only cue; `banner.<id>` for the
text fallback (banner text key). Cues marked "3.1" or "3.2" are the existing rows; **NEW** rows are added by the 2026-11-15 reconciliation.
`StageEventDefinition.announcerCueId` holds the `s1` id of its event.

| Cue id | Fired by (document: signal) | Pri | Max s | Source row |
|---|---|---|---|---|
| `vo.rob0.s1.ice` | stage-events S1, EVT-ICE, 5.0 s before start | P1 | 1.2 | 3.2 Ice event warn |
| `vo.rob0.s1.mud` | S1, EVT-MUD | P1 | 1.2 | 3.2 Mud event warn |
| `vo.rob0.s1.meteor` | S1, EVT-METEOR | P1 | 1.2 | **NEW**: 1 "Rock shower!" / 2 "Meteors ahead!" |
| `vo.rob0.s1.glass` | S1, EVT-GLASS | P1 | 1.2 | 3.2 Glass event warn |
| `vo.rob0.s1.rings` | S1, EVT-RINGS | P2 | 1.2 | 3.2 Ring gauntlet start |
| `vo.rob0.s1.eater` | S1, MON-EATER Warn at 940 m (`eater_warn`) | P0 | 1.2 | 3.1 Monster appears |
| `vo.rob0.s1.boss` | S1, BOS-ROB0 intro at 2000 m (`boss_start`) | P2 | 2.5 | 3.1 Boss entrance |
| `vo.rob0.meteor.marker` | hazards: meteor marker 1.1 s | P0 | 1.0 | 3.2 Meteor marker |
| `vo.rob0.eater.surge` | hazards: Eater Surge telegraph 1.2 s | P0 | 1.0 | **NEW**: 1 "Lunge!" / 2 "Here it comes!" |
| `vo.rob0.eater.danger` | hazards: gap band Danger (under 12 m) | P0 | 1.2 | 3.2 Road-Eater danger |
| `vo.rob0.eater.snap` | hazards: Snap countdown 1.0 s (gap under 6 m) | P0 | 1.0 | 3.2 Road-Eater last chance |
| `vo.rob0.eater.nearmiss` | gap under 4 m recovered over 12 m | P3 | 2.0 | 3.2 Eater close call |
| `vo.rob0.eater.hit` | Snap resolves (bite), Pushback | P2 | 1.5 | 3.2 Eater caught you |
| `vo.rob0.eater.escape` | Escape (gap 45 m held 8 s) | P2 | 2.5 | 3.1 Escape |
| `vo.rob0.eater.retreat` | timeout retreat at 1750 m | P2 | 2.0 | **NEW**: 1 "It wandered off!" / 2 "Munchy got bored!" |
| `vo.rob0.eater.eaten` | run end, cause `eater_bite` | P2 | 2.0 | 3.2 Eaten |
| `vo.rob0.proj.bolt` | hazards: Corn Shot tell | P0 | 1.0 | 3.2 Bolt tell |
| `vo.rob0.proj.bomb` | hazards: Pumpkin Bomb tell | P0 | 1.0 | 3.2 Bomb tell |
| `vo.rob0.proj.saw` | hazards: Hay Roller tell (archetype id PRJ-SAW) | P0 | 1.0 | 3.2 Saw tell |
| `vo.rob0.boss.phase` | hazards: phase change telegraph 2.0 s | P2 | 1.5 | **NEW**: 1 "New rules!" / 2 "Stage two!" |
| `vo.rob0.boss.ring` | hazards: Overload Ring appears (one per pattern) | P3 | 1.0 | **NEW**: 1 "Grab the ring!" / 2 "Gold ring!" |
| `vo.rob0.boss.defeat` | Overload reaches 100 | P2 | 2.5 | 3.1 Boss defeated |
| `vo.rob0.boss.retreat` | boss timeout (105 s) or after the defeat sequence | P2 | 2.0 | 3.2 Boss retreat |
| `vo.rob0.boss.death` | run end, boss cause | P2 | 2.0 | 3.2 Defeat by boss |
| `vo.rob0.glass.crack` | hazards: glass web state (1.6 s dwell) | P0 | 0.8 | 3.2 Glass crack under car |
| `vo.rob0.mud.stall` | `mud_stall` | P1 | 1.2 | 3.2 Mud stall |
| `vo.rob0.event.clear` | `event_end` | P3 | 1.5 | 3.2 Event clear |
| `vo.rob0.finish` | finish gate reached | P3 | 1.5 | **NEW**: 1 "Finish line!" / 2 "Inspection passed!" |
| `vo.rob0.warning`, `.nearmiss`, `.dodge`, `.hit`, `.lowink`, `.lastheart`, `.flip`, `.combo`, `.ringchain`, `.frenzy.on`, `.frenzy.off`, `.record`, `.first` | generic and reactive situations | P0 to P4 per 3.1 and 3.2 | per row | 3.1 and 3.2 (existing) |

Sound-only and banner-only cues (no voice by design, so they never depend on the licence decision):
| Id | Fired by | Notes |
|---|---|---|
| `sfx.<event>.sign`, `sfx.<event>.cue` (event = ice, mud, meteor, glass, rings, eater, boss) | stage-events S2 (3.0 s, world sign sting) and S3 (1.0 s, edge cue) | Fields `warnSfx` and `cueSfx` of `StageEventDefinition` |
| `banner.<event>` | S1 banner for each event, 1 to 3 words, 1.5 s | Fallback for every `s1` voice cue |
| `sfx.glass.hairline`, `sfx.glass.drop` | glass hairline (0.8 s dwell) and drop | The web state has the voice cue above |
| `sfx.eater.chew`, `sfx.eater.escapeTick`, `sfx.eater.jumpChew` | chew loop by band, escape timer tick, jump-delay chew | Reward and band feedback |
| `sfx.eater.pushback` plus `banner.eater.pushback` ("Phew!") | Pushback after a bite | No voice: the hit line plays at the same time |
| `banner.eater.escape` ("ESCAPED"), `banner.eater.retreat` ("IT WANDERED OFF") | chase end | Paired with the voice cues above |
| `sfx.proj.fire.bolt`, `sfx.proj.fire.bomb`, `sfx.proj.fire.saw` | projectile launch | Three distinct pitches (primary channel) |
| `sfx.boss.phaseFlare` | phase change flare | Paired with `vo.rob0.boss.phase` |
| `sfx.ice.slip` | ice slip (coasting, no damage) | No voice |

Counts: 12 required situations x 3 variants = 36 lines, plus about 62 readability lines, plus 12 lines from the six NEW cues in 3.3 (about 74; approximate, to be recounted at recording time). All lines are keys with 2 to 3 variants,
avoiding back-to-back repetition. Word counts: every line 7 words or fewer except the three boss-defeat lines and "Inspector RO-B0.
Show me your permit!" (8 words, trim to "Inspector RO-B0! Permit, please!" if the measured clip exceeds 2.5 s).

## 4. Priority table
Lower number wins. Voice is **never queued behind a joke**: low priority is dropped, not delayed.

| Pri | Class | Examples | Preempts | Dropped when |
|---|---|---|---|---|
| P0 | Hazard tell, safety | bolt, bomb, saw, meteor, Eater surge, danger and snap, glass crack, generic warning | Everything, including P0 older than 0.4 s of a different type. Cuts playing audio with a 60 ms fade | Never. Only the same key within its cooldown |
| P1 | Zone and state warning | ice, mud, meteor, glass event warn (S1), low ink, mud stall, last heart | P2 to P4 | A P0 is playing or armed |
| P2 | Story beat | boss entrance, Eater escape, Eater retreat, boss phase change, hit, boss defeat, retreat, gauntlet start, eaten | P3, P4 | P0 or P1 playing |
| P3 | Reactive feedback | near miss, dodge, frenzy on, record, event clear, first appearance | P4 | Any higher playing, or the line is older than 0.5 s when it would start |
| P4 | Banter and jokes | flip, combo, ring chain, frenzy off | Nothing | Any other line playing, any telegraph armed, or banter budget used |

Rules:
1. **One voice channel.** At most one RO-B0 line plays at once. Higher priority cuts lower; equal priority waits only if under 0.3 s
   of the current line remains, else the newer is dropped (P0 vs P0 of a different tell: the newer plays, because tells are a
   different threat each).
2. **Telegraph lock.** From a telegraph start until 0.3 s after impact, P3 and P4 are dropped and no P3/P4 line may start in the 1.0 s
   *before* a scheduled telegraph (StageDirector exposes `NextEvent` warn time; AudioDirector reads it, the audio layer holds no game
   rules). Dodge and near-miss lines that finish after the telegraph lock begins are cut by the P0 rule.
3. **No joke masks a hazard.** P4 lines are only allowed when no hazard, event warn, Road-Eater gap under 24 m (outside the Calm band), or boss attack pattern is
   active. During a boss the only banter allowed is in the 3 s lull between patterns.
4. **Never later than the telegraph window.** A P0 line must start within 0.15 s of its trigger; if the voice bus is busy it cuts
   the current line. If a clip fails to load, the banner and SFX tell still fire (they are independent channels).
5. **Ducking.** Music -6 dB under any line (existing announcer ducking), -10 dB under P0. SFX never duck for voice except looping
   ambience (-3 dB). The tell SFX always plays at full level; voice is layered, not a replacement.
6. **Pause and death.** Pause freezes the voice and resumes it. On death or restart all voice stops and the queue is cleared (matches F0).
7. **Hit stop and slow-mo.** Voice plays at time scale 1 (unscaled audio) so a tell never stretches. Slow-mo does not delay P0.

## 5. Cooldowns
| Scope | Value |
|---|---|
| Global gap between any two lines (except P0 over lower) | 0.4 s after a line ends |
| Same key | 8 s (P4), 6 s (P3), 3 s (P2), 1.5 s (P0 same tell key) |
| Same situation family (near miss chain 1/2/3 count as one family) | 4 s |
| P4 banter budget | at most 1 banter line per 15 s, 6 per run |
| P3 reactive budget | at most 1 per 6 s; near-miss voice only on chain 1 and 3 (chain 2 uses the toast and SFX only unless 10 s since last voice) |
| Anti-repeat | variant index never equals last index for that key; after all variants are used, reshuffle |
| Fresh player | once-per-profile lines (first appearance) store a flag in `SaveService` (no new `PlayerPrefs`); flag is an existing save field to add by unity-implementer |
| Hit lines | max once per heart lost, skipped if a boss or Eater line played in the last 2 s |
| Event clear | skipped if a threat starts within 4 s |
| Post-run | one result-screen line at most (eaten, defeat, or record), 0.6 s after the screen appears |

Budget summary: about 15 to 25 voiced lines in a 5-minute slice run; if telemetry shows over 30, raise the P3/P4 cooldowns first, never P0.

## 6. Subtitle, mute and localisation integration spec
Engine hook: `SoundManager.Say(key)` and `AudioDirector` stay the entry point. This spec adds data only; the lead routes code work.

**Data (ScriptableObject `VoiceLineSO`, new, in Game layer data folder; no magic numbers in code):**
`key`, `priority` (0..4), `cooldownSeconds`, `maxSeconds`, `variants[]` { `locKey`, `clipName`, `lengthSeconds` }, `subtitleSeconds`
(optional override), `bannerLocKey` (optional), `isReadabilityCue` (bool, true means never cut by settings), `onceOnly` (bool).

**Subtitle behaviour:**
- Display in the HUD as a one-line TMP text at the top safe area under the event banner (UITheme tokens for colour, font, size; no
  hardcoded values). RO-B0 name tag with a small icon, text in a dark capsule for contrast.
- Shown for each line from the moment it plays; if there is no clip (current state), the text is shown for `maxSeconds` as a
  text-only fallback so every cue still reads.
- Text and sound start in the same frame. P0 subtitles replace any current subtitle and are bold with a red outline plus a shape
  icon (so not colour only, rule 5 of the readability contract).
- Settings (stored through `SaveService`, existing settings object; no new keys): `Subtitles` default **ON**; `Voice` default ON.
- **Voice off:** the voice bus is muted; subtitles stay as set; SFX tell, banner and haptic still fire. Readability cues have a
  banner fallback (`bannerLocKey`), so they are never silent *and* invisible. If both Voice and Subtitles are off, the banner and SFX still carry every P0 and P1 cue.
- **Master mute / device silent:** same as voice off. Subtitles stay.
- A "reduce shake" setting does not affect voice.

**Localisation:** all subtitle text lives as keys (`vo.rob0.<situation>.<n>`), never in code or scenes. The English strings in section 3 are the
source. Each translation must respect the length cap (re-record or re-measure per language); recordings use the same key plus a language
suffix folder (`VO/en`, `VO/xx`). Dynamic numbers (for example "Eaten at {0} m") use positional placeholders; spoken lines carry no numbers.

## 7. Cues that must never be cut (readability rule 1 to 3 s, brief section 4)
These carry the 1.0 to 3.0 s telegraph and are P0 or P1. They keep their SFX and banner when voice is off, and cannot be removed by
cooldown or budget rules (only the same-key short cooldown applies):
1. PRJ-BOLT, PRJ-BOMB, PRJ-SAW tells (distinct words and distinct pitch; the fire SFX remains the primary channel).
2. Meteor marker.
3. Road-Eater warn at 940 m, surge, danger (gap under 12 m), last chance (gap under 6 m).
4. Ice, mud, meteor and glass event warns (S1, 5 s ahead; the S2 sign at 40 m and S3 cue at 1 s are SFX and never depend on voice).
5. Glass crack.
6. Low ink and last heart (state warnings; P1, may be shortened to a SFX plus subtitle if crowded).
Cuttable first, in order: banter (flip, combo, ring chain, frenzy off), event clear, dodge, near-miss chain 2, record, first appearance.

## 8. Open items and risks
1. **Licence (OPEN, brief question 6):** the existing Zarvox TTS files are unresolved in the ledger. Decide: record by a human or approve a licensed TTS service
   (log licence in `Docs/ASSET_LEDGER.md` before the files are added). Not approved by this document.
2. Clip lengths are unmeasured; every recorded clip must be timed and checked against section 2.
3. The Road-Eater and RO-B0 are not built; keys and events (`eater_warn`, `proj_tell`, `event_warn`) are from the brief and need
   to be raised by the StageDirector or hazard code (lead routes).
4. Needs from other owners: `VoiceLineSO` plus priority queue in `AudioDirector` (unity-implementer), a `SaveService` flag for the
   once-only first-appearance line and for the Subtitles and Voice toggles, a HUD subtitle prefab (ui), localisation table, and a ledger row per recorded file.
5. Sarcasm tone check with kids (ages 6 to 10): ask whether "illegal road" reads as playful; if any tester finds RO-B0 mean, soften the "hit" and "dodge" lines first.
6. Open question: should RO-B0 speak at all outside the boss fight (first appearance and banter) or only in the boss and tell cues? The slice brief says "no voice recordings beyond the cue list", so only the readability cues in 3.2 and the situations marked P0 to P2 are recording-critical; P3 and P4 are optional polish.
