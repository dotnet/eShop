"""Deterministic validators over test evidence (TRX / JUnit results, Cobertura coverage)."""
from __future__ import annotations

import xml.etree.ElementTree as ET

from _vcommon import fail


def _local(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def _parse(ctx, path):
    text = ctx.read(path)
    if text is None:
        return None
    return ET.fromstring(text.lstrip("\ufeff"))


def test_results(ctx) -> list[dict]:
    """Every supplied test-result file must report >0 executed tests and zero failures/errors."""
    results = []
    for path in ctx.evidence_paths("test_results"):
        try:
            root = _parse(ctx, path)
        except ET.ParseError as exc:
            results.append(fail(path, None, None, f"Unreadable test results: {exc}"))
            continue
        if root is None:
            continue
        total = failed = 0
        if _local(root.tag) == "TestRun":
            counters = next((e for e in root.iter() if _local(e.tag) == "Counters"), None)
            if counters is not None:
                total = int(counters.get("executed", counters.get("total", 0)))
                failed = int(counters.get("failed", 0)) + int(counters.get("error", 0)) + int(counters.get("timeout", 0)) + int(counters.get("aborted", 0))
        else:  # JUnit
            suites = [root] if _local(root.tag) == "testsuite" else [e for e in root if _local(e.tag) == "testsuite"]
            for s in suites:
                total += int(s.get("tests", 0)) - int(s.get("skipped", 0))
                failed += int(s.get("failures", 0)) + int(s.get("errors", 0))
        if total == 0:
            results.append(fail(path, None, None, "Test results report zero executed tests."))
        elif failed:
            results.append(fail(path, None, None, f"{failed} of {total} executed tests failed."))
    return results


def coverage_threshold(ctx) -> list[dict]:
    """Line coverage of changed projects (or overall) must meet params.min_line_rate."""
    minimum = float(ctx.params.get("min_line_rate", 0.7))
    changed_projects = set()
    for path in ctx.changed:
        proj = ctx.index.project_for(path)
        if proj and proj["type"] not in ("test", "test_tooling"):
            changed_projects.add(proj["name"])
    results = []
    for path in ctx.evidence_paths("coverage_report"):
        try:
            root = _parse(ctx, path)
        except ET.ParseError as exc:
            results.append(fail(path, None, None, f"Unreadable coverage report: {exc}"))
            continue
        if root is None:
            continue
        packages = {p.get("name"): float(p.get("line-rate", 0)) for p in root.iter() if _local(p.tag) == "package"}
        relevant = {n: r for n, r in packages.items() if n in changed_projects}
        if relevant:
            for name, rate in sorted(relevant.items()):
                if rate < minimum:
                    results.append(fail(path, None, None, f"Line coverage for {name} is {rate:.0%}, below {minimum:.0%}."))
        else:
            rate = float(root.get("line-rate", 0))
            if rate < minimum:
                results.append(fail(path, None, None, f"Overall line coverage is {rate:.0%}, below {minimum:.0%}."))
    return results
