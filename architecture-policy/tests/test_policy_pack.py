"""Tests for the Architecture Policy Pack hooks. Run: python -m unittest discover architecture-policy/tests"""
import json
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

HOOKS = Path(__file__).resolve().parents[1] / "hooks"
AGENTS = Path(__file__).resolve().parents[1] / "agents"
sys.path.insert(0, str(HOOKS))
sys.path.insert(0, str(AGENTS))

# Never write test runs into the developer's real failure history.
_TMP = tempfile.TemporaryDirectory()
os.environ["ARCH_HISTORY_DB"] = str(Path(_TMP.name) / "history.sqlite")
os.environ.pop("ARCH_HISTORY", None)
os.environ.pop("ARCH_REVIEW_LLM_CMD", None)

import architecture  # noqa: E402
from architecture import NotFound  # noqa: E402
from arch_review_agent import ArchReviewAgent  # noqa: E402

PACK = architecture.get_pack()
API_FILE = "src/Catalog.API/Apis/CatalogApi.cs"
ANON_WRITE = 'api.MapPost("/items", CreateItem);'
SECRET_FILE = "src/Basket.API/Program.cs"
SECRET = 'var pw = "Password=SuperS3cretValue!;";'


def run(checkpoint, files, today="2026-11-09", artifacts=None, **ctx):
    context = {"memory_files": files, "changed_files": list(files), "today": today,
               "write_evidence": False, "record_history": False, "caller": "unittest", **ctx}
    return architecture.validate(checkpoint, context, artifacts or {}, pack=PACK)


def fresh_db():
    fd, path = tempfile.mkstemp(suffix=".sqlite", dir=_TMP.name)
    os.close(fd)
    os.remove(path)
    return path


def outcomes(result, rule_id):
    return [f["outcome"] for f in result["findings"] if f["rule_id"] == rule_id]


class DecisionTests(unittest.TestCase):
    def test_clean_change_passes_pre_commit(self):
        r = run("pre_commit", {"src/Catalog.API/Model/Thing.cs": "namespace Catalog; public record Thing(int Id);"})
        self.assertEqual(r["decision"], "PASS")
        self.assertEqual(r["exit_code"], 0)

    def test_block_dominates(self):
        r = run("pre_commit", {"src/Basket.API/Program.cs": 'var pw = "Password=SuperS3cretValue!;";\nConsole.WriteLine(pw);'})
        self.assertEqual(r["decision"], "BLOCK")
        self.assertEqual(r["exit_code"], 1)
        self.assertIn("BLOCK", outcomes(r, "SEC-001"))

    def test_active_exception_waives_and_expired_exception_enforces(self):
        active = run("pre_commit", {API_FILE: ANON_WRITE}, rule_ids=["API-002"])
        self.assertEqual(outcomes(active, "API-002"), ["WAIVED"])
        self.assertEqual(active["decision"], "PASS")
        self.assertIn("EXC-0001", [e if isinstance(e, str) else e["id"] for e in active["exceptions"]["applied"]])

        expired = run("pre_commit", {API_FILE: ANON_WRITE}, today="2027-03-01", rule_ids=["API-002"])
        self.assertEqual(expired["decision"], "BLOCK")
        self.assertIn("BLOCK", outcomes(expired, "API-002"))

    def test_exception_does_not_leak_to_other_services(self):
        r = run("pre_commit", {"src/Ordering.API/Apis/OrdersApi.cs": ANON_WRITE}, rule_ids=["API-002"])
        self.assertEqual(r["decision"], "BLOCK")

    def test_strict_turns_warn_into_nonzero(self):
        files = {"src/Basket.API/Grpc/BasketService.cs": 'Console.WriteLine("basket updated");'}
        r = run("pre_commit", files, rule_ids=["OBS-002"])
        self.assertEqual(r["decision"], "WARN")
        self.assertEqual(r["exit_code"], 0)
        self.assertEqual(run("pre_commit", files, rule_ids=["OBS-002"], strict=True)["exit_code"], 3)


