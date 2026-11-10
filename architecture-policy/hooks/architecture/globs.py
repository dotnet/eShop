"""Repository-relative glob matching with '**' support (POSIX separators)."""
from __future__ import annotations

import functools
import re


@functools.lru_cache(maxsize=4096)
def compile_glob(glob: str) -> re.Pattern[str]:
    i, out = 0, []
    while i < len(glob):
        c = glob[i]
        if c == "*":
            if glob.startswith("**/", i):
                out.append("(?:.*/)?")
                i += 3
                continue
            if glob.startswith("**", i):
                out.append(".*")
                i += 2
                continue
            out.append("[^/]*")
        elif c == "?":
            out.append("[^/]")
        else:
            out.append(re.escape(c))
        i += 1
    return re.compile("^" + "".join(out) + "$")


def match(path: str, glob: str) -> bool:
    return bool(compile_glob(glob).match(normalize(path)))


def match_any(path: str, globs) -> bool:
    return any(match(path, g) for g in (globs or []))


def normalize(path: str) -> str:
    p = str(path).replace("\\", "/")
    while p.startswith("./"):
        p = p[2:]
    return p
