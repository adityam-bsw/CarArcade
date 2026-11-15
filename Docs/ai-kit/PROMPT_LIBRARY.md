# Road Builder Racer — Shared Prompt Library

## Global style bible prefix
Append this prefix to every visual prompt:

"Original polished high-resolution 2D mobile arcade racing game illustration. Consistent hand-painted cartoon rendering, clean readable silhouette, controlled dark charcoal outline, warm directional light from upper-left, subtle material highlights, cohesive palette of workshop charcoal #252B36, racing orange #F4A340, signal red #D94B40, warm cream #E7E0CF and workshop teal #6B9B87. Orthographic side-view when depicting gameplay assets. No text, no logos, no watermark, no imitation of existing game assets. Clean alpha edges, production-ready asset."

## Vehicle master prompt
Create an original compact off-road vehicle for a side-view physics racing game. Exact orthographic side view facing RIGHT, both wheels visible, full vehicle in frame, separate-looking body, front wheel, rear wheel, suspension, driver and glass. Transparent background. No ground shadow. 1024x512. Keep wheel diameter 30% of body height, consistent ground baseline, body width 3 wheel diameters. Include no text or brand marks. Generate one vehicle only.

## Vehicle parts extraction prompt
Using the supplied approved vehicle master as the only design reference, create a clean separated sprite sheet with independent body, front wheel, rear wheel, driver, glass, suspension and accessory parts. Preserve exact proportions, orientation, colours, line weight and alignment. Transparent background, no new design features. Include consistent pivots and no baked shadow.

## World far-layer prompt
Create only the far background layer for [WORLD]. Wide 2D side-scrolling painted game environment, atmospheric distance, [WORLD PALETTE], consistent upper-left lighting. No road, no vehicle, no foreground props, no text. Horizontal panorama with seamless left/right continuation. 2048x512.

## World mid-layer prompt
Create only the mid-distance terrain layer for [WORLD]: hills, rock formations and world-specific silhouettes. Transparent background, no sky, no road, no vehicle. Consistent horizon and light direction with supplied far-layer reference. 2048x768.

## World near-layer prompt
Create only the near terrain silhouettes and medium props for [WORLD]. Transparent background. Keep central gameplay lane visually quiet. No road collision geometry, no vehicle, no text. Match approved style reference. 2048x768.

## Foreground prop atlas prompt
Create a clean atlas of 12 isolated [WORLD] props, each with consistent scale, outline, lighting and palette. Separate objects with generous transparent spacing. No shadows connecting objects, no text, no labels, no watermark. 2048x2048.

## Seamless road surface prompt
Create a perfectly tileable 2D road material texture for [SURFACE]. Orthographic flat material view, no perspective, no vehicle, no shadows, no scenery. Left/right and top/bottom edges must tile seamlessly. Include only subtle surface detail. 1024x256. Gameplay behaviour is implemented separately in Unity.

## Surface damage overlay prompt
Create a transparent overlay atlas for [SURFACE] showing intact, warning, damaged and destroyed visual states. Match approved surface palette and outline. No background and no gameplay text. Each state isolated and clearly separable.

## Icon prompt
Create one original 128x128 game UI icon for [ICON]. Strong silhouette, readable at 48px, limited 3-colour palette, dark outline, transparent background, no text, no stock-icon appearance.

## Store screenshot prompt
Create a promotional composition using supplied REAL gameplay screenshot as the main image. Preserve the gameplay screenshot exactly; do not invent UI or mechanics. Add a short caption area and subtle decorative artwork consistent with the style bible. Google Play landscape screenshot composition. No fake claims, no other game branding.

## Robot projectile prompts
- Ice: crystalline shard, sharp readable silhouette, cyan-white highlights, transparent background.
- Volcano: glowing lava bomb with dark crust and orange cracks.
- Underwater: translucent pressure bubble with bright rim.
- Candy: colourful gumdrop bomb with sticky trail.
- Synthwave: neon energy bolt with zigzag silhouette.
All projectiles must be readable at 64px and have a clear hitbox silhouette.

## Music prompt template
"Original loopable [WORLD] arcade game background music. [BPM] BPM, [MOOD], [INSTRUMENTS], 60–90 seconds, seamless loop, no vocals, no recognizable melody, no copyrighted style imitation. Leave frequency space for SFX and robot voice. Provide intro, loop body and optional intensity stem."

Suggested world palette:
- Hills: 112 BPM, playful acoustic percussion and chiptune.
- Desert: 100 BPM, warm plucked strings and light electronic pulse.
- Snow: 92 BPM, airy bells and soft synth.
- Moon: 105 BPM, spacious analog synth and low percussion.
- Volcano: 128 BPM, driving drums and distorted synth bass.
- Underwater: 90 BPM, fluid pads and bubble-like percussion.
- Candy: 124 BPM, playful mallets and bright chiptune.
- Cave: 96 BPM, low percussion and echoing plucks.
- Synthwave: 118 BPM, retro synth bass and gated drums.
- Haunted: 88 BPM, quirky detuned keys and soft pulses.

## SFX prompt template
"Create an original short game sound effect for [EVENT], duration [0.2–1.5s], clean transient, no background music, no recognizable copyrighted sample. Deliver dry version and optional tail. Must remain distinct on a small phone speaker."

## Robot voice prompt template
"Original fictional sarcastic mechanical robot announcer voice. Clear diction, playful deadpan, short line under 2.5 seconds, no imitation of a known actor or character. Line: [TEXT]. Deliver clean isolated voice, consistent pitch and processing across the pack."