class CheckpointTests(unittest.TestCase):
    def test_pre_commit_never_runs_semantic_validators(self):
        r = run("pre_commit", {API_FILE: ANON_WRITE})
        self.assertEqual(r["review_requests"], [])
        semantic = {f["rule_id"] for f in r["findings"] if f["validation_mode"] == "semantic"}
        self.assertEqual(semantic, set())

    def test_code_review_emits_review_requests_and_accepts_judgments(self):
        files = {API_FILE: "namespace Catalog; public static class CatalogApi { }"}
        first = run("code_review", files)
        self.assertTrue(first["review_requests"], "semantic rules should request review at code_review")
        req = first["review_requests"][0]
        self.assertIn("rubric", req)
        rid = req["rule_id"]

        passed = run("code_review", files, rule_ids=[rid], artifacts={
            "semantic_judgments": [{"rule_id": rid, "verdict": "pass", "rationale": "ok",
                                    "reviewer": {"type": "agent", "id": "review-agent"}}],
            "approvals": [{"rule_id": rid, "approver": "jane", "role": "architect", "date": "2026-11-09"}]})
        self.assertNotIn("WARN", outcomes(passed, rid))
        self.assertNotIn("BLOCK", outcomes(passed, rid))

        failed = run("code_review", files, rule_ids=[rid], artifacts={
            "semantic_judgments": [{"rule_id": rid, "verdict": "fail", "rationale": "violates rubric",
                                    "reviewer": {"type": "agent", "id": "review-agent"},
                                    "findings": [{"file": API_FILE, "line": 1, "message": "bad"}]}]})
        self.assertTrue(set(outcomes(failed, rid)) & {"WARN", "BLOCK"})

    def test_testing_without_test_results_blocks(self):
        r = run("testing", {API_FILE: "namespace Catalog;"}, rule_ids=["TEST-002"])
        self.assertEqual(r["decision"], "BLOCK")
        self.assertTrue(any(f["status"] == "missing_evidence" for f in r["findings"]))

    def test_unknown_checkpoint_rejected(self):
        with self.assertRaises(ValueError):
            architecture.validate("deploy", {}, {}, pack=PACK)


class OperationTests(unittest.TestCase):
    def test_finding_ids_are_stable(self):
        files = {"src/Basket.API/Program.cs": 'var pw = "Password=SuperS3cretValue!;";'}
        a = [f["finding_id"] for f in run("pre_commit", files)["findings"]]
        b = [f["finding_id"] for f in run("pre_commit", files)["findings"]]
        self.assertTrue(a)
        self.assertEqual(a, b)

    def test_explain_finding_falls_back_to_rule(self):
        fid = run("pre_commit", {"src/Basket.API/Program.cs": 'var pw = "Password=SuperS3cretValue!;";'})["findings"][0]["finding_id"]
        exp = architecture.explain_finding(fid, pack=PACK)
        self.assertEqual(exp["rule"]["id"], fid.rsplit("-", 1)[0])
        self.assertIn("steps", exp["how_to_fix"])
        with self.assertRaises(NotFound):
            architecture.explain_finding("NOPE-999-abc", pack=PACK)

    def test_get_operations(self):
        self.assertEqual(architecture.get_rule("SEC-001", pack=PACK)["rule"]["id"], "SEC-001")
        pid = PACK.rules["DATA-003"]["patterns"][0]
        self.assertEqual(architecture.get_pattern(pid, pack=PACK)["pattern"]["id"], pid)
        ex = architecture.get_examples(pid, "approved", pack=PACK)["examples"]
        self.assertTrue(ex and all(e["kind"] == "approved" for e in ex))
        with self.assertRaises(NotFound):
            architecture.get_rule("XYZ-001", pack=PACK)

    def test_get_applicable_rules_filters_by_file_and_checkpoint(self):
        r = architecture.get_applicable_rules({"checkpoint": "pre_commit", "files": [API_FILE]}, pack=PACK)
        ids = {x["id"] for x in r["rules"]}
        self.assertIn("API-002", ids)
        self.assertTrue(all(x["checkpoints"]["pre_commit"] != "off" for x in r["rules"]))
        none = architecture.get_applicable_rules({"files": ["README.md"], "categories": ["data"]}, pack=PACK)
        self.assertEqual(none["count"], 0)

    def test_operations_contract_matches_engine(self):
        contract = json.loads((HOOKS / "operations.json").read_text(encoding="utf-8"))
        self.assertEqual({o["name"] for o in contract["operations"]}, set(architecture.OPERATIONS))

    def test_cli_call_is_same_engine(self):
        out = subprocess.run([sys.executable, str(HOOKS / "arch.py"), "call", "get_rule", '{"rule_id": "ARCH-001"}'],
                             capture_output=True, text=True, encoding="utf-8")
        self.assertEqual(out.returncode, 0, out.stderr)
        self.assertEqual(json.loads(out.stdout)["rule"]["id"], "ARCH-001")


