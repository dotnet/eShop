"""architecture - the reusable hook interface of the Architecture Policy Pack.

Every consumer calls these operations; none re-implements rules:

    Pre-Commit   -> architecture.validate("pre_commit",  context, artifacts)
    Review Agent -> architecture.validate("code_review", context, artifacts)
    Test Agent   -> architecture.validate("testing",     context, artifacts)
    CI/CD        -> architecture.validate("ci",          context, artifacts)

Operations (also exposed as JSON-in/JSON-out via `arch.py call <operation>`):
    get_applicable_rules(context)        get_rule(rule_id)
    get_pattern(pattern_id)              get_examples(pattern_id)
    validate(checkpoint, context, artifacts)
    explain_finding(finding_id, context)
    get_failure_history(query)           (observability: historic failures recorded by every validate call)
    get_lessons(query)                   (learning loop: how often/why an agent was denied + prompt-ready lessons)
"""
from __future__ import annotations

import time

from . import engine
from . import evidence as evidence_mod
from . import history as history_mod
from . import waivers
from .globs import normalize
from .policy import CHECKPOINTS, PolicyPack, get_pack
from .profile import ProjectIndex

__all__ = ["get_applicable_rules", "get_rule", "get_pattern", "get_examples", "validate", "persist",
           "explain_finding", "get_failure_history", "get_lessons", "OPERATIONS", "CHECKPOINTS", "PolicyPack", "get_pack"]

API_VERSION = "1.0"


class NotFound(LookupError):
    pass


def _pack(pack):
    return pack or get_pack()


def _rule_summary(rule: dict) -> dict:
    return {
        "id": rule["id"], "title": rule["title"], "category": rule["category"], "severity": rule["severity"],
        "mode": rule["validation"]["mode"], "human_review_required": rule["human_review_required"],
        "checkpoints": {cp: (b["enforcement"] if b.get("enabled") else "off") for cp, b in rule["checkpoints"].items()},
        "patterns": rule.get("patterns", []), "validators": rule["validation"]["validators"],
    }


def get_applicable_rules(context: dict | None = None, pack: PolicyPack | None = None) -> dict:
    """Rules that apply to a context.

    context keys (all optional): checkpoint, files (list) or changed_files (list|dict),
    categories, severities, project_types, modes, include_disabled (bool).
    """
    pack = _pack(pack)
    ctx = context or {}
    checkpoint = ctx.get("checkpoint")
    if checkpoint and checkpoint not in CHECKPOINTS:
        raise ValueError(f"unknown checkpoint {checkpoint!r}")
    files = ctx.get("files") or ctx.get("changed_files") or []
    files = [normalize(f) for f in (files.keys() if isinstance(files, dict) else files)]
    index = ProjectIndex(pack.profile)
    out = []
    for rule in pack.active_rules():
        if ctx.get("categories") and rule["category"] not in ctx["categories"]:
            continue
        if ctx.get("severities") and rule["severity"] not in ctx["severities"]:
            continue
        if ctx.get("modes") and rule["validation"]["mode"] not in ctx["modes"]:
            continue
        pts = rule["applicability"].get("project_types")
        if ctx.get("project_types") and pts and not set(pts) & set(ctx["project_types"]):
            continue
        if checkpoint and not rule["checkpoints"][checkpoint].get("enabled") and not ctx.get("include_disabled"):
            continue
        matched = [f for f in files if engine.is_applicable(pack, index, rule, f)]
        if files and not matched:
            continue
        item = _rule_summary(rule)
        item["matched_files"] = matched
        out.append(item)
    return {"api_version": API_VERSION, "checkpoint": checkpoint, "count": len(out), "rules": out}


def get_rule(rule_id: str, pack: PolicyPack | None = None) -> dict:
    pack = _pack(pack)
    rule = pack.rules.get(rule_id)
    if rule is None:
        raise NotFound(f"rule {rule_id!r} not found")
    import datetime as dt
    today = dt.date.today()
    return {
        "api_version": API_VERSION,
        "rule": rule,
        "source": f"architecture-policy/{pack.rule_files.get(rule_id)}",
        "validators": [{k: pack.validators[v].get(k) for k in ("id", "name", "type", "implementation", "description", "checkpoints")}
                       for v in rule["validation"]["validators"] if v in pack.validators],
        "patterns": [{"id": p, "name": pack.patterns[p]["name"], "summary": pack.patterns[p]["summary"]}
                     for p in rule.get("patterns", []) if p in pack.patterns],
        "exceptions": [dict(e, status=waivers.status_of(e, today)) for e in pack.exceptions if e["rule_id"] == rule_id],
    }


def get_pattern(pattern_id: str, pack: PolicyPack | None = None) -> dict:
    pack = _pack(pack)
    pattern = pack.patterns.get(pattern_id)
    if pattern is None:
        raise NotFound(f"pattern {pattern_id!r} not found")
    return {"api_version": API_VERSION, "pattern": {k: v for k, v in pattern.items() if k != "examples"},
            "example_ids": [e["id"] for e in pattern["examples"]]}


def get_examples(pattern_id: str, kind: str | None = None, pack: PolicyPack | None = None) -> dict:
    pack = _pack(pack)
    pattern = pack.patterns.get(pattern_id)
    if pattern is None:
        raise NotFound(f"pattern {pattern_id!r} not found")
    examples = [e for e in pattern["examples"] if not kind or e["kind"] == kind]
    exemplars = [dict(x, exists=(pack.repo_root / x["path"]).exists()) for x in pattern.get("exemplars", [])]
    return {"api_version": API_VERSION, "pattern_id": pattern_id, "examples": examples, "exemplars": exemplars}


