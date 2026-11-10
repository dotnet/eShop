"""Minimal stdio MCP server for Arch_review_Agent (JSON-RPC 2.0, newline-delimited; no third-party dependencies).

Tools are generated from agent.json, so the MCP surface, the CLI and the Python API share one contract:
    arch_review, arch_advise, arch_explain_finding, arch_get_failure_history, arch_get_lessons, arch_get_rule,
    arch_get_pattern, arch_get_examples
"""
from __future__ import annotations

import json
import sys
import traceback

from .agent import AGENT_NAME, AGENT_OPERATIONS, AGENT_VERSION, CARD, ArchReviewAgent

PROTOCOL_VERSION = "2025-06-18"
TOOL_PREFIX = "arch_"


def tools() -> list[dict]:
    return [{"name": TOOL_PREFIX + op["name"], "title": f"{AGENT_NAME}: {op['name']}", "description": op["description"],
             "inputSchema": op["input_schema"]} for op in CARD["operations"]]


class Server:
    def __init__(self, agent: ArchReviewAgent | None = None):
        self._agent = agent

    @property
    def agent(self) -> ArchReviewAgent:
        if self._agent is None:
            self._agent = ArchReviewAgent()
        return self._agent

    def handle(self, msg: dict) -> dict | None:
        method, mid = msg.get("method"), msg.get("id")
        if mid is None:  # notification (e.g. notifications/initialized)
            return None
        try:
            if method == "initialize":
                client = (msg.get("params") or {}).get("protocolVersion")
                result = {"protocolVersion": client or PROTOCOL_VERSION,
                          "capabilities": {"tools": {"listChanged": False}},
                          "serverInfo": {"name": CARD["id"], "title": AGENT_NAME, "version": AGENT_VERSION},
                          "instructions": CARD["description"]}
            elif method == "ping":
                result = {}
            elif method == "tools/list":
                result = {"tools": tools()}
            elif method == "tools/call":
                params = msg.get("params") or {}
                name = params.get("name", "")
                op = name[len(TOOL_PREFIX):] if name.startswith(TOOL_PREFIX) else name
                if op not in AGENT_OPERATIONS:
                    return _error(mid, -32602, f"unknown tool {name!r}")
                try:
                    out = self.agent.call(op, params.get("arguments") or {})
                    result = {"content": [{"type": "text", "text": json.dumps(out, indent=2, default=str)}],
                              "structuredContent": out, "isError": False}
                except Exception as exc:
                    result = {"content": [{"type": "text", "text": f"{type(exc).__name__}: {exc}"}], "isError": True}
            else:
                return _error(mid, -32601, f"method not found: {method}")
            return {"jsonrpc": "2.0", "id": mid, "result": result}
        except Exception as exc:  # pragma: no cover - defensive
            traceback.print_exc(file=sys.stderr)
            return _error(mid, -32603, f"{type(exc).__name__}: {exc}")


def _error(mid, code: int, message: str) -> dict:
    return {"jsonrpc": "2.0", "id": mid, "error": {"code": code, "message": message}}


def serve(stdin=None, stdout=None) -> int:
    stdin, stdout = stdin or sys.stdin, stdout or sys.stdout
    server = Server()
    for line in stdin:
        line = line.strip()
        if not line:
            continue
        try:
            msg = json.loads(line)
        except json.JSONDecodeError as exc:
            reply = _error(None, -32700, f"parse error: {exc}")
        else:
            reply = server.handle(msg) if isinstance(msg, dict) else _error(None, -32600, "invalid request")
        if reply is not None:
            stdout.write(json.dumps(reply, default=str) + "\n")
            stdout.flush()
    return 0
