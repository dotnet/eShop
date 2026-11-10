"""Arch_review_Agent: the architecture review agent other (code) agents call.

It orchestrates the Architecture Policy Pack hook operations and adds agent behaviour on top:
  1. architecture.validate(checkpoint, ...)       - deterministic validators + review requests (no persistence)
  2. semantic reviewer (LLM) answers pending review requests, or they are handed back to the caller
  3. architecture.validate again with judgments   - final decision
  4. architecture.persist(final)                  - evidence bundle + failure history (once per review)
  5. shape an actionable response: prioritized fixes, approved patterns, human-review needs, iteration delta,
     historic recurrence.

It contains no rule IDs, rule logic or validators: every decision comes from the policy pack.
"""
from __future__ import annotations

import datetime as dt
import json
import uuid
from pathlib import Path

import architecture
from architecture import history as history_mod

from .reviewers import INSTRUCTIONS, JUDGMENT_SCHEMA, normalize_judgments, resolve_reviewer

CARD_PATH = Path(__file__).with_name("agent.json")
CARD = json.loads(CARD_PATH.read_text(encoding="utf-8"))
AGENT_ID = CARD["id"]
AGENT_NAME = CARD["name"]
AGENT_VERSION = CARD["version"]
RESULT_SCHEMA = CARD["result_schema"]

ORDER = {"BLOCK": 0, "WARN": 1, "INFO": 2, "WAIVED": 3}
SEVERITY_ORDER = {"critical": 0, "high": 1, "medium": 2, "low": 3}
EXCERPT_CHARS = 8000
EXCERPT_FILES = 6


class AgentRequestError(ValueError):
    pass


