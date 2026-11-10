#!/usr/bin/env python3
"""Entry point for Arch_review_Agent (callable by any code agent).

    python architecture-policy/agents/arch_review.py review '{"requester": {"agent": "code-agent"}, "base": "origin/main"}'
    python architecture-policy/agents/arch_review.py advise '{"files": ["src/Ordering.API/Apis/OrdersApi.cs"]}'
    python architecture-policy/agents/arch_review.py review request.json      # or '-' for stdin
    python architecture-policy/agents/arch_review.py mcp                      # stdio MCP server
    python architecture-policy/agents/arch_review.py card                     # agent.json contract

JSON in, JSON out. Exit codes: 0 PASS/WARN, 1 BLOCK, 2 error.
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
for stream in (sys.stdout, sys.stdin):
    if hasattr(stream, "reconfigure"):
        stream.reconfigure(encoding="utf-8")

from arch_review_agent import AGENT_OPERATIONS, CARD, ArchReviewAgent  # noqa: E402


def _load(value: str):
    if value == "-":
        return json.load(sys.stdin)
    p = Path(value)
    if len(value) < 260 and p.is_file():
        return json.loads(p.read_text(encoding="utf-8"))
    return json.loads(value)


def main(argv: list[str]) -> int:
    usage = f"usage: arch_review.py ({'|'.join(sorted(AGENT_OPERATIONS))}) [request-json|file|-] | mcp | card"
    if not argv or argv[0] in ("-h", "--help"):
        print(usage)
        return 0 if argv else 2
    op = argv[0]
    if op == "mcp":
        from arch_review_agent.mcp_server import serve
        return serve()
    if op == "card":
        print(json.dumps(CARD, indent=2))
        return 0
    try:
        request = _load(argv[1]) if len(argv) > 1 else {}
        out = ArchReviewAgent().call(op, request)
    except Exception as exc:
        print(json.dumps({"error": type(exc).__name__, "message": str(exc), "usage": usage}), file=sys.stderr)
        return 2
    print(json.dumps(out, indent=2, default=str))
    return 1 if op == "review" and out.get("decision") == "BLOCK" else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
