"""Validation engine: the single implementation behind every checkpoint.

architecture.validate(checkpoint, context, artifacts) -> result with decision PASS / WARN / BLOCK.
"""
from __future__ import annotations

import datetime as dt
import hashlib

from . import evidence as evidence_mod
from . import waivers
from .globs import match_any, normalize
from .policy import CHECKPOINTS, PolicyPack
from .profile import ProjectIndex
from .workspace import Workspace

ENFORCEMENT_OUTCOME = {"block": "BLOCK", "warn": "WARN", "advisory": "INFO"}
MISSING_OUTCOME = {"block": "BLOCK", "warn": "WARN", "info": "INFO"}
RANK = {"PASS": 0, "INFO": 0, "WAIVED": 0, "WARN": 1, "BLOCK": 2}


def cap(outcome: str, ceiling: str) -> str:
    return outcome if RANK[outcome] <= RANK[ceiling] else ceiling


class ValidatorContext:
    """What a validator may see. Validators return a list of result dicts:
    {status: fail|needs_review|pass|error, file?, line?, message, snippet?}."""

    def __init__(self, engine_run: dict, rule: dict, validator: dict):
        self._run = engine_run
        self.rule = rule
        self.validator = validator
        self.params = validator.get("params", {}) or {}
        self.checkpoint = engine_run["checkpoint"]
        self.workspace: Workspace = engine_run["workspace"]
        self.index: ProjectIndex = engine_run["index"]
        self.profile = engine_run["pack"].profile
        self.files: list[str] = engine_run["rule_files"][rule["id"]]
        self.changed: dict[str, str] = engine_run["changed"]
        self.artifacts: dict = engine_run["artifacts"]

    def applies(self, path: str) -> bool:
        return is_applicable(self._run["pack"], self.index, self.rule, path)

    def read(self, path: str) -> str | None:
        return self.workspace.read(path)

    def all_files(self) -> list[str]:
        return self.workspace.list_files()

    def evidence_paths(self, type_id: str) -> list[str]:
        return evidence_mod.collect(self._run["pack"], type_id, self.rule, self._run)

    def judgments(self) -> list[dict]:
        return [j for j in self.artifacts.get("semantic_judgments", []) or [] if j.get("rule_id") == self.rule["id"]]


def is_applicable(pack: PolicyPack, index: ProjectIndex, rule: dict, path: str) -> bool:
    a = rule["applicability"]
    if match_any(path, pack.profile.get("global_exclude", [])):
        return False
    if not match_any(path, a["include"]) or match_any(path, a.get("exclude")):
        return False
    if a.get("project_types"):
        proj = index.project_for(path)
        if not proj or proj["type"] not in a["project_types"]:
            return False
    if a.get("service_traits"):
        if not set(a["service_traits"]) <= index.traits_for(path):
            return False
    return True


def finding_id(rule_id: str, validator_id: str, file: str | None, line, message: str) -> str:
    raw = f"{rule_id}|{validator_id}|{file or ''}|{line or ''}|{message}"
    return f"{rule_id}-{hashlib.sha1(raw.encode()).hexdigest()[:10]}"


def _normalize_changed(changed) -> dict[str, str]:
    if isinstance(changed, dict):
        return {normalize(k): (v or "M")[0].upper() for k, v in changed.items()}
    return {normalize(p): "M" for p in (changed or [])}


