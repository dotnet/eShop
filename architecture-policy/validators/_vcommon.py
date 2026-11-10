"""Helpers shared by validator implementations (kept free of engine imports)."""
from __future__ import annotations

import re
import sys
from pathlib import Path

# Validators reuse the engine's glob semantics so applicability and params agree.
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "hooks"))
from architecture.globs import match, match_any, normalize  # noqa: E402,F401

_FLAG_MAP = {"i": re.IGNORECASE, "m": re.MULTILINE, "s": re.DOTALL}


def compile_regex(spec: str, flags: str | list | None = None) -> re.Pattern[str]:
    value = 0
    for f in (flags or []):
        value |= _FLAG_MAP[f]
    return re.compile(spec, value)


def line_of(text: str, offset: int) -> int:
    return text.count("\n", 0, offset) + 1


def line_text(text: str, line: int) -> str:
    lines = text.splitlines()
    return lines[line - 1].strip()[:240] if 0 < line <= len(lines) else ""


def is_comment(text: str, offset: int) -> bool:
    start = text.rfind("\n", 0, offset) + 1
    prefix = text[start:offset].lstrip()
    return prefix.startswith("//") or prefix.startswith("*") or prefix.startswith("/*")


def fail(path: str, text: str | None, offset: int | None, message: str) -> dict:
    res = {"status": "fail", "file": path, "message": message}
    if text is not None and offset is not None:
        ln = line_of(text, offset)
        res["line"] = ln
        res["snippet"] = line_text(text, ln)
    return res
