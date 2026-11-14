---
name: rb-game-ux
description: Mobile game UX design for Road Builder Racer - screen flow, thumb zones, HUD, onboarding, reward and store moments, session design, feedback and accessibility, with a review checklist. Use before building or changing any screen, popup, HUD element, tutorial, menu flow or reward, and when a screen feels cluttered, confusing or boring.
---

# Game UX for Road Builder Racer

Think as the player first (`unity-gamer`), then design. The target player: casual, one hand, on a
bus, 2-5 minute sessions, never reads text.

## Principles

1. **One screen, one job, one obvious primary action.** The biggest, brightest button is the thing
   we want them to press.
2. **Playing in under 10 seconds** from app open: Boot -> Home -> Play. No login wall, no popups
   before the first run.
3. **Show, don't tell.** Teach by playing: first run is a guided stroke with a hand-pointer and the
   world forgiving mistakes. No text walls. Max 6 words per instruction.
4. **Every input answers within 100 ms** (visual + sound + haptic). Dead taps are the #1 reason
   for 1-star reviews.
5. **Fair deaths only.** The player must always be able to say "my mistake". Unfair deaths get a
   quick restart and a rescue, never an ad wall.
6. **Retry in one tap, under 1 second.** Game over is a launchpad: best score, what to do next,
   Retry as the biggest button.
7. **Rewards are felt, not read.** Coins fly to the counter, numbers count up, bars fill, with sound.
8. **Respect the player:** ads only opt-in (rewarded) or at natural breaks, never mid-run, and
   always skippable on exit. Fair economy. This is what earns 5 stars.

## Layout rules (landscape primary, portrait must not break)

- Safe area on every edge. Keep important HUD inside it.
- **Thumb zones:** primary actions bottom corners/bottom centre. Drawing area is the middle and
  must **never** be covered by buttons or HUD (the finger draws there).
- Touch target at least 48 dp (about 9 mm). Space between targets at least 8 dp.
- HUD in corners, low contrast when idle, bright when it matters (ink low = pulse).
- Text: minimum 14 sp body, 20+ sp for key numbers. High contrast. Numbers use the display font.
- Test at 21:9, 16:9, 4:3 and 9:16 (`unity-canvas-ui`).

## Standard flow

`Boot -> Home -> [Mode select] -> Gameplay -> Result -> (Retry | Home | Upgrade)`

- **Home:** Play (huge), mode card (swipe/tap to change), coins, small icons for Garage, Daily,
  Settings. One notification dot at most.
- **Mode select:** cards with a one-line pitch and a looping preview. Locked = silhouette + price
  or goal, never an empty grey box.
- **HUD:** distance (largest), ink bar, lives, coins. Nothing else. Mode-specific elements (eraser
  distance, cargo health) appear in a reserved top-centre slot.
- **Result:** distance count-up, new-best celebration, coins earned, one upgrade suggestion the
  player can afford, Retry primary.
- **Settings:** sound, music, haptics, left-hand layout, quality, credits, privacy link.

## Session and retention design

- Short loop (run 1-3 min) + medium loop (afford the next upgrade in 2-3 runs) + long loop
  (unlock a mode/vehicle in days) + daily hook (Daily Run, login chest, one daily goal).
- Always show the **next goal** ("120 m to beat your best", "80 coins to Sport Car").
- Near-miss feedback ("so close!") beats silent failure.
- Never punish quitting; save state; welcome back without a guilt popup.

## Onboarding script (first run)

1. Car starts on a short road. Pointer shows a swipe. Text: "Draw the road!"
2. A small gap. Player draws, succeeds, big praise.
3. Ink bar highlighted once: "Ink runs out!". Coin arc teaches planning.
4. No more text. Introduce each new element the first time it appears, once.

## Accessibility and kids

- Never rely on colour alone (add shape/icon). Haptics and shake can be turned off.
- If the audience includes children, apply `unity-kids-safety` before choosing any SDK.

## Review checklist (run before calling a screen done)

- [ ] Primary action obvious in 1 second? (squint test)
- [ ] Works one-handed, drawing area free?
- [ ] Every tap gives immediate feedback (visual, sound, haptic)?
- [ ] Safe area, 4 aspect ratios, large font scale OK?
- [ ] Empty, locked, loading and error states designed?
- [ ] Back/close always available and consistent?
- [ ] Reward moments animated (see `rb-motion-design`)?
- [ ] Copy is 6 words or fewer per instruction, no jargon?
- [ ] Player can recover from every mistake in one tap?

For real validation write a script with `unity-playtest-script` and watch 5 new players.
