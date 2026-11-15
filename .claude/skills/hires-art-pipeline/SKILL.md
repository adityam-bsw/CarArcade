---
name: hires-art-pipeline
description: Use when producing, reviewing, importing, or scaling high-resolution 2D art.
---
# High-Resolution Art Pipeline

## Procedure
1. Read ROAD_BUILDER_ART_BIBLE.md and approved reference sheet.
2. Confirm asset type, camera, dimensions, palette, outline, lighting, and alpha requirements.
3. Generate concept variations using the shared prompt template; never use unrelated style prompts.
4. Select one master asset and create controlled variations.
5. Separate riggable parts (body, wheels, driver, glass, accessories) where needed.
6. Clean edges, remove unwanted shadows/backgrounds, normalize baseline and pivot.
7. Record provider/model, prompt, references, date, output ID, edits, licence terms, author and reviewer.
8. Export lossless source PNG/SVG and game-ready variants.
9. Compare against style bible and reject drift.
10. Import through approved Unity presets and atlas groups.

## Rules
- Never use scraped Pinterest/Google images.
- Never imitate a named game's exact characters, logos, or assets.
- AI output is not automatically rights-clear; verify tool terms and input rights.
- Preserve source files and provenance.
- Keep gameplay geometry independent of art.
- Generate pixel-art alternative only when requested by the art-style pipeline.

## Checklist
- [ ] Style matches.
- [ ] Alpha edges clean.
- [ ] Correct orientation/baseline.
- [ ] Source and licence recorded.
- [ ] Mobile dimensions appropriate.
- [ ] Unity import preset applied.
