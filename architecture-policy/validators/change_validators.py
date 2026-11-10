"""Deterministic change-set validators."""
from __future__ import annotations

from _vcommon import fail, match_any


def tests_accompany_changes(ctx) -> list[dict]:
    """Production changes in a mapped project must be accompanied by changes in its test projects.

    Uses profile.test_mapping. params: ignore_globs (changes that never require tests).
    """
    ignore = ctx.params.get("ignore_globs", [])
    mapping = ctx.profile.get("test_mapping", [])
    changed_paths = [p for p, s in ctx.changed.items() if s != "D"]
    pending: dict[str, list[str]] = {}
    for path in sorted(changed_paths):
        if not ctx.applies(path) or match_any(path, ignore):
            continue
        for entry in mapping:
            if path.startswith(entry["source"].rstrip("/") + "/"):
                pending.setdefault(entry["source"], []).append(path)
                break
    results = []
    for source, files in pending.items():
        tests = next(e["tests"] for e in mapping if e["source"] == source)
        if any(match_any(p, tests) for p in changed_paths):
            continue
        results.append(fail(files[0], None, None,
                            f"{len(files)} changed file(s) in {source} without accompanying test changes in {', '.join(tests)}."))
    return results
