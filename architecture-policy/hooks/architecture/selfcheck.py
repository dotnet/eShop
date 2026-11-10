"""Integrity checks for the Architecture Policy Pack (schemas, IDs, references, validators, hooks, docs)."""
from __future__ import annotations

import datetime as dt
import json
import re
from pathlib import Path

from . import OPERATIONS, waivers
from .engine import ValidatorContext, is_applicable
from .globs import match_any
from .policy import CHECKPOINTS, PolicyPack
from .profile import ProjectIndex
from .workspace import Workspace

RULE_PREFIX = re.compile(r"^([A-Z]+)-\d{3}$")


class Report:
    def __init__(self):
        self.checks: list[dict] = []

    def add(self, name: str, errors: list[str], warnings: list[str] | None = None, info: str | None = None):
        self.checks.append({"check": name, "status": "fail" if errors else ("warn" if warnings else "pass"),
                            "errors": errors, "warnings": warnings or [], "info": info})

    @property
    def ok(self) -> bool:
        return all(c["status"] != "fail" for c in self.checks)


def run_validator_self_test(pack: PolicyPack, vdef: dict, test: dict) -> tuple[bool, str]:
    rule_id = test.get("rule_id") or next((r["id"] for r in pack.active_rules() if vdef["id"] in r["validation"]["validators"]), None)
    rule = pack.rules.get(rule_id)
    if rule is None:
        return False, f"no rule references {vdef['id']} (rule_id {rule_id!r})"
    files = {k: v for k, v in (test.get("files") or {}).items()}
    exclude = pack.profile.get("global_exclude", [])
    ws = Workspace(pack.repo_root, "memory", memory=files, exclude=exclude)
    index = ProjectIndex(pack.profile)
    changed = dict(test.get("changed") or {p: "A" for p in files})
    checkpoint = test.get("checkpoint") or next(cp for cp in ("code_review", "ci", "testing", "pre_commit") if cp in vdef["checkpoints"])
    run = {"pack": pack, "checkpoint": checkpoint, "workspace": ws, "index": index, "changed": changed,
           "artifacts": dict(test.get("artifacts") or {}), "rule_files": {}, "validators_executed": {}}
    candidates = sorted(set(files) | {p for p, s in changed.items() if s != "D"})
    applicable = [p for p in candidates if is_applicable(pack, index, rule, p)]
    run["rule_files"][rule["id"]] = applicable or candidates
    try:
        results = pack.implementation(vdef["implementation"])(ValidatorContext(run, rule, vdef)) or []
    except Exception as exc:
        return False, f"raised {type(exc).__name__}: {exc}"
    non_pass = [r for r in results if r.get("status", "fail") != "pass"]
    statuses = {r.get("status", "fail") for r in non_pass}
    expect = test["expect"]
    if "error" in statuses:
        return False, f"validator error: {[r['message'] for r in non_pass if r.get('status') == 'error']}"
    ok = {"pass": not non_pass, "fail": "fail" in statuses, "needs_review": "needs_review" in statuses}[expect]
    if ok and test.get("expect_count") is not None:
        ok = len(non_pass) == test["expect_count"]
    detail = f"expected {expect}" + (f" x{test['expect_count']}" if test.get("expect_count") is not None else "") + \
             f", got {sorted(statuses) or ['pass']} x{len(non_pass)}"
    return ok, detail


