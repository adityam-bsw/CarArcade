#!/usr/bin/env python3
"""Builds Docs/ASSET_STATUS.md from Docs/asset-manifest/items/*.json, and can list or update items.
Standard library only.

  python3 tools/assets/report.py                 # rewrite Docs/ASSET_STATUS.md
  python3 tools/assets/report.py list todo       # ids with a status
  python3 tools/assets/report.py list todo world # ids with a status and category
  python3 tools/assets/report.py set ID status [note]   # change a status and append to its history
  python3 tools/assets/report.py show ID
"""
import datetime
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
ITEMS = ROOT / "Docs" / "asset-manifest" / "items"
REPORT = ROOT / "Docs" / "ASSET_STATUS.md"

STATUSES = ["have", "todo", "scouting", "found", "downloaded", "imported", "not_found", "forging", "forged", "ready", "rejected"]
CATEGORIES = ["surface", "world", "vehicle"]


def load():
    items = []
    for p in sorted(ITEMS.glob("*.json")):
        items.append(json.loads(p.read_text(encoding="utf-8")))
    return items


def save(item):
    (ITEMS / f"{item['id']}.json").write_text(json.dumps(item, indent=2) + "\n", encoding="utf-8")


def report():
    items = load()
    lines = ["# Asset status", "",
             f"Generated {datetime.date.today()} from `Docs/asset-manifest/items/*.json` by `tools/assets/report.py`. Do not edit by hand.", ""]
    lines.append("| Category | " + " | ".join(STATUSES) + " | total |")
    lines.append("|---|" + "---|" * (len(STATUSES) + 1))
    for cat in CATEGORIES:
        row = [sum(1 for i in items if i["category"] == cat and i["status"] == s) for s in STATUSES]
        lines.append(f"| {cat} | " + " | ".join(str(n) for n in row) + f" | {sum(row)} |")
    lines.append("")
    for cat in CATEGORIES:
        lines += [f"## {cat.title()}s", "", "| Item | Status | Source | Licence | Project path | Notes |", "|---|---|---|---|---|---|"]
        for i in [x for x in items if x["category"] == cat]:
            src = i.get("source") or ""
            if len(src) > 60:
                src = src[:57] + "..."
            lines.append(f"| {i['name']} | {i['status']} | {src} | {i.get('licence') or ''} | {i.get('project_path') or ''} | {(i.get('notes') or '')[:80]} |")
        lines.append("")
    REPORT.write_text("\n".join(lines), encoding="utf-8")
    print(f"wrote {REPORT.relative_to(ROOT)} ({len(items)} items)")


def main(argv):
    if len(argv) <= 1:
        return report()
    cmd = argv[1]
    if cmd == "list":
        status = argv[2] if len(argv) > 2 else None
        cat = argv[3] if len(argv) > 3 else None
        for i in load():
            if (status is None or i["status"] == status) and (cat is None or i["category"] == cat):
                print(i["id"])
    elif cmd == "show":
        print((ITEMS / f"{argv[2]}.json").read_text(encoding="utf-8"))
    elif cmd == "set":
        item_id, status = argv[2], argv[3]
        note = argv[4] if len(argv) > 4 else ""
        if status not in STATUSES:
            sys.exit(f"unknown status {status}; use one of {STATUSES}")
        path = ITEMS / f"{item_id}.json"
        item = json.loads(path.read_text(encoding="utf-8"))
        item["history"].append({"date": str(datetime.date.today()), "from": item["status"], "to": status, "note": note})
        item["status"] = status
        save(item)
        print(f"{item_id}: {status}")
    else:
        sys.exit(__doc__)


if __name__ == "__main__":
    main(sys.argv)
