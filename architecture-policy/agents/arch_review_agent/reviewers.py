"""Semantic reviewer backends for Arch_review_Agent.

Semantic/hybrid rules need architectural judgment. The policy pack's semantic validators never call an LLM; they emit
review requests. Arch_review_Agent answers them with a pluggable reviewer:

* None (default)   - defer: return the rubric to the caller as pending_judgments.
* CommandReviewer  - $ARCH_REVIEW_LLM_CMD reads the judgment request JSON on stdin, prints {"judgments": [...]}.
* CallableReviewer - any Python callable(request: dict) -> list[dict] (used by in-process agents and tests).
"""
from __future__ import annotations

import json
import os
import re
import shlex
import subprocess
from typing import Callable, Protocol

VERDICTS = ("pass", "fail", "not_applicable")

INSTRUCTIONS = (
    "You are the semantic reviewer of Arch_review_Agent. For each request, judge ONLY the given rule against its rubric, "
    "questions and pass_criteria, using the file excerpts and approved patterns provided. Be strict: if the rubric is "
    "not clearly satisfied, answer 'fail' with concrete findings (file, line, message). Use 'not_applicable' only when "
    "the change does not touch what the rule governs. Respond with JSON only: {\"judgments\": [<judgment>, ...]} with "
    "one judgment per request, shaped like response_schema."
)

JUDGMENT_SCHEMA = {
    "rule_id": "RULE-ID from the request",
    "verdict": "pass|fail|not_applicable",
    "rationale": "1-3 sentences grounded in the code",
    "findings": [{"file": "path", "line": 0, "message": "what violates the rubric and how to fix it"}],
}


class Reviewer(Protocol):
    name: str

    def judge(self, request: dict) -> list[dict]: ...


class CallableReviewer:
    def __init__(self, fn: Callable[[dict], list], name: str = "callable"):
        self.fn = fn
        self.name = name

    def judge(self, request: dict) -> list[dict]:
        return list(self.fn(request) or [])


class CommandReviewer:
    def __init__(self, command: str, timeout: float = 180, name: str = "command"):
        self.command = command
        self.timeout = timeout
        self.name = name

    def judge(self, request: dict) -> list[dict]:
        args = shlex.split(self.command, posix=os.name != "nt")
        proc = subprocess.run(args, input=json.dumps(request), capture_output=True, text=True,
                              encoding="utf-8", timeout=self.timeout)
        if proc.returncode != 0:
            raise RuntimeError(f"reviewer command exited {proc.returncode}: {proc.stderr.strip()[:500]}")
        return parse_judgments(proc.stdout)


def resolve_reviewer(reviewer=None) -> Reviewer | None:
    if reviewer is None:
        cmd = os.environ.get("ARCH_REVIEW_LLM_CMD")
        return CommandReviewer(cmd, float(os.environ.get("ARCH_REVIEW_LLM_TIMEOUT", "180"))) if cmd else None
    if callable(reviewer) and not hasattr(reviewer, "judge"):
        return CallableReviewer(reviewer)
    return reviewer


def parse_judgments(text: str) -> list[dict]:
    """Accept a JSON list, {"judgments": [...]}, or prose with an embedded JSON block (typical LLM CLI output)."""
    candidates = [text.strip()]
    candidates += re.findall(r"```(?:json)?\s*(.*?)```", text, flags=re.S)
    m = re.search(r"(\{.*\}|\[.*\])", text, flags=re.S)
    if m:
        candidates.append(m.group(1))
    for c in candidates:
        try:
            data = json.loads(c)
        except (json.JSONDecodeError, TypeError):
            continue
        if isinstance(data, dict):
            data = data.get("judgments", [])
        if isinstance(data, list):
            return [j for j in data if isinstance(j, dict)]
    raise ValueError("reviewer output contained no judgments JSON")


def normalize_judgments(raw: list[dict], allowed_rule_ids: set[str], reviewer_id: str) -> tuple[list[dict], list[str]]:
    """Keep well-formed judgments for requested rules; stamp the reviewer identity. Returns (valid, rejection notes)."""
    valid, notes = [], []
    for j in raw:
        rid, verdict = j.get("rule_id"), str(j.get("verdict", "")).lower()
        if rid not in allowed_rule_ids:
            notes.append(f"ignored judgment for unrequested rule {rid!r}")
            continue
        if verdict not in VERDICTS:
            notes.append(f"ignored judgment for {rid}: invalid verdict {j.get('verdict')!r}")
            continue
        findings = [f for f in (j.get("findings") or []) if isinstance(f, dict) and f.get("message")]
        valid.append({"rule_id": rid, "verdict": verdict, "rationale": str(j.get("rationale", ""))[:2000],
                      "reviewer": {"type": "agent", "id": reviewer_id}, "findings": findings})
    return valid, notes