def validate(pack: PolicyPack, checkpoint: str, context: dict | None = None, artifacts: dict | None = None) -> dict:
    if checkpoint not in CHECKPOINTS:
        raise ValueError(f"checkpoint must be one of {CHECKPOINTS}, got {checkpoint!r}")
    context = dict(context or {})
    artifacts = dict(artifacts or {})
    started = dt.datetime.now(dt.timezone.utc)
    today = dt.date.fromisoformat(context["today"]) if context.get("today") else dt.date.today()
    cp_cfg = pack.checkpoint_config(checkpoint)
    index = ProjectIndex(pack.profile)
    exclude = pack.profile.get("global_exclude", [])

    if context.get("memory_files") is not None:
        ws = Workspace(pack.repo_root, "memory", overlay=artifacts.get("files"), memory=context["memory_files"], exclude=exclude)
    else:
        staged = checkpoint == "pre_commit" and context.get("staged", True) and context.get("changed_files") is None
        ws = Workspace(pack.repo_root, "index" if staged else "worktree", overlay=artifacts.get("files"), exclude=exclude)

    if context.get("changed_files") is not None:
        changed = _normalize_changed(context["changed_files"])
    else:
        changed = ws.changed_files(checkpoint, context.get("base"))
    for p in (artifacts.get("files") or {}):
        changed.setdefault(normalize(p), "M")
    changed = {p: s for p, s in changed.items() if not match_any(p, exclude)}

    run = {"pack": pack, "checkpoint": checkpoint, "workspace": ws, "index": index, "changed": changed,
           "artifacts": artifacts, "rule_files": {}, "validators_executed": {}}

    wanted_rules = set(context.get("rule_ids") or [])
    wanted_cats = set(context.get("categories") or [])
    findings: list[dict] = []
    rule_results: list[dict] = []
    review_requests: list[dict] = []
    evidence_collected: dict[str, list[str]] = {}

    for rule in pack.active_rules():
        rid = rule["id"]
        if wanted_rules and rid not in wanted_rules:
            continue
        if wanted_cats and rule["category"] not in wanted_cats:
            continue
        beh = rule["checkpoints"][checkpoint]
        if not beh.get("enabled"):
            rule_results.append({"rule_id": rid, "result": "skipped", "reason": f"disabled at {checkpoint}"})
            continue
        enforcement = beh["enforcement"]
        scope = context.get("scope") or beh.get("scope") or cp_cfg.get("default_scope", "changed")
        if scope == "all":
            candidates = ws.list_files()
        else:
            candidates = [p for p, s in changed.items() if s != "D"]
        files = sorted(p for p in candidates if is_applicable(pack, index, rule, p))
        changed_hits = [p for p in changed if is_applicable(pack, index, rule, p)]
        if not files and not changed_hits:
            rule_results.append({"rule_id": rid, "result": "not_applicable", "scope": scope})
            continue
        run["rule_files"][rid] = files
        run["validators_executed"][rid] = []
        rule_findings: list[dict] = []
        semantic_pending = False

        for vid in rule["validation"]["validators"]:
            vdef = pack.validators.get(vid)
            if vdef is None:
                rule_findings.append(_finding(rule, vid, enforcement, {"status": "error", "message": f"Unknown validator {vid}"}, "deterministic"))
                continue
            if checkpoint not in vdef["checkpoints"]:
                continue
            if vdef["type"] == "semantic" and not cp_cfg.get("allow_semantic", False):
                continue
            try:
                fn = pack.implementation(vdef["implementation"])
                results = fn(ValidatorContext(run, rule, vdef)) or []
            except Exception as exc:  # fail closed
                results = [{"status": "error", "message": f"Validator {vid} failed: {type(exc).__name__}: {exc}"}]
            run["validators_executed"][rid].append(vid)
            for res in results:
                if res.get("status", "fail") == "pass":
                    continue
                f = _finding(rule, vid, enforcement, res, vdef["type"])
                rule_findings.append(f)
                if f["status"] == "needs_review":
                    semantic_pending = True

        if semantic_pending or any(f["validation_mode"] == "semantic" for f in rule_findings):
            review_requests.append(_review_request(rule, files or changed_hits, checkpoint))

        present_types = []
        for req in rule["required_evidence"]:
            if checkpoint not in req["checkpoints"]:
                continue
            items = evidence_mod.collect(pack, req["type"], rule, run)
            if items:
                present_types.append(req["type"])
            else:
                rule_findings.append(_finding(rule, f"evidence:{req['type']}", enforcement, {
                    "status": "missing_evidence",
                    "message": f"Required evidence '{req['type']}' is missing at {checkpoint}"
                               + (f": {req['description']}" if req.get("description") else ""),
                }, "evidence", outcome_override=MISSING_OUTCOME[req["on_missing"]]))
        evidence_collected[rid] = present_types
        findings.extend(rule_findings)
        rule_results.append({"rule_id": rid, "files_evaluated": len(files), "scope": scope,
                             "validators": run["validators_executed"][rid], "finding_ids": [f["finding_id"] for f in rule_findings]})

    exc_report = waivers.apply(pack.exceptions, findings, checkpoint, today)
    for exc_id, hits in exc_report["expired_hits"].items():
        exc = next(e for e in pack.exceptions if e["id"] == exc_id)
        rule = pack.rules.get(exc["rule_id"])
        findings.append(_finding(rule, "exception-register", "warn", {
            "status": "exception_expired", "file": "architecture-policy/exceptions/exceptions.yaml",
            "message": f"Exception {exc_id} for {exc['rule_id']} expired on {exc['expires']}; {len(hits)} finding(s) are enforced again. Renew via architecture review or remediate.",
        }, "governance", outcome_override="WARN"))

    for rr in rule_results:
        ids = set(rr.get("finding_ids", []))
        if "finding_ids" not in rr:
            continue
        outcomes = [f["outcome"] for f in findings if f["finding_id"] in ids]
        statuses = {f["status"] for f in findings if f["finding_id"] in ids}
        if not outcomes:
            rr["result"] = "pass"
        elif all(o == "WAIVED" for o in outcomes):
            rr["result"] = "waived"
        elif "fail" in statuses or "error" in statuses:
            rr["result"] = "fail"
        elif "needs_review" in statuses:
            rr["result"] = "needs_review"
        else:
            rr["result"] = "missing_evidence" if "missing_evidence" in statuses else "pass"
        rr["outcome"] = max(outcomes, key=lambda o: RANK[o]) if outcomes else "PASS"

    decision = "PASS"
    for f in findings:
        if RANK[f["outcome"]] > RANK[decision]:
            decision = "BLOCK" if f["outcome"] == "BLOCK" else "WARN"
    exit_codes = pack.manifest.get("decision", {}).get("exit_codes", {})
    exit_code = exit_codes.get(decision, 0)
    if decision == "WARN" and context.get("strict"):
        exit_code = exit_codes.get("WARN_STRICT", 3)

    run_id = "run-{}-{}-{}".format(started.strftime("%Y%m%dT%H%M%S%fZ"), checkpoint,
                                   hashlib.sha1(repr(sorted(changed.items())).encode()).hexdigest()[:6])
    count = lambda key: {k: sum(1 for f in findings if f[key] == k) for k in sorted({f[key] for f in findings})}
    result = {
        "schema": "architecture-validation-result/v1",
        "run_id": run_id,
        "timestamp": started.isoformat(),
        "checkpoint": checkpoint,
        "decision": decision,
        "exit_code": exit_code,
        "caller": context.get("caller", "unspecified"),
        "policy": {"name": pack.manifest.get("name"), "version": pack.manifest.get("version"),
                   "profile": pack.profile_id, "content_sha256": pack.content_hash()},
        "repository": {"head": ws.head(), "branch": ws.branch(), "base": context.get("base"), "source": ws.source},
        "inputs": {"changed_files": changed, "scope_override": context.get("scope"),
                   "artifact_keys": sorted(artifacts.keys()), "today": today.isoformat()},
        "summary": {
            "rules_evaluated": sum(1 for r in rule_results if r["result"] not in ("skipped", "not_applicable")),
            "rules_not_applicable": sum(1 for r in rule_results if r["result"] == "not_applicable"),
            "rules_skipped": sum(1 for r in rule_results if r["result"] == "skipped"),
            "findings_by_outcome": count("outcome"),
            "findings_by_status": count("status"),
        },
        "rule_results": rule_results,
        "findings": findings,
        "review_requests": review_requests,
        "exceptions": {
            "applied": exc_report["applied"],
            "expired": exc_report["expired_hits"],
            "expiring_soon": waivers.expiring_soon(pack.exceptions, today, pack.manifest.get("exceptions", {}).get("expiry_warning_days", 14)),
        },
        "evidence": {"collected": evidence_collected, "bundle_path": None},
    }
    if context.get("write_evidence", True):
        result["evidence"]["bundle_path"] = evidence_mod.write_bundle(pack, result, context.get("evidence_dir"))
    return result