def validate(checkpoint: str, context: dict | None = None, artifacts: dict | None = None,
             pack: PolicyPack | None = None) -> dict:
    """Evaluate all applicable rules at a checkpoint and return a PASS/WARN/BLOCK decision.

    context: caller, scope (changed|all), base (git ref), changed_files (list|dict path->A/M/D/R),
             staged (bool, pre_commit), rule_ids, categories, strict (WARN -> non-zero exit),
             write_evidence (bool), evidence_dir, today (YYYY-MM-DD, testing only), memory_files,
             record_history (bool, default true), history_db (path override).
    artifacts: files {path: proposed content}, semantic_judgments [...], approvals [...],
               test_results [paths], coverage_reports [paths], adr_refs {rule_id: ref}.
    """
    pack = _pack(pack)
    started = time.perf_counter()
    result = engine.validate(pack, checkpoint, context, artifacts)
    result["api_version"] = API_VERSION
    result["duration_ms"] = int((time.perf_counter() - started) * 1000)
    result["history"] = history_mod.record(pack, result, context or {})
    return result


def persist(result: dict, context: dict | None = None, pack: PolicyPack | None = None) -> dict:
    """Write the evidence bundle and failure history for a result produced with write_evidence/record_history off.

    Lets multi-pass callers (e.g. Arch_review_Agent resolving semantic judgments) record only their final run.
    """
    pack = _pack(pack)
    ctx = dict(context or {})
    if ctx.get("write_evidence", True) and not result["evidence"].get("bundle_path"):
        result["evidence"]["bundle_path"] = evidence_mod.write_bundle(pack, result, ctx.get("evidence_dir"))
    result["history"] = history_mod.record(pack, result, ctx)
    return result


def get_failure_history(query: dict | None = None, pack: PolicyPack | None = None) -> dict:
    """Historic failures across all callers: trend, top rules/files, recurring findings, exception usage."""
    result = history_mod.query(_pack(pack), query)
    result["api_version"] = API_VERSION
    return result


def get_lessons(query: dict | None = None, pack: PolicyPack | None = None) -> dict:
    """Per-agent learning loop: denial rate/trend, denial reasons, repeat mistakes and prompt-ready lessons."""
    result = history_mod.lessons(_pack(pack), query)
    result["api_version"] = API_VERSION
    return result


def explain_finding(finding_id: str, context: dict | None = None, pack: PolicyPack | None = None) -> dict:
    """Explain a finding recorded in an evidence bundle (or, failing that, its rule)."""
    pack = _pack(pack)
    ctx = context or {}
    finding, run = evidence_mod.find_finding(pack, finding_id, ctx.get("evidence_dir"))
    rule_id = finding["rule_id"] if finding else finding_id.rsplit("-", 1)[0]
    rule = pack.rules.get(rule_id)
    if rule is None:
        raise NotFound(f"finding {finding_id!r} not found in evidence runs and no rule {rule_id!r}")
    patterns = []
    for pid in rule.get("patterns", []):
        p = pack.patterns.get(pid)
        if p:
            patterns.append({"id": pid, "name": p["name"], "summary": p["summary"],
                             "approved_example": next((e for e in p["examples"] if e["kind"] == "approved"), None)})
    explanation = {
        "api_version": API_VERSION,
        "finding_id": finding_id,
        "found_in_run": run["run_id"] if run else None,
        "finding": finding,
        "rule": {k: rule[k] for k in ("id", "title", "category", "severity", "description", "rationale")},
        "why_it_matters": rule["rationale"],
        "how_to_fix": rule["remediation"],
        "patterns": patterns,
        "validation_mode": rule["validation"]["mode"],
        "human_review_required": rule["human_review_required"],
        "exception_process": {
            "register": "architecture-policy/exceptions/exceptions.yaml",
            "max_duration_days": pack.manifest["exceptions"]["max_duration_days"],
            "approver_roles": pack.manifest["exceptions"]["approver_roles_by_severity"].get(rule["severity"], []),
        },
    }
    if finding and finding.get("exception"):
        explanation["exception"] = next((e for e in pack.exceptions if e["id"] == finding["exception"]["id"]), None)
    explanation["history"] = history_mod.finding_history(pack, [finding_id], ctx.get("history_db")).get(finding_id)
    return explanation


OPERATIONS = {
    "get_applicable_rules": lambda req, pack=None: get_applicable_rules(req.get("context", req), pack=pack),
    "get_rule": lambda req, pack=None: get_rule(req["rule_id"], pack=pack),
    "get_pattern": lambda req, pack=None: get_pattern(req["pattern_id"], pack=pack),
    "get_examples": lambda req, pack=None: get_examples(req["pattern_id"], req.get("kind"), pack=pack),
    "validate": lambda req, pack=None: validate(req["checkpoint"], req.get("context"), req.get("artifacts"), pack=pack),
    "explain_finding": lambda req, pack=None: explain_finding(req["finding_id"], req.get("context"), pack=pack),
    "get_failure_history": lambda req, pack=None: get_failure_history(req.get("query", req), pack=pack),
    "get_lessons": lambda req, pack=None: get_lessons(req.get("query", req), pack=pack),
}