class HistoryTests(unittest.TestCase):
    def test_failures_are_recorded_and_recurring_findings_reported(self):
        db = fresh_db()
        for _ in range(2):
            r = run("pre_commit", {SECRET_FILE: SECRET}, record_history=True, history_db=db)
            self.assertTrue(r["history"]["recorded"], r["history"])
        run("pre_commit", {"src/Catalog.API/Model/Thing.cs": "public record Thing(int Id);"}, record_history=True, history_db=db)
        h = architecture.get_failure_history({"history_db": db, "since_days": 1}, pack=PACK)
        self.assertEqual((h["totals"]["runs"], h["totals"]["block_runs"], h["totals"]["pass_runs"]), (3, 2, 1))
        self.assertIn("SEC-001", [r["rule_id"] for r in h["top_rules"]])
        rec = [x for x in h["recurring_findings"] if x["rule_id"] == "SEC-001"]
        self.assertTrue(rec and rec[0]["occurrences"] == 2)
        self.assertEqual(rec[0]["callers"], ["unittest"])
        explained = architecture.explain_finding(rec[0]["finding_id"], {"history_db": db}, pack=PACK)
        self.assertEqual(explained["history"]["occurrences"], 2)

    def test_pending_semantic_review_is_not_counted_as_failure_by_default(self):
        db = fresh_db()
        run("code_review", {API_FILE: "namespace Catalog; public static class CatalogApi { }"}, record_history=True, history_db=db)
        default = architecture.get_failure_history({"history_db": db, "min_occurrences": 1}, pack=PACK)
        pending = architecture.get_failure_history({"history_db": db, "min_occurrences": 1, "include_pending_review": True}, pack=PACK)
        self.assertGreater(pending["totals"]["findings"], default["totals"]["findings"])

    def test_record_history_false_and_env_off_skip_recording(self):
        db = fresh_db()
        r = run("pre_commit", {SECRET_FILE: SECRET}, history_db=db)
        self.assertFalse(r["history"]["recorded"])
        os.environ["ARCH_HISTORY"] = "off"
        try:
            r = run("pre_commit", {SECRET_FILE: SECRET}, record_history=True, history_db=db)
            self.assertFalse(r["history"]["recorded"])
        finally:
            os.environ.pop("ARCH_HISTORY")
        self.assertFalse(Path(db).exists())


