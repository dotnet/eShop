"""Deterministic data-governance validators."""
from __future__ import annotations

from _vcommon import compile_regex, fail, is_comment, match_any


def migration_immutability(ctx) -> list[dict]:
    """Existing EF Core migrations must never be edited, renamed or deleted.

    params: migration_globs, allow_globs (e.g. the model snapshot, which EF regenerates).
    """
    p = ctx.params
    results = []
    for path, status in sorted(ctx.changed.items()):
        if status not in ("M", "D"):
            continue
        if not match_any(path, p["migration_globs"]) or match_any(path, p.get("allow_globs")):
            continue
        if not ctx.applies(path):
            continue
        verb = "modified" if status == "M" else "deleted or renamed"
        results.append(fail(path, None, None, f"Applied migration was {verb}. Add a new migration instead."))
    return results


def database_ownership(ctx) -> list[dict]:
    """A service may only connect to datastores it owns (profile.services[].owns_datastores).

    params: connection_patterns: [regex with a named group 'name']
    """
    shared = {n.lower() for n in ctx.profile.get("shared_datastore_names", [])}
    patterns = [compile_regex(r) for r in ctx.params["connection_patterns"]]
    results = []
    for path in ctx.files:
        project = ctx.index.project_for(path)
        own_service = project.get("service") if project else None
        text = ctx.read(path) or ""
        for regex in patterns:
            for m in regex.finditer(text):
                if is_comment(text, m.start()):
                    continue
                name = m.group("name")
                if name.lower() in shared:
                    continue
                owner = ctx.index.datastore_owner(name)
                if owner and owner != own_service:
                    results.append(fail(path, text, m.start(),
                                        f"Service '{own_service}' connects to datastore '{name}' owned by service '{owner}'."))
    return results
