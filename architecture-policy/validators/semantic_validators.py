"""Semantic validator: turns architectural-judgment rules into review requests and
consumes recorded judgments from the Review Agent, Test Agent, or a human reviewer.

The engine never calls an LLM itself. Callers that can exercise judgment answer the
emitted review_requests by passing artifacts.semantic_judgments on a subsequent call.
"""
from __future__ import annotations


def semantic_review(ctx) -> list[dict]:
    judgments = ctx.judgments()
    if not judgments:
        return [{"status": "needs_review",
                 "message": f"Semantic review required for {ctx.rule['id']} ({ctx.rule['title']}) "
                            f"on {len(ctx.files)} file(s); answer the review request with a semantic_judgment."}]
    results = []
    for j in judgments:
        if j.get("verdict") != "fail":
            continue
        reviewer = j.get("reviewer", {})
        who = f"{reviewer.get('type', 'reviewer')}:{reviewer.get('id', 'unknown')}"
        items = j.get("findings") or [{"message": j.get("rationale", "Semantic review failed.")}]
        for item in items:
            results.append({"status": "fail", "file": item.get("file"), "line": item.get("line"),
                            "message": f"[{who}] {item.get('message') or j.get('rationale', '')}".strip()})
    unknown = [j for j in judgments if j.get("verdict") not in ("pass", "fail", "not_applicable")]
    if unknown and not results:
        results.append({"status": "needs_review", "message": "Semantic judgment present but verdict is not pass|fail|not_applicable."})
    return results
