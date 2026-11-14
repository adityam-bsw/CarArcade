---
name: rb-asset-agents
description: Runs the three-agent asset pipeline for Road Builder Racer (scout finds and downloads free art, forge creates what is missing, inspector verifies and imports). Use when the user wants more surfaces, worlds or vehicles, wants to fill the asset manifest, check asset status, or review/approve new art. Defines the item states and how to launch each agent.
---

# Asset agents: scout, forge, inspector

Rules (binding for every agent): `Docs/ASSET_AGENT_RULES.md`. Agent definitions: `.claude/agents/`.
Records: one JSON per item in `Docs/asset-manifest/items/`, status page `Docs/ASSET_STATUS.md`, reviews in
`Docs/asset-manifest/reviews/`, ledger `Docs/ASSET_LEDGER.md`.

## Item states
```
todo -> scouting -> downloaded -> imported -> ready        (scout path; only the inspector sets imported/ready)
todo -> scouting -> not_found -> forging -> forged -> ready (forge path)
any -> rejected (reason in notes)        have = already in the project before the agents existed
```
`found` = a good candidate exists but cannot be downloaded yet (over the size cap, blocked, needs a manual click).
Game code may only reference items that are `ready` or `have`.

## Commands (run from the repo root)
```bash
python3 tools/assets/report.py                  # rewrite Docs/ASSET_STATUS.md
python3 tools/assets/report.py list todo vehicle
python3 tools/assets/report.py set <id> <status> "<note>"
python3 tools/assets/make_manifest.py           # add any new items from the lists (never overwrites progress)
```

## Run order
1. **Scouts, in parallel, one per slice** (about 15 to 20 items each so a run stays focused): surfaces, worlds, vehicles
   (split vehicles in two). Give each: its item ids, a quarantine root, and "follow `.claude/agents/asset-scout.md`".
   Scouts share one browser pane: each must open its own tab and pass its tabId.
2. **Inspector** on everything `downloaded`: verifies, imports, marks `ready` or `rejected`.
3. **Forge** on everything `not_found`: creates what is missing, marks `forged`.
4. **Inspector** again on everything `forged`.
5. `python3 tools/assets/report.py`, then tell the user: what is ready, what is a placeholder, what was blocked,
   and which decisions are theirs (CC-BY fallbacks, placeholder quality, manual downloads).

## Guards
- `AssetLedgerCoverageTests` (EditMode) fails if any art folder lacks a ledger row or licence file, or contains
  scripts/installers/PSD/URL files. Run: `unity command test run --mode EditMode --filter AssetLedgerCoverageTests`.
- Nothing from quarantine reaches `Assets/` except through the inspector.
- Agents never use generative AI, never spoof or bypass site blocks, never run downloaded code.

## When to ask the user
A licence that is CC-BY (credit), a download over 25 MB, a site that needs manual download, or art that is only
placeholder quality. Everything else the agents decide against the rules.
