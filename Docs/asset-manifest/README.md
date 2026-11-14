# Asset manifest

One JSON file per wanted asset in `items/` (85 items: surfaces, worlds, vehicles), created by
`tools/assets/make_manifest.py` from the long list in `Docs/WORLD_AND_VEHICLE_CATALOGUE.md`.
One file per item so several agents can work at once without editing the same file.

Fields: `id`, `category` (surface/world/vehicle), `name`, `status`, `requirements`, `have` (project paths already
covering it), `notes`, `source`, `licence`, `licence_evidence`, `download_dir`, `bytes`, `sha256`, `project_path`,
`quality` (1 to 5), `history`.

Status and the pipeline: `.claude/skills/rb-asset-agents/SKILL.md`. Rules: `Docs/ASSET_AGENT_RULES.md`.
Overview table: `Docs/ASSET_STATUS.md` (generated: `python3 tools/assets/report.py`).
Inspector reviews: `reviews/<id>.md`.
