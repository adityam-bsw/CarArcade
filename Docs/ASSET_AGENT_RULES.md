# Asset agent rules

Binding for the three asset agents (`asset-scout`, `asset-forge`, `asset-inspector`) and for anyone running them.
Pipeline and states: `.claude/skills/rb-asset-agents/SKILL.md`. Item records: `Docs/asset-manifest/items/*.json`.

## 1. Who does what
| Agent | Does | Never does |
|---|---|---|
| **scout** | Searches, verifies the licence on the creator's own page, downloads into quarantine, inspects, records in the manifest | Imports into `Assets/`, edits game code, creates art |
| **forge** | Makes assets that scouts could not find, from existing project art or from our own code | Uses generative AI, downloads from the web, claims hand-drawn work |
| **inspector** | Checks everything. Imports approved downloads, approves or rejects forged assets, keeps ledger, manifest and project consistent | Approves its own work or skips a check |
Only the inspector may set an item to `ready`. Game code may only reference assets whose item is `ready` or `have`.

## 2. Licences
- **Accept:** CC0, public domain, or an explicit "free to use as you want / credit not required" statement by the author.
- **Accept with a flag:** CC-BY (and OFL for fonts). Use only if no CC0 option exists. Record `credit_required` and the exact credit text.
- **A pack that only forbids reselling or redistributing the pack itself is fine** (for example VISTA). Anything that
  forbids use in a commercial game or inside an app is not.
- **Reject:** NC, ND, SA, GPL/LGPL art, "personal use only", no licence stated, ripped game assets, AI-generated art,
  anything whose licence you could not read on the creator's own page or in the pack.
- The licence must be read from the **creator's page or the file in the download**, not from a blog, a listing site or
  a search snippet. Save that evidence (section 5).

## 3. Where to look
Prefer, in order: kenney.nl, opengameart.org, itch.io, ambientcg.com, polyhaven.com. Anything else only if the page
shows the licence and it is CC0 or public domain. Reasonable effort: about 6 searches or page reads per item, then
mark it `not_found` and say what was tried. One pack often serves several items: record each.

## 4. Downloading
- Quarantine folder only: `<scratchpad>/quarantine/<item-id>/` (outside the project). Each pack in its own folder.
- Limits: 25 MB per file, 150 MB per run. Larger: record the candidate with its size and stop for the user.
- **Never** spoof a User-Agent, bypass a bot block, log in, solve a captcha, or work around a paywall or block.
  If a plain fetch is refused (403), read the page in the built-in browser like a normal visitor. If that also fails,
  record `blocked: needs manual download` with the URL and move on.
- **Never** execute, import or keep downloaded code, installers or scripts (`.js .py .sh .bat .exe .dll .app .url .jar`).
  Do not run an archive's contents. List a zip (`unzip -Z1`) and reject names containing `..` or absolute paths before
  extracting, and extract only into its own quarantine folder.
- Record size and SHA-256. A byte-identical duplicate of an existing asset is not new: link to it.

## 5. Evidence and records (every asset)
1. `LICENCE_EVIDENCE.txt` next to the art: source URL, author, licence name, the exact wording, date read, who read it.
   (A pack that ships its own `License.txt` may use that instead.)
2. A row in `Docs/ASSET_LEDGER.md` (path, source, author, licence, credit, date, used for).
3. The item's manifest file updated (`source`, `licence`, `licence_evidence`, `project_path`, `quality`, `history`).
4. Everything under `Assets/Art/<Group>/<Pack>/` is covered by 1 and 2. The EditMode test
   `AssetLedgerCoverageTests` fails the build otherwise.

## 6. Quality bar (inspector checklist)
- **All:** transparent background where expected; no junk files; PNG readable (`tools/assets/pixel.py`); Unity import
  is point filtered, no mipmaps, uncompressed, sensible pixels-per-unit (pixel art 16, matching our world); console clean.
- **Vehicle:** side view facing right; about 24 to 80 px wide at 16 px per unit; one consistent pixel density; wheels
  separate or wheel positions noted; fits the rig type (two wheel, four wheel, hull, runner, hover).
- **Surface:** tiles horizontally (edge seam score under 0.05) or is a strip we can tile; rows for top, body, skirt;
  palette-limited; a physics hint (grip low, normal or high).
- **World:** four layers (sky, far, mid, near) at 384x216, or convertible to that; loops horizontally.
- **Style:** pixel art that sits next to VISTA and Kenney. Vector or smooth art is `rejected` or marked `needs_conversion`.
- **Forged assets** are rated honestly 1 to 5. Below 3 is a `placeholder` and must say so.

## 7. Forge rules
- **No generative AI.** Do not call any image or model generation tool. Allowed: recolour, mirror, crop, compose or
  re-rig existing project art; procedural generation with our own code (`tools/assets/pixel.py`, Unity editor scripts);
  drawing pixel by pixel from explicit coordinates.
- Derivatives name their sources. A derivative of CC0 art is CC0 but still gets a ledger row listing the sources.
- Output goes to `Assets/Art/<Group>/Forged/<item-id>/` with `FORGE_NOTES.txt` (method, sources, limits).
- Never describe a procedural result as hand-drawn.

## 8. Reports
Every agent ends with a short report: items touched, new status, what failed and why, anything needing a human
decision (a CC-BY choice, a size over the cap, a blocked download, a placeholder-quality asset).
