"""Arch_review_Agent - architecture review agent callable by any code agent.

    import sys; sys.path.insert(0, "architecture-policy/agents")
    from arch_review_agent import ArchReviewAgent
    agent = ArchReviewAgent()                       # optional: reviewer=<callable|Reviewer> for semantic rules
    agent.advise({"files": ["src/Ordering.API/Apis/OrdersApi.cs"]})
    agent.review({"requester": {"agent": "code-agent", "task_id": "T-42"},
                  "files": {"src/Ordering.API/Apis/OrdersApi.cs": "<proposed content>"}})

Also exposed as a JSON CLI and a stdio MCP server: architecture-policy/agents/arch_review.py.
Contract: agent.json (operations + input schemas).
"""
import sys
from pathlib import Path

_HOOKS = Path(__file__).resolve().parents[2] / "hooks"
if str(_HOOKS) not in sys.path:
    sys.path.insert(0, str(_HOOKS))

from .agent import (AGENT_ID, AGENT_NAME, AGENT_OPERATIONS, AGENT_VERSION, CARD, AgentRequestError,  # noqa: E402
                    ArchReviewAgent)
from .reviewers import CallableReviewer, CommandReviewer, Reviewer  # noqa: E402

__all__ = ["ArchReviewAgent", "AgentRequestError", "AGENT_ID", "AGENT_NAME", "AGENT_VERSION", "AGENT_OPERATIONS",
           "CARD", "Reviewer", "CallableReviewer", "CommandReviewer", "review", "advise"]


def review(request: dict, reviewer=None) -> dict:
    return ArchReviewAgent(reviewer=reviewer).review(request)


def advise(request: dict) -> dict:
    return ArchReviewAgent().advise(request)
