"""Explicit, time-bounded policy exceptions (waivers)."""
from __future__ import annotations

import datetime as dt
import re

from .globs import match_any


def parse_date(value) -> dt.date:
    if isinstance(value, dt.date):
        return value
    return dt.date.fromisoformat(str(value))


def status_of(exc: dict, today: dt.date) -> str:
    created, expires = parse_date(exc["created"]), parse_date(exc["expires"])
    if today < created:
        return "pending"
    if today > expires:
        return "expired"
    return "active"


def days_remaining(exc: dict, today: dt.date) -> int:
    return (parse_date(exc["expires"]) - today).days


def matches(exc: dict, finding: dict, checkpoint: str) -> bool:
    if exc.get("rule_id") != finding.get("rule_id"):
        return False
    scope = exc.get("scope", {})
    cps = scope.get("checkpoints")
    if cps and checkpoint not in cps:
        return False
    path = finding.get("file")
    if not path or not match_any(path, scope.get("paths", [])):
        return False
    pattern = scope.get("match")
    if pattern:
        haystack = f"{finding.get('message', '')}\n{finding.get('snippet', '')}"
        if not re.search(pattern, haystack):
            return False
    return True


def apply(exceptions: list[dict], findings: list[dict], checkpoint: str, today: dt.date) -> dict:
    """Mutates findings in place. Returns a report of applied / expired / expiring exceptions."""
    applied, expired_hits = {}, {}
    for f in findings:
        if f["status"] not in ("fail", "needs_review", "missing_evidence"):
            continue
        for exc in exceptions:
            if not matches(exc, f, checkpoint):
                continue
            state = status_of(exc, today)
            f["exception"] = {"id": exc["id"], "status": state, "expires": exc["expires"]}
            if state == "active":
                f["outcome"] = "WAIVED"
                applied.setdefault(exc["id"], []).append(f["finding_id"])
                break
            if state == "expired":
                expired_hits.setdefault(exc["id"], []).append(f["finding_id"])
    return {"applied": applied, "expired_hits": expired_hits}


def expiring_soon(exceptions: list[dict], today: dt.date, warn_days: int) -> list[dict]:
    out = []
    for exc in exceptions:
        if status_of(exc, today) == "active" and days_remaining(exc, today) <= warn_days:
            out.append({"id": exc["id"], "rule_id": exc["rule_id"], "expires": exc["expires"],
                        "days_remaining": days_remaining(exc, today)})
    return out