def _finding(rule: dict, validator_id: str, enforcement: str, res: dict, mode: str, outcome_override: str | None = None) -> dict:
    status = res.get("status", "fail")
    base = ENFORCEMENT_OUTCOME[enforcement]
    if outcome_override:
        outcome = outcome_override
    elif status in ("fail", "error"):
        outcome = base
    elif status == "needs_review":
        outcome = cap("WARN", base)
    else:
        outcome = "INFO"
    message = res.get("message", "")
    return {
        "finding_id": finding_id(rule["id"], validator_id, res.get("file"), res.get("line"), message),
        "rule_id": rule["id"],
        "rule_title": rule["title"],
        "category": rule["category"],
        "severity": rule["severity"],
        "validator": validator_id,
        "validation_mode": mode,
        "status": status,
        "enforcement": enforcement,
        "outcome": outcome,
        "file": res.get("file"),
        "line": res.get("line"),
        "message": message,
        "snippet": res.get("snippet"),
        "remediation": rule["remediation"]["summary"],
        "patterns": rule.get("patterns", []),
        "exception": None,
    }


def _review_request(rule: dict, files: list[str], checkpoint: str) -> dict:
    sem = rule["validation"].get("semantic", {})
    return {
        "rule_id": rule["id"],
        "title": rule["title"],
        "checkpoint": checkpoint,
        "human_review_required": rule["human_review_required"],
        "files": files[:200],
        "rubric": sem.get("rubric"),
        "questions": sem.get("questions", []),
        "pass_criteria": sem.get("pass_criteria"),
        "patterns": rule.get("patterns", []),
        "respond_with": {
            "artifact": "semantic_judgments",
            "shape": {"rule_id": rule["id"], "verdict": "pass|fail|not_applicable", "rationale": "string",
                      "reviewer": {"type": "agent|human", "id": "string"},
                      "findings": [{"file": "path", "line": 0, "message": "string"}]},
        },
    }