class ArchReviewAgentTests(unittest.TestCase):
    OPTS = {"write_evidence": False}

    def agent(self, reviewer=None):
        return ArchReviewAgent(pack=PACK, reviewer=reviewer)

    def test_review_blocks_with_actionable_items(self):
        res = self.agent().review({"requester": {"agent": "code-agent"}, "files": {SECRET_FILE: SECRET},
                                   "options": self.OPTS})
        self.assertEqual(res["schema"], "arch-review-result/v1")
        self.assertEqual(res["decision"], "BLOCK")
        self.assertFalse(res["can_proceed"])
        item = next(i for i in res["action_items"] if i["rule_id"] == "SEC-001")
        self.assertEqual(item["outcome"], "BLOCK")
        self.assertTrue(res["next_actions"])
        h = architecture.get_failure_history({"caller": "arch-review-agent:code-agent", "since_days": 1}, pack=PACK)
        self.assertGreaterEqual(h["totals"]["runs"], 1)

    def test_requester_is_required_and_self_certification_rejected(self):
        from arch_review_agent.agent import AgentRequestError
        with self.assertRaises(AgentRequestError):
            self.agent().review({"files": {API_FILE: "x"}})
        files = {API_FILE: "namespace Catalog; public static class CatalogApi { }"}
        first = self.agent().review({"requester": "code-agent", "files": files, "options": self.OPTS})
        rid = first["pending_judgments"][0]["rule_id"]
        res = self.agent().review({"requester": "code-agent", "files": files, "options": self.OPTS, "semantic_judgments": [
            {"rule_id": rid, "verdict": "pass", "rationale": "mine", "reviewer": {"type": "agent", "id": "code-agent"}}]})
        self.assertTrue(any("self-certified" in n for n in res["notes"]))
        self.assertIn(rid, [p["rule_id"] for p in res["pending_judgments"]])

    def test_semantic_reviewer_resolves_pending_rules(self):
        seen = {}

        def reviewer(request):
            seen["rules"] = [r["rule_id"] for r in request["requests"]]
            return [{"rule_id": r, "verdict": "pass", "rationale": "meets rubric"} for r in seen["rules"]]

        files = {API_FILE: "namespace Catalog; public static class CatalogApi { }"}
        res = self.agent(reviewer).review({"requester": "code-agent", "files": files, "options": self.OPTS})
        self.assertTrue(seen["rules"])
        self.assertEqual(res["status"], "completed")
        self.assertEqual(res["pending_judgments"], [])
        self.assertTrue(res["semantic_reviewer"].startswith("arch-review-agent/"))

    def test_iteration_delta_shows_resolved_findings(self):
        first = self.agent().review({"requester": "code-agent", "files": {SECRET_FILE: SECRET}, "options": self.OPTS})
        fixed = 'var pw = builder.Configuration["Basket:Password"];'
        second = self.agent().review({"requester": "code-agent", "files": {SECRET_FILE: fixed}, "options": self.OPTS,
                                      "previous_review_id": first["review_id"]})
        resolved = second["delta"]["resolved"]
        self.assertTrue(any(f.startswith("SEC-001") for f in resolved), second["delta"])
        self.assertNotIn("SEC-001", [i["rule_id"] for i in second["action_items"]])

    def test_advise_returns_rules_and_patterns(self):
        res = self.agent().advise({"files": ["src/Ordering.Infrastructure/OrderingContext.cs"]})
        self.assertTrue(res["rules"])
        self.assertTrue(res["patterns"])

    def test_agent_card_matches_implementation_and_mcp_server(self):
        from arch_review_agent.agent import AGENT_OPERATIONS
        from arch_review_agent import mcp_server
        card = json.loads((AGENTS / "arch_review_agent" / "agent.json").read_text(encoding="utf-8"))
        self.assertEqual({o["name"] for o in card["operations"]}, set(AGENT_OPERATIONS))
        msgs = [{"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {}},
                {"jsonrpc": "2.0", "id": 2, "method": "tools/list"},
                {"jsonrpc": "2.0", "id": 3, "method": "tools/call",
                 "params": {"name": "arch_get_rule", "arguments": {"rule_id": "SEC-001"}}}]
        out = subprocess.run([sys.executable, str(AGENTS / "arch_review.py"), "mcp"],
                             input="\n".join(json.dumps(m) for m in msgs) + "\n",
                             capture_output=True, text=True, encoding="utf-8", timeout=60)
        replies = {r["id"]: r for r in map(json.loads, out.stdout.splitlines())}
        self.assertIn("serverInfo", replies[1]["result"])
        self.assertEqual(len(replies[2]["result"]["tools"]), len(AGENT_OPERATIONS))
        self.assertIn("SEC-001", replies[3]["result"]["content"][0]["text"])
        self.assertTrue(hasattr(mcp_server, "serve"))


class LessonsTests(unittest.TestCase):
    CLEAN = {"src/Catalog.API/Model/Thing.cs": "namespace Catalog; public record Thing(int Id);"}

    def test_lessons_report_denials_reasons_reintroductions_and_prompt(self):
        db = fresh_db()
        seq = [{SECRET_FILE: SECRET}, self.CLEAN, {SECRET_FILE: SECRET}, self.CLEAN]
        for files in seq:
            run("pre_commit", files, record_history=True, history_db=db, caller="test-agent")
        run("pre_commit", {SECRET_FILE: SECRET}, record_history=True, history_db=db, caller="other-agent")
        les = architecture.get_lessons({"agent": "test-agent", "history_db": db}, pack=PACK)
        st = les["denial_stats"]
        self.assertEqual((st["runs"], st["denied_runs"], st["denial_rate"]), (4, 2, 0.5))
        reason = next(r for r in les["denial_reasons"] if r["rule_id"] == "SEC-001")
        self.assertEqual((reason["runs"], reason["reintroduced"]), (2, 1))
        self.assertTrue(reason["why"][0]["message"])
        lesson = next(le for le in les["lessons"] if le["rule_id"] == "SEC-001")
        self.assertTrue(lesson["instead"])
        self.assertIn("SEC-001", les["prompt"])
        self.assertTrue(les["fingerprint"])
        with self.assertRaises(ValueError):
            architecture.get_lessons({"history_db": db}, pack=PACK)

    def test_trend_improves_when_agent_stops_failing(self):
        db = fresh_db()
        for files in [{SECRET_FILE: SECRET}] * 2 + [self.CLEAN] * 2:
            run("pre_commit", files, record_history=True, history_db=db, caller="test-agent")
        self.assertEqual(architecture.get_lessons({"agent": "test-agent", "history_db": db}, pack=PACK)
                         ["denial_stats"]["trend"], "improving")

    def test_agent_feeds_lessons_into_advise_and_flags_repeat_mistakes(self):
        db = fresh_db()
        agent = ArchReviewAgent(pack=PACK)
        req = {"requester": "test-agent", "files": {SECRET_FILE: SECRET}, "context": {"history_db": db},
               "options": {"write_evidence": False}}
        first = agent.review(req)
        self.assertFalse(next(i for i in first["action_items"] if i["rule_id"] == "SEC-001")["repeat_mistake"])
        agent.review(dict(req, files={SECRET_FILE: 'var pw = builder.Configuration["Basket:Password"];'}))
        third = agent.review(req)
        item = next(i for i in third["action_items"] if i["rule_id"] == "SEC-001")
        self.assertTrue(item["repeat_mistake"])
        self.assertTrue(any("get_lessons" in a for a in third["next_actions"]))
        advice = agent.advise({"requester": {"agent": "test-agent"}, "files": [SECRET_FILE], "context": {"history_db": db}})
        self.assertIn("SEC-001", [le["rule_id"] for le in advice["lessons"]["lessons"]])
        self.assertIn("SEC-001", advice["lessons"]["prompt"])
        iters = agent.get_lessons({"agent": "test-agent", "history_db": db})["iterations"]
        self.assertEqual(iters["reviews"], 3)


class PackIntegrityTests(unittest.TestCase):
    def test_selfcheck_passes(self):
        from architecture.selfcheck import selfcheck
        rep = selfcheck(PACK)
        failed = [c for c in rep["checks"] if c["status"] == "fail"]
        self.assertTrue(rep["ok"], failed)


if __name__ == "__main__":
    unittest.main()