def selfcheck(pack: PolicyPack, today: dt.date | None = None, run_self_tests: bool = True) -> dict:
    today = today or dt.date.today()
    rep = Report()
    root, repo = pack.root, pack.repo_root
    manifest = pack.manifest

    # 1. YAML + JSON Schema
    try:
        import jsonschema  # noqa: F401
        schema_note = None
        schema_errors = list(pack.schema_errors)
    except ImportError:
        schema_note = "jsonschema not installed"
        schema_errors = ["jsonschema package missing: pip install -r architecture-policy/requirements.txt"]
    rep.add("yaml_parse", list(pack.load_errors), info=f"{len(pack.documents)} documents loaded")
    rep.add("json_schema", schema_errors, info=schema_note or "all documents validated against architecture-rule.schema.json")
    try:
        from jsonschema import Draft202012Validator
        Draft202012Validator.check_schema(pack.schema)
        rep.add("schema_is_valid_json_schema", [])
    except ImportError:
        pass
    except Exception as exc:
        rep.add("schema_is_valid_json_schema", [str(exc)])

    # 2. IDs
    errs = [f"duplicate id {d}" for d in pack.duplicate_ids]
    prefixes = {c["id"]: c["prefix"] for c in manifest.get("categories", [])}
    for rid, rule in pack.rules.items():
        m = RULE_PREFIX.match(rid or "")
        if not m or prefixes.get(rule.get("category")) != m.group(1):
            errs.append(f"{rid}: prefix does not match category {rule.get('category')!r}")
        if pack.rule_files.get(rid) != next((c["file"] for c in manifest["categories"] if c["id"] == rule.get("category")), None):
            errs.append(f"{rid}: defined in {pack.rule_files.get(rid)} but category file differs")
    for pid, p in pack.patterns.items():
        expected = f"patterns/{pid[4:].lower()}.yaml"
        if expected not in pack.documents:
            errs.append(f"{pid}: expected file {expected}")
    rep.add("unique_and_well_formed_ids", errs,
            info=f"{len(pack.rules)} rules, {len(pack.patterns)} patterns, {len(pack.validators)} validators, {len(pack.exceptions)} exceptions")

    # 3. References
    errs, warns = [], []
    for rid, rule in pack.rules.items():
        for v in rule["validation"]["validators"]:
            if v not in pack.validators:
                errs.append(f"{rid}: unknown validator {v}")
        for p in rule.get("patterns", []):
            if p not in pack.patterns:
                errs.append(f"{rid}: unknown pattern {p}")
            elif rid not in pack.patterns[p]["rules"]:
                errs.append(f"{rid} -> {p}: pattern does not list the rule back")
        for ev in rule["required_evidence"]:
            if ev["type"] not in pack.evidence_types:
                errs.append(f"{rid}: unknown evidence type {ev['type']}")
            for cp in ev["checkpoints"]:
                if not rule["checkpoints"][cp].get("enabled"):
                    errs.append(f"{rid}: evidence {ev['type']} required at disabled checkpoint {cp}")
        for ref in rule["remediation"].get("references", []):
            if not ref.startswith("http") and not (repo / ref.split("#")[0]).exists():
                warns.append(f"{rid}: remediation reference not found: {ref}")
    for pid, p in pack.patterns.items():
        for r in p["rules"]:
            if r not in pack.rules:
                errs.append(f"{pid}: unknown rule {r}")
            elif pid not in pack.rules[r].get("patterns", []):
                errs.append(f"{pid} -> {r}: rule does not reference the pattern back")
        for ex in p.get("exemplars", []):
            if not (repo / ex["path"]).exists():
                errs.append(f"{pid}: exemplar path not found: {ex['path']}")
        if not any(e["kind"] == "approved" for e in p["examples"]):
            errs.append(f"{pid}: needs at least one approved example")
        ids = [e["id"] for e in p["examples"]]
        if len(ids) != len(set(ids)):
            errs.append(f"{pid}: duplicate example ids")
    used = {v for r in pack.rules.values() for v in r["validation"]["validators"]}
    for vid in pack.validators:
        if vid not in used:
            warns.append(f"{vid}: not referenced by any rule")
    for pid in pack.patterns:
        if not any(pid in r.get("patterns", []) for r in pack.rules.values()):
            warns.append(f"{pid}: not referenced by any rule")
    rep.add("cross_references", errs, warns)

    # 4. Rule semantics / governance invariants
    errs = []
    for rid, rule in pack.rules.items():
        mode = rule["validation"]["mode"]
        vtypes = {pack.validators[v]["type"] for v in rule["validation"]["validators"] if v in pack.validators}
        if mode == "deterministic" and vtypes != {"deterministic"}:
            errs.append(f"{rid}: deterministic rule must use only deterministic validators")
        if mode == "semantic" and vtypes != {"semantic"}:
            errs.append(f"{rid}: semantic rule must use only semantic validators")
        if mode == "hybrid" and vtypes != {"deterministic", "semantic"}:
            errs.append(f"{rid}: hybrid rule needs both deterministic and semantic validators")
        for cp in CHECKPOINTS:
            beh = rule["checkpoints"][cp]
            if not beh.get("enabled"):
                continue
            supporting = [v for v in rule["validation"]["validators"] if v in pack.validators
                          and cp in pack.validators[v]["checkpoints"]
                          and (pack.validators[v]["type"] != "semantic" or pack.checkpoint_config(cp).get("allow_semantic"))]
            if not supporting:
                errs.append(f"{rid}: enabled at {cp} but no validator can run there")
        if mode == "semantic" and rule["checkpoints"]["pre_commit"].get("enabled"):
            errs.append(f"{rid}: semantic-only rules must not be enabled at pre_commit")
        if rule["human_review_required"] and not any(
                e["type"] == "human_approval" and "code_review" in e["checkpoints"] for e in rule["required_evidence"]):
            errs.append(f"{rid}: human_review_required rules must require human_approval evidence at code_review")
        if rule["severity"] == "critical" and mode == "deterministic" and rule["checkpoints"]["ci"].get("enforcement") != "block":
            errs.append(f"{rid}: critical deterministic rules must block at ci")
    rep.add("rule_governance_invariants", errs)

    # 5. Validators: implementations, regexes, self-tests
    errs, info_tests = [], 0
    for vid, v in pack.validators.items():
        try:
            pack.implementation(v["implementation"])
        except Exception as exc:
            errs.append(f"{vid}: implementation {v['implementation']} not importable: {exc}")
            continue
        for item in (v.get("params") or {}).get("patterns", []) + (v.get("params") or {}).get("require", []):
            try:
                re.compile(item["regex"])
            except re.error as exc:
                errs.append(f"{vid}: invalid regex {item['regex']!r}: {exc}")
        for key in ("trigger",):
            if (v.get("params") or {}).get(key):
                try:
                    re.compile(v["params"][key])
                except re.error as exc:
                    errs.append(f"{vid}: invalid {key} regex: {exc}")
        if v["type"] == "deterministic" and len(v.get("self_tests", [])) < 2:
            errs.append(f"{vid}: deterministic validators need >= 2 self_tests")
        if run_self_tests:
            for t in v.get("self_tests", []):
                info_tests += 1
                ok, detail = run_validator_self_test(pack, v, t)
                if not ok:
                    errs.append(f"{vid} self-test '{t['name']}': {detail}")
    rep.add("validators", errs, info=f"{info_tests} validator self-tests executed")

    # 6. Profile
    errs, warns = [], []
    profile = pack.profile
    for p in profile.get("projects", []):
        if not (repo / p["path"]).is_dir():
            errs.append(f"profile project path not found: {p['path']}")
    classified = {p["path"] for p in profile.get("projects", [])}
    for csproj in sorted(repo.glob("**/*.csproj")):
        rel = csproj.relative_to(repo).as_posix()
        if match_any(rel, profile.get("global_exclude", [])):
            continue
        if csproj.parent.relative_to(repo).as_posix() not in classified:
            errs.append(f"unclassified project: {rel} (add it to profiles/{pack.profile_id}/profile.yaml)")
    service_ids = {s["id"] for s in profile.get("services", [])}
    for p in profile.get("projects", []):
        if p.get("service") and p["service"] not in service_ids:
            errs.append(f"{p['path']}: unknown service {p['service']}")
        if p.get("layer") and p["layer"] not in profile.get("layer_dependencies", {}):
            errs.append(f"{p['path']}: layer {p['layer']} missing from layer_dependencies")
    for m in profile.get("test_mapping", []):
        if not (repo / m["source"]).is_dir():
            errs.append(f"test_mapping source not found: {m['source']}")
    rep.add("profile", errs, warns, info=f"{len(classified)} projects classified")

    # 7. Exceptions
    errs, warns = [], []
    ex_cfg = manifest.get("exceptions", {})
    max_days = ex_cfg.get("max_duration_days", 90)
    for e in pack.exceptions:
        rule = pack.rules.get(e["rule_id"])
        if rule is None:
            errs.append(f"{e['id']}: unknown rule {e['rule_id']}")
            continue
        try:
            created, expires = dt.date.fromisoformat(e["created"]), dt.date.fromisoformat(e["expires"])
        except ValueError as exc:
            errs.append(f"{e['id']}: invalid date: {exc}")
            continue
        if expires < created:
            errs.append(f"{e['id']}: expires before created")
        if (expires - created).days > max_days:
            errs.append(f"{e['id']}: duration {(expires - created).days}d exceeds max {max_days}d")
        allowed = ex_cfg.get("approver_roles_by_severity", {}).get(rule["severity"], [])
        if e["approver_role"] not in allowed:
            errs.append(f"{e['id']}: approver_role {e['approver_role']!r} may not approve {rule['severity']} rule {rule['id']} (allowed: {allowed})")
        for pat in [e["scope"].get("match")] if e["scope"].get("match") else []:
            try:
                re.compile(pat)
            except re.error as exc:
                errs.append(f"{e['id']}: invalid match regex: {exc}")
        if not any(any(repo.glob(p)) for p in e["scope"]["paths"]):
            warns.append(f"{e['id']}: scope paths match no files (stale exception?)")
        status = waivers.status_of(e, today)
        if status == "expired":
            warns.append(f"{e['id']}: EXPIRED on {e['expires']} - renew or remediate")
    for e in waivers.expiring_soon(pack.exceptions, today, ex_cfg.get("expiry_warning_days", 14)):
        warns.append(f"{e['id']}: expires soon ({e['expires']})")
    rep.add("exceptions", errs, warns)

    # 8. Evidence types
    errs = []
    for tid, t in pack.evidence_types.items():
        if t["collected_by"] in ("caller", "discovery") and not t.get("artifact_key"):
            errs.append(f"{tid}: caller/discovery evidence needs an artifact_key")
    rep.add("evidence_types", errs)

    # 9. Hook contract + adapters
    errs = []
    contract_path = root / "hooks" / "operations.json"
    try:
        contract = json.loads(contract_path.read_text(encoding="utf-8"))
        names = {op["name"] for op in contract["operations"]}
        if names != set(OPERATIONS):
            errs.append(f"operations.json {sorted(names)} != OPERATIONS {sorted(OPERATIONS)}")
        if sorted(contract.get("checkpoints", [])) != sorted(CHECKPOINTS):
            errs.append("operations.json checkpoints differ from engine CHECKPOINTS")
        for cp_caller in contract.get("callers", []):
            if cp_caller["checkpoint"] not in CHECKPOINTS:
                errs.append(f"caller {cp_caller['caller']}: unknown checkpoint {cp_caller['checkpoint']}")
    except Exception as exc:
        errs.append(f"hooks/operations.json unreadable: {exc}")
    for adapter in sorted((root / "hooks" / "adapters").glob("*")):
        text = adapter.read_text(encoding="utf-8")
        if "arch.py" not in text:
            errs.append(f"{adapter.name}: adapters must delegate to hooks/arch.py")
        if re.search(r"\b(ARCH|API|SEC|DATA|OBS|RES|ERR|TEST)-\d{3}\b", text):
            errs.append(f"{adapter.name}: adapters must not embed rule IDs/logic")
    rep.add("hook_contract", errs)

    # 10. Failure history store (observability): config, schema creation, round-trip, query
    errs = []
    import sqlite3
    import tempfile
    from contextlib import closing
    from . import history
    hcfg = history.config(pack)
    unknown = set(hcfg["record_outcomes"]) - {"BLOCK", "WARN", "WAIVED", "INFO"}
    if unknown:
        errs.append(f"history.record_outcomes has unknown outcomes {sorted(unknown)}")
    if not str(hcfg["path"]).replace("\\", "/").startswith("evidence/"):
        errs.append(f"history.path {hcfg['path']} should live under the gitignored evidence/ directory")
    gi = root / "evidence" / ".gitignore"
    if gi.exists() and "history/" not in gi.read_text(encoding="utf-8"):
        errs.append("evidence/.gitignore must ignore history/ (local store is not source-controlled)")
    try:
        with tempfile.TemporaryDirectory() as tmp:
            db = str(Path(tmp) / "h.sqlite")
            fake = {"run_id": "run-selfcheck", "timestamp": dt.datetime.now(dt.timezone.utc).isoformat(), "checkpoint": "ci",
                    "caller": "selfcheck", "decision": "BLOCK", "exit_code": 1, "findings": [
                        {"finding_id": "X-1", "rule_id": next(iter(pack.rules), "X"), "outcome": "BLOCK", "status": "fail",
                         "file": "a.cs", "line": 1, "message": "m"}]}
            with closing(history.connect(Path(db))) as conn, conn:
                history._insert_run(conn, fake, set(hcfg["record_outcomes"]))
            q = history.query(pack, {"history_db": db, "min_occurrences": 1})
            if q["totals"].get("runs") != 1 or not q["top_rules"]:
                errs.append(f"history query round-trip mismatch: {q['totals']}")
            with closing(sqlite3.connect(db)) as conn:
                tables = {r[0] for r in conn.execute("SELECT name FROM sqlite_master WHERE type='table'")}
            missing = {"meta", "runs", "findings", "agent_reviews"} - tables
            if missing:
                errs.append(f"history schema missing tables {sorted(missing)}")
    except Exception as exc:
        errs.append(f"history store unusable: {type(exc).__name__}: {exc}")
    rep.add("history_store", errs, info=f"sqlite at {hcfg['path']}, retention {hcfg['retention_days']}d")

    # 11. Agents that consume the hooks (Arch_review_Agent): contract consistent, no embedded rule logic
    errs, agents_found = [], 0
    for card_path in sorted((root / "agents").glob("*/agent.json")):
        agents_found += 1
        try:
            card = json.loads(card_path.read_text(encoding="utf-8"))
        except Exception as exc:
            errs.append(f"{card_path.parent.name}/agent.json unreadable: {exc}")
            continue
        for key in ("id", "name", "version", "result_schema", "operations"):
            if key not in card:
                errs.append(f"{card['id'] if 'id' in card else card_path.parent.name}: agent.json missing {key}")
        ops = [o.get("name") for o in card.get("operations", [])]
        if len(ops) != len(set(ops)):
            errs.append(f"{card.get('id')}: duplicate operation names")
        for o in card.get("operations", []):
            try:
                from jsonschema import Draft202012Validator
                Draft202012Validator.check_schema(o.get("input_schema", {}))
            except ImportError:
                pass
            except Exception as exc:
                errs.append(f"{card.get('id')}.{o.get('name')}: invalid input_schema: {exc}")
        src = card_path.parent / "agent.py"
        if src.exists():
            m = re.search(r"AGENT_OPERATIONS\s*=\s*\{(.*?)\}", src.read_text(encoding="utf-8"), re.S)
            impl = set(re.findall(r'"(\w+)"\s*:', m.group(1))) if m else set()
            if impl != set(ops):
                errs.append(f"{card.get('id')}: agent.json operations {sorted(ops)} != AGENT_OPERATIONS {sorted(impl)}")
        for py in sorted(card_path.parent.glob("*.py")):
            if re.search(r"\b(ARCH|API|SEC|DATA|OBS|RES|ERR|TEST)-\d{3}\b", py.read_text(encoding="utf-8")):
                errs.append(f"{card_path.parent.name}/{py.name}: agents must not embed rule IDs/logic; use the hook operations")
        for d in card.get("invocation", {}).get("agent_definitions", []):
            if not (repo / d).exists():
                errs.append(f"{card.get('id')}: agent definition not found: {d}")
    rep.add("agent_contracts", errs, info=f"{agents_found} agent(s)")

    # 12. Generated documentation in sync
    from . import docs
    md_path = root / "architecture-rules.md"
    current = md_path.read_text(encoding="utf-8") if md_path.exists() else ""
    rep.add("docs_in_sync", [] if current == docs.render(pack) else
            ["architecture-rules.md is stale: run 'python architecture-policy/hooks/arch.py docs'"])

    semantic = [r["id"] for r in pack.active_rules() if r["validation"]["mode"] != "deterministic"]
    stats = {
        "rules": len(pack.rules),
        "rules_by_category": {c["id"]: sum(1 for r in pack.rules.values() if r["category"] == c["id"]) for c in manifest["categories"]},
        "rules_by_severity": {s: sum(1 for r in pack.rules.values() if r["severity"] == s) for s in ("critical", "high", "medium", "low")},
        "rules_by_mode": {m: sum(1 for r in pack.rules.values() if r["validation"]["mode"] == m) for m in ("deterministic", "hybrid", "semantic")},
        "human_review_rules": [r["id"] for r in pack.active_rules() if r["human_review_required"]],
        "semantic_or_hybrid_rules": semantic,
        "validators": len(pack.validators),
        "validators_by_type": {t: sum(1 for v in pack.validators.values() if v["type"] == t) for t in ("deterministic", "semantic")},
        "validator_self_tests": info_tests,
        "patterns": len(pack.patterns),
        "examples": sum(len(p["examples"]) for p in pack.patterns.values()),
        "exceptions": {s: sum(1 for e in pack.exceptions if waivers.status_of(e, today) == s) for s in ("pending", "active", "expired")},
        "evidence_types": len(pack.evidence_types),
        "operations": sorted(OPERATIONS),
        "agents": sorted(p.parent.name for p in (root / "agents").glob("*/agent.json")),
    }
    return {"ok": rep.ok, "checks": rep.checks, "stats": stats, "policy_sha256": pack.content_hash()}
