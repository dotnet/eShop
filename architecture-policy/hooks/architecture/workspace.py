"""File access abstraction: git worktree, git index (staged), or in-memory files.

Validators never touch the filesystem or git directly; they go through a Workspace so the
same validator code runs for pre-commit (staged blobs), PR review, CI, agent-proposed
content (overlay), and validator self-tests (memory).
"""
from __future__ import annotations

import os
import subprocess
from pathlib import Path

from .globs import match_any, normalize


class Workspace:
    def __init__(self, root: Path, source: str = "worktree", overlay: dict | None = None,
                 memory: dict | None = None, exclude: list | None = None):
        if source not in ("worktree", "index", "memory"):
            raise ValueError(f"unknown workspace source {source!r}")
        self.root = Path(root)
        self.source = source
        self.overlay = {normalize(k): v for k, v in (overlay or {}).items()}
        self.memory = {normalize(k): v for k, v in (memory or {}).items()}
        self.exclude = list(exclude or [])
        self._files: list[str] | None = None
        self._cache: dict[str, str | None] = {}

    # -- git -----------------------------------------------------------------
    def git(self, *args: str) -> str | None:
        if self.source == "memory":
            return None
        try:
            res = subprocess.run(["git", *args], cwd=self.root, capture_output=True,
                                 text=True, encoding="utf-8", errors="replace", timeout=60)
        except (OSError, subprocess.SubprocessError):
            return None
        return res.stdout if res.returncode == 0 else None

    def head(self) -> str | None:
        out = self.git("rev-parse", "HEAD")
        return out.strip() if out else None

    def branch(self) -> str | None:
        out = self.git("rev-parse", "--abbrev-ref", "HEAD")
        return out.strip() if out else None

    # -- listing ---------------------------------------------------------------
    def list_files(self) -> list[str]:
        if self._files is None:
            if self.source == "memory":
                files = set(self.memory) | set(self.overlay)
            else:
                args = ["ls-files", "-z", "--cached"]
                if self.source == "worktree":
                    args += ["--others", "--exclude-standard"]
                out = self.git(*args)
                if out is None:
                    files = {normalize(os.path.relpath(os.path.join(d, f), self.root))
                             for d, _, fs in os.walk(self.root) for f in fs}
                else:
                    files = {normalize(p) for p in out.split("\0") if p}
                files |= set(self.overlay)
            self._files = sorted(p for p in files if not match_any(p, self.exclude))
        return self._files

    def exists(self, path: str) -> bool:
        path = normalize(path)
        if path in self.overlay or path in self.memory:
            return True
        if self.source == "memory":
            return False
        return (self.root / path).is_file()

    def read(self, path: str) -> str | None:
        path = normalize(path)
        if path in self._cache:
            return self._cache[path]
        if path in self.overlay:
            text = self.overlay[path]
        elif self.source == "memory":
            text = self.memory.get(path)
        elif self.source == "index":
            text = self.git("show", f":{path}")
            if text is None:
                text = self._read_disk(path)
        else:
            text = self._read_disk(path)
        self._cache[path] = text
        return text

    def _read_disk(self, path: str) -> str | None:
        p = self.root / path
        try:
            return p.read_text(encoding="utf-8", errors="replace")
        except OSError:
            return None

    # -- change detection --------------------------------------------------------
    def changed_files(self, checkpoint: str, base: str | None = None) -> dict[str, str]:
        """Return {path: status} with status in A/M/D/R (R = new path of a rename)."""
        if self.source == "memory":
            return {}
        if checkpoint == "pre_commit":
            out = self.git("diff", "--cached", "--name-status", "-M", "-z")
            return self._parse_name_status(out)
        merge_base = None
        candidates = [base] if base else ["origin/main", "main"]
        for ref in candidates:
            if not ref:
                continue
            mb = self.git("merge-base", ref, "HEAD")
            if mb:
                merge_base = mb.strip()
                break
        if not merge_base:
            merge_base = "HEAD"
        changed = self._parse_name_status(self.git("diff", "--name-status", "-M", "-z", merge_base))
        untracked = self.git("ls-files", "-z", "--others", "--exclude-standard") or ""
        for p in untracked.split("\0"):
            if p:
                changed.setdefault(normalize(p), "A")
        return {p: s for p, s in changed.items() if not match_any(p, self.exclude)}

    def _parse_name_status(self, out: str | None) -> dict[str, str]:
        result: dict[str, str] = {}
        if not out:
            return result
        parts = [p for p in out.split("\0") if p != ""]
        i = 0
        while i < len(parts):
            status = parts[i]
            code = status[0]
            if code in ("R", "C"):
                old, new = normalize(parts[i + 1]), normalize(parts[i + 2])
                if code == "R":
                    result[old] = "D"
                result[new] = "R" if code == "R" else "A"
                i += 3
            else:
                result[normalize(parts[i + 1])] = code if code in "AMD" else "M"
                i += 2
        return {p: s for p, s in result.items() if not match_any(p, self.exclude)}
