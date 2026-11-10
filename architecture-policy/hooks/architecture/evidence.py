"""Evidence collection and evidence bundles (immutable record of every validation run)."""
from __future__ import annotations

import hashlib
import json
from pathlib import Path

from .globs import normalize


def collect(pack, type_id: str, rule: dict, run: dict) -> list:
    """Return the evidence items of `type_id` available for `rule` in this run (empty = missing)."""
    spec = pack.evidence_types.get(type_id)
    if spec is None:
        return []
    artifacts = run["artifacts"]
    rid = rule["id"]
    if type_id == "validator_output":
        return [v for v in run["validators_executed"].get(rid, [])]
    if type_id == "change_set":
        # The engine always computes the change set; an empty set is still evidence of what was evaluated.
        return [{"count": len(run["changed"]), "files": dict(sorted(run["changed"].items()))}]
    if type_id == "exception_record":
        return [e["id"] for e in pack.exceptions if e.get("rule_id") == rid]
    key = spec.get("artifact_key")
    value = artifacts.get(key) if key else None
    if type_id in ("review_judgment", "human_approval"):
        items = [j for j in (value or []) if j.get("rule_id") == rid]
        if type_id == "human_approval":
            items = [j for j in items if j.get("approver")]
        if type_id == "review_judgment":
            items = [j for j in items if j.get("verdict") in ("pass", "fail", "not_applicable")]
        return items
    if type_id == "adr_reference":
        refs = (value or {}).get(rid) if isinstance(value, dict) else None
        return [refs] if refs else []
    # file-based evidence (test results, coverage)
    paths = [normalize(p) for p in (value or [])]
    if not paths and spec.get("collected_by") == "discovery":
        paths = discover(pack.repo_root, spec.get("discovery_globs", []))
    ws = run["workspace"]
    return [p for p in paths if ws.exists(p)]


def discover(repo_root: Path, globs: list[str]) -> list[str]:
    found = set()
    for g in globs:
        for p in repo_root.glob(g):
            if p.is_file() and "node_modules" not in p.parts:
                found.add(p.relative_to(repo_root).as_posix())
    return sorted(found)


def write_bundle(pack, result: dict, out_dir: Path | None = None) -> str:
    out_dir = Path(out_dir or pack.root / pack.manifest.get("evidence", {}).get("output_dir", "evidence/runs"))
    out_dir.mkdir(parents=True, exist_ok=True)
    payload = json.dumps(result, indent=2, sort_keys=False, default=str)
    digest = hashlib.sha256(payload.encode()).hexdigest()
    path = out_dir / f"{result['run_id']}.json"
    bundle = dict(result)
    bundle["evidence"] = dict(result.get("evidence", {}), bundle_sha256=digest)
    path.write_text(json.dumps(bundle, indent=2, default=str), encoding="utf-8")
    _prune(out_dir, pack.manifest.get("evidence", {}).get("retain_runs", 50))
    try:
        return path.relative_to(pack.repo_root).as_posix()
    except ValueError:
        return str(path)


def _prune(out_dir: Path, keep: int):
    runs = sorted(out_dir.glob("run-*.json"))
    for old in runs[:-keep] if keep and len(runs) > keep else []:
        old.unlink(missing_ok=True)


def find_finding(pack, finding_id: str, out_dir: Path | None = None):
    out_dir = Path(out_dir or pack.root / pack.manifest.get("evidence", {}).get("output_dir", "evidence/runs"))
    for path in sorted(out_dir.glob("run-*.json"), reverse=True):
        try:
            data = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            continue
        for f in data.get("findings", []):
            if f.get("finding_id") == finding_id:
                return f, data
    return None, None