class ArchReviewAgent:
    """Callable by any agent in-process; the CLI and MCP server are thin wrappers over these methods."""

    def __init__(self, pack=None, reviewer=None):
        self.pack = pack or architecture.get_pack()
        self._reviewer = reviewer

    # ------------------------------------------------------------------------------------------ operations

    def review(self, request: dict) -> dict:
        req = dict(request or {})
        requester = req.get("requester") or {}
        if isinstance(requester, str):
            requester = {"agent": requester}
        if not requester.get("agent"):
            raise AgentRequestError("requester.agent is required (who is asking for the review)")
        options = dict(req.get("options") or {})
        checkpoint = req.get("checkpoint", "code_review")
        files = dict(req.get("files") or {})
        notes: list[str] = []

        context = dict(req.get("context") or {})
        context.update({k: req[k] for k in ("base", "scope", "rule_ids", "categories") if req.get(k) is not None})
        context["caller"] = f"{AGENT_ID}:{requester['agent']}"
        context["strict"] = bool(options.get("strict", False))
        changed = req.get("changed_files")
        if changed is None and files:
            changed = list(files)
        if changed is not None:
            context["changed_files"] = changed
        persist_ctx = {k: context[k] for k in ("evidence_dir", "history_db") if context.get(k)}
        persist_ctx["write_evidence"] = options.get("write_evidence", True)
        persist_ctx["record_history"] = options.get("record_history", True)
        context.update(write_evidence=False, record_history=False)

        judgments = []
        for j in req.get("semantic_judgments") or []:
            if (j.get("reviewer") or {}).get("id") == requester["agent"]:
                notes.append(f"Rejected self-certified judgment for {j.get('rule_id')}: the requesting agent may not "
                             "judge its own change; an independent reviewer (this agent's LLM reviewer, another agent "
                             "or a human) must answer.")
                continue
            judgments.append(j)
        artifacts = {k: req[k] for k in ("approvals", "test_results", "coverage_reports", "adr_refs") if req.get(k)}
        if files:
            artifacts["files"] = files
        if judgments:
            artifacts["semantic_judgments"] = judgments

        result = architecture.validate(checkpoint, context, artifacts, pack=self.pack)
        judged = {j.get("rule_id") for j in judgments}
        pending = [rr for rr in result["review_requests"] if rr["rule_id"] not in judged]

        reviewer_used = None
        if pending and options.get("semantic", "auto") == "auto":
            reviewer = resolve_reviewer(self._reviewer)
            if reviewer is not None:
                reviewer_used = f"{AGENT_ID}/{reviewer.name}"
                try:
                    raw = reviewer.judge(self._judgment_request(pending, files, req.get("intent")))
                    new, rej = normalize_judgments(raw, {p["rule_id"] for p in pending}, reviewer_used)
                    notes.extend(rej)
                except Exception as exc:  # reviewer failures degrade to "defer", never to a silent pass
                    new = []
                    notes.append(f"Semantic reviewer {reviewer.name} failed ({type(exc).__name__}: {exc}); "
                                 "pending rules returned to the caller.")
                if new:
                    artifacts["semantic_judgments"] = judgments + new
                    result = architecture.validate(checkpoint, context, artifacts, pack=self.pack)
                    judged |= {j["rule_id"] for j in new}
                    pending = [rr for rr in result["review_requests"] if rr["rule_id"] not in judged]

        architecture.persist(result, persist_ctx, pack=self.pack)
        response = self._shape_review(result, requester, req, pending, notes, reviewer_used, options)
        history_mod.record_review(self.pack, response, persist_ctx)
        return response

    def advise(self, request: dict) -> dict:
        req = dict(request or {})
        files = req.get("files") or []
        paths = list(files.keys()) if isinstance(files, dict) else list(files)
        if not paths:
            raise AgentRequestError("files is required (paths the code agent plans to create or change)")
        checkpoint = req.get("checkpoint", "code_review")
        applicable = architecture.get_applicable_rules(
            {"checkpoint": checkpoint, "files": paths, "categories": req.get("categories")}, pack=self.pack)
        rules = sorted(applicable["rules"], key=lambda r: (SEVERITY_ORDER.get(r["severity"], 9), r["id"]))
        max_ex = int(req.get("max_examples_per_pattern", 1))
        pattern_ids = sorted({p for r in rules for p in r["patterns"]})
        patterns = []
        for pid in pattern_ids:
            pat = architecture.get_pattern(pid, pack=self.pack)["pattern"]
            ex = architecture.get_examples(pid, "approved", pack=self.pack) if max_ex else {"examples": [], "exemplars": []}
            patterns.append({"id": pid, "name": pat["name"], "summary": pat["summary"],
                             "approved_examples": ex["examples"][:max_ex],
                             "exemplars": [e for e in ex["exemplars"] if e.get("exists")]})
        hdb = (req.get("context") or {}).get("history_db")
        requester = req.get("requester") or {}
        requester = {"agent": requester} if isinstance(requester, str) else requester
        lessons = None
        if requester.get("agent"):
            full = architecture.get_lessons({"agent": requester["agent"], "history_db": hdb,
                                             "limit": int(req.get("max_lessons", 5))}, pack=self.pack)
            lessons = {k: full[k] for k in ("denial_stats", "lessons", "prompt", "fingerprint")}
        return {
            "schema": "arch-review-advice/v1",
            "agent": {"id": AGENT_ID, "name": AGENT_NAME, "version": AGENT_VERSION},
            "checkpoint": checkpoint,
            "files": paths,
            "rule_count": len(rules),
            "rules": [{k: r[k] for k in ("id", "title", "category", "severity", "mode", "human_review_required",
                                          "patterns", "matched_files")} | {"enforcement": r["checkpoints"].get(checkpoint)}
                      for r in rules],
            "blocking_rules": [r["id"] for r in rules if r["checkpoints"].get(checkpoint) == "block"],
            "semantic_rules": [r["id"] for r in rules if r["mode"] != "deterministic"],
            "human_review_rules": [r["id"] for r in rules if r["human_review_required"]],
            "patterns": patterns,
            "historic_hotspots": history_mod.hotspots(self.pack, paths, history_db=hdb),
            "lessons": lessons,
            "next_step": "Apply `lessons.prompt` (your past denials) and the patterns above, then call review with the "
                         "changed files (and proposed contents in `files` to review before writing to disk).",
        }

    def explain_finding(self, request: dict) -> dict:
        return architecture.explain_finding(request["finding_id"], request.get("context"), pack=self.pack)

    def get_failure_history(self, request: dict) -> dict:
        return architecture.get_failure_history(request.get("query", request), pack=self.pack)

    def get_lessons(self, request: dict) -> dict:
        return architecture.get_lessons(request.get("query", request), pack=self.pack)

    def get_rule(self, request: dict) -> dict:
        return architecture.get_rule(request["rule_id"], pack=self.pack)

    def get_pattern(self, request: dict) -> dict:
        return architecture.get_pattern(request["pattern_id"], pack=self.pack)

    def get_examples(self, request: dict) -> dict:
        return architecture.get_examples(request["pattern_id"], request.get("kind"), pack=self.pack)

    def call(self, operation: str, request: dict | None = None) -> dict:
        if operation not in AGENT_OPERATIONS:
            raise AgentRequestError(f"unknown operation {operation!r}; expected one of {sorted(AGENT_OPERATIONS)}")
        return getattr(self, AGENT_OPERATIONS[operation])(request or {})

    # ------------------------------------------------------------------------------------------ helpers

    def _excerpts(self, paths: list[str], files: dict) -> list[dict]:
        out = []
        for p in paths[:EXCERPT_FILES]:
            text = files.get(p)
            if text is None:
                fp = self.pack.repo_root / p
                try:
                    text = fp.read_text(encoding="utf-8", errors="replace") if fp.is_file() else None
                except OSError:
                    text = None
            if text is not None:
                out.append({"path": p, "truncated": len(text) > EXCERPT_CHARS, "content": text[:EXCERPT_CHARS]})
        return out

    def _approved_examples(self, pattern_ids: list[str]) -> list[dict]:
        out = []
        for pid in pattern_ids:
            try:
                ex = architecture.get_examples(pid, "approved", pack=self.pack)["examples"][:1]
            except architecture.NotFound:
                continue
            pat = self.pack.patterns.get(pid, {})
            out.append({"id": pid, "name": pat.get("name"), "summary": pat.get("summary"), "approved_example": ex[0] if ex else None})
        return out

    def _judgment_request(self, pending: list[dict], files: dict, intent: str | None) -> dict:
        return {
            "instructions": INSTRUCTIONS,
            "intent": intent,
            "response_schema": {"judgments": [JUDGMENT_SCHEMA]},
            "requests": [{
                "rule_id": rr["rule_id"], "title": rr["title"], "rubric": rr.get("rubric"),
                "questions": rr.get("questions", []), "pass_criteria": rr.get("pass_criteria"),
                "files": self._excerpts(rr.get("files", []), files),
                "approved_patterns": self._approved_examples(rr.get("patterns", [])),
            } for rr in pending],
        }

    def _shape_review(self, result: dict, requester: dict, req: dict, pending: list[dict], notes: list[str],
                      reviewer_used: str | None, options: dict) -> dict:
        now = dt.datetime.now(dt.timezone.utc)
        review_id = f"arv-{now.strftime('%Y%m%dT%H%M%S')}-{uuid.uuid4().hex[:8]}"
        actionable = sorted((f for f in result["findings"] if f["outcome"] in ("BLOCK", "WARN")),
                            key=lambda f: (ORDER[f["outcome"]], SEVERITY_ORDER.get(f["severity"], 9), f["rule_id"],
                                           f.get("file") or "", int(f["line"]) if str(f.get("line") or "").isdigit() else 0))
        hdb = (req.get("context") or {}).get("history_db")
        seen = history_mod.finding_history(self.pack, [f["finding_id"] for f in actionable], hdb)
        prior = history_mod.agent_rule_denials(self.pack, requester["agent"], exclude_run_id=result["run_id"], history_db=hdb)
        max_items = int(options.get("max_action_items", 50))
        items = []
        for f in actionable[:max_items]:
            h = seen.get(f["finding_id"])
            p = prior.get(f["rule_id"])
            items.append({
                "finding_id": f["finding_id"], "rule_id": f["rule_id"], "rule_title": f["rule_title"],
                "category": f["category"], "severity": f["severity"], "outcome": f["outcome"], "status": f["status"],
                "validation_mode": f["validation_mode"], "file": f.get("file"), "line": f.get("line"),
                "message": f["message"], "fix": f["remediation"], "patterns": f.get("patterns", []),
                "history": h, "recurring": bool(h and h["occurrences"] >= 2),
                "repeat_mistake": bool(p), "requester_prior_failures": p,
                "explain": {"operation": "explain_finding", "finding_id": f["finding_id"]},
            })
        guidance = {}
        for rid in sorted({i["rule_id"] for i in items}):
            rule = self.pack.rules.get(rid)
            if rule:
                guidance[rid] = {"title": rule["title"], "why": " ".join(rule["rationale"].split()),
                                 "remediation": rule["remediation"],
                                 "patterns": self._approved_examples(rule.get("patterns", []))}
        ex_cfg = self.pack.manifest.get("exceptions", {})
        human = []
        for f in result["findings"]:
            if f["validator"] == "evidence:human_approval" and f["outcome"] in ("BLOCK", "WARN"):
                human.append({"rule_id": f["rule_id"], "title": f["rule_title"], "outcome": f["outcome"],
                              "approver_roles": ex_cfg.get("approver_roles_by_severity", {}).get(f["severity"], []),
                              "how": "Record the approval in `approvals` [{rule_id, approver, role, date, reference}] on the next review call."})
        pending_out = []
        if pending:
            jr = self._judgment_request(pending, dict(req.get("files") or {}), req.get("intent"))
            for item in jr["requests"]:
                item["respond_with"] = dict(JUDGMENT_SCHEMA, reviewer={"type": "agent|human", "id": "independent reviewer id (not the requester)"})
            pending_out = jr["requests"]

        delta = None
        prev = req.get("previous_review_id")
        if prev:
            prev_run = history_mod.run_for_review(self.pack, prev, hdb)
            if prev_run:
                before = {f["finding_id"] for f in history_mod.findings_of_run(self.pack, prev_run, hdb)
                          if f["outcome"] in ("BLOCK", "WARN")}
                now_ids = {f["finding_id"] for f in actionable}
                delta = {"previous_review_id": prev, "previous_run_id": prev_run,
                         "resolved": sorted(before - now_ids), "persisting": sorted(before & now_ids),
                         "introduced": sorted(now_ids - before)}
            else:
                notes.append(f"previous_review_id {prev} not found in history; no delta computed.")

        decision = result["decision"]
        blocks = sum(1 for f in actionable if f["outcome"] == "BLOCK")
        warns = len(actionable) - blocks
        next_actions = []
        if blocks:
            next_actions.append(f"Fix {blocks} BLOCK finding(s) using action_items[].fix and guidance, then call review "
                                f"again with previous_review_id={review_id}.")
        if pending_out:
            next_actions.append(f"Answer {len(pending_out)} semantic review request(s) in pending_judgments via an independent "
                                "reviewer and resubmit them as semantic_judgments.")
        if human:
            next_actions.append("Obtain human approval for: " + ", ".join(h["rule_id"] for h in human) + ".")
        if warns:
            next_actions.append(f"Address or consciously accept {warns} WARN finding(s).")
        if not next_actions:
            next_actions.append("No architecture action required; proceed.")
        recurring = sum(1 for i in items if i["recurring"])
        repeats = sorted({i["rule_id"] for i in items if i["repeat_mistake"]})
        if repeats:
            next_actions.append(f"Repeated past mistake(s) on {', '.join(repeats)}: load your lessons "
                                f"(get_lessons {{\"agent\": \"{requester['agent']}\"}}) into your instructions before retrying.")

        return {
            "schema": RESULT_SCHEMA,
            "review_id": review_id,
            "timestamp": now.isoformat(),
            "agent": AGENT_ID,
            "agent_version": AGENT_VERSION,
            "requester": requester,
            "previous_review_id": prev,
            "status": "awaiting_judgments" if pending_out else "completed",
            "decision": decision,
            "can_proceed": decision != "BLOCK",
            "exit_code": result["exit_code"],
            "summary": (f"{decision}: {blocks} blocking, {warns} warning finding(s); {len(pending_out)} semantic rule(s) "
                        f"pending; {len(human)} human approval(s) needed; {recurring} recurring; "
                        f"{len(repeats)} repeated mistake(s) by {requester['agent']}."),
            "next_actions": next_actions,
            "action_items": items,
            "action_items_truncated": max(0, len(actionable) - max_items),
            "guidance": guidance,
            "pending_judgments": pending_out,
            "human_review": human,
            "waived": [{"finding_id": f["finding_id"], "rule_id": f["rule_id"], "file": f.get("file"),
                        "exception": (f.get("exception") or {}).get("id")} for f in result["findings"] if f["outcome"] == "WAIVED"],
            "exceptions_expiring_soon": result["exceptions"]["expiring_soon"],
            "delta": delta,
            "semantic_reviewer": reviewer_used,
            "notes": notes,
            "run_id": result["run_id"],
            "engine": {"checkpoint": result["checkpoint"], "policy": result["policy"], "summary": result["summary"],
                       "evidence_bundle": result["evidence"].get("bundle_path"), "history": result.get("history"),
                       "duration_ms": result.get("duration_ms")},
        }


AGENT_OPERATIONS = {
    "review": "review",
    "advise": "advise",
    "explain_finding": "explain_finding",
    "get_failure_history": "get_failure_history",
    "get_lessons": "get_lessons",
    "get_rule": "get_rule",
    "get_pattern": "get_pattern",
    "get_examples": "get_examples",
}
