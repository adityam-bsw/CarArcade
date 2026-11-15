# AI Team Workflow & Dependency Graph

## Recommended execution order
1. game-designer defines feature brief and acceptance criteria.
2. level-stage-director / hazard-designer / economy-balancer work in parallel on separate specs.
3. hires-art-director approves style bible.
4. hires-vehicle-artist, hires-world-artist and hires-surface-artist work in parallel after style approval.
5. animation-vfx-artist and sound-designer work after event/art specs exist.
6. voice-director works from approved event/VO trigger list.
7. unity-implementer integrates approved data and assets.
8. QA-playtester validates integrated build.
9. performance-guard skill runs before release.
10. store-marketer prepares assets only from verified release footage.

## Parallel-safe
- Economy simulation and art bible exploration.
- Vehicle art, world art and surface art after style lock.
- Music and SFX production after audio bible.
- Prompt drafting and event spec review.

## Must be sequential
- Style bible before final asset generation.
- Feature spec before implementation.
- Asset provenance review before shipping.
- Unity integration before integrated QA.
- QA and performance validation before release.
- Store screenshots after real gameplay is stable.

## Shared file ownership
- Game designer: design docs only.
- Art director: style bible and art approvals.
- Artists: their own asset output folders.
- Unity implementer: code/prefabs in assigned scope.
- QA: test reports only.
- No agent edits the same Unity scene or ScriptableObject database concurrently.
