"""Deterministic text validators: forbidden and required source patterns."""
from __future__ import annotations

from _vcommon import compile_regex, fail, is_comment, match_any


def forbidden_pattern(ctx) -> list[dict]:
    """Fail on any match of a forbidden regex.

    params:
      patterns: [{regex, message, flags?: [i|m|s]}]
      file_globs: optional narrower globs within rule applicability
      allow_files: globs where the pattern is the approved implementation (e.g. the outbox service)
      ignore_comments: skip matches on comment lines (default true)
      max_per_file: cap findings per file (default 20)
    """
    p = ctx.params
    compiled = [(compile_regex(x["regex"], x.get("flags")), x["message"]) for x in p["patterns"]]
    results = []
    for path in ctx.files:
        if p.get("file_globs") and not match_any(path, p["file_globs"]):
            continue
        if match_any(path, p.get("allow_files")):
            continue
        text = ctx.read(path)
        if not text:
            continue
        hits = 0
        for regex, message in compiled:
            for m in regex.finditer(text):
                if p.get("ignore_comments", True) and is_comment(text, m.start()):
                    continue
                results.append(fail(path, text, m.start(), message))
                hits += 1
                if hits >= p.get("max_per_file", 20):
                    break
    return results


def required_pattern(ctx) -> list[dict]:
    """Require patterns in files (or their project) that match a trigger.

    params:
      file_globs: files to inspect (within rule applicability)
      trigger: optional regex; only files containing it are checked
      require: [{regex, message}] - every entry must match
      search_scope: file | project (project = any file of the owning project matching project_globs)
      project_globs: default ['**/*.cs']
    """
    p = ctx.params
    trigger = compile_regex(p["trigger"], p.get("trigger_flags")) if p.get("trigger") else None
    requirements = [(compile_regex(r["regex"], r.get("flags")), r["message"]) for r in p["require"]]
    scope = p.get("search_scope", "file")
    results, reported = [], set()
    for path in ctx.files:
        if p.get("file_globs") and not match_any(path, p["file_globs"]):
            continue
        text = ctx.read(path)
        if text is None:
            continue
        tm = trigger.search(text) if trigger else None
        if trigger and not tm:
            continue
        if scope == "project":
            project = ctx.index.project_for(path)
            if project is None:
                continue
            corpus_files = [f for f in ctx.index.project_files(ctx.all_files(), project)
                            if match_any(f, p.get("project_globs", ["**/*.cs"]))]
            corpus = "\n".join(ctx.read(f) or "" for f in corpus_files)
            if path not in corpus_files:
                corpus += "\n" + text
            key_base = project["name"]
        else:
            corpus, key_base = text, path
        for regex, message in requirements:
            if regex.search(corpus):
                continue
            key = (key_base, regex.pattern)
            if key in reported:
                continue
            reported.add(key)
            results.append(fail(path, text, tm.start() if tm else None, message))
    return results
