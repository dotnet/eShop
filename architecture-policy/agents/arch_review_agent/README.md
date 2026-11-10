# Arch_review_Agent

An independent architecture review agent. Any code agent, test agent, CI job or human can call it. It is a **thin orchestration layer** over the Architecture Policy Pack hook operations (`architecture.*`). It contains **no rule logic**: selfcheck fails if a rule ID appears in agent code. It never edits code, never adds exceptions and never grants human approvals.

| Item | Location |
| --- | --- |
| Contract (agent card, operations, input schemas) | [`agent.json`](agent.json) |
| Core logic | [`agent.py`](agent.py) |
| Semantic (LLM) reviewer backends | [`reviewers.py`](reviewers.py) |
| MCP server (stdio) | [`mcp_server.py`](mcp_server.py) |
| CLI entry | [`../arch_review.py`](../arch_review.py) |
| LLM host definitions | `.github/agents/arch-review.agent.md`, `.claude/agents/arch-review.md` |

## Calling it

```python
import sys; sys.path.insert(0, "architecture-policy/agents")
from arch_review_agent import ArchReviewAgent
agent = ArchReviewAgent()                      # optional: ArchReviewAgent(reviewer=my_llm_fn)
guide = agent.advise({"files": ["src/Ordering.API/Apis/OrdersApi.cs"]})
result = agent.review({"requester": {"agent": "code-agent", "task_id": "T-42"},
                       "files": {"src/Ordering.API/Apis/OrdersApi.cs": "<proposed content>"},
                       "intent": "add cancel-order endpoint"})
```

```bash
python architecture-policy/agents/arch_review.py review request.json      # or '<json>' or - (stdin); exit 1 on BLOCK
python architecture-policy/agents/arch_review.py mcp                      # stdio MCP server: arch_review, arch_advise, ...
python architecture-policy/agents/arch_review.py card                     # print agent.json
```

To register it as an MCP server in a host, add it to that host's MCP config (for example `.vscode/mcp.json`):

```json
{ "servers": { "arch-review": { "type": "stdio", "command": "python", "args": ["architecture-policy/agents/arch_review.py", "mcp"] } } }
```

### Operations

`review`, `advise`, `explain_finding`, `get_failure_history`, `get_lessons`, `get_rule`, `get_pattern`, `get_examples`. Input schemas are in `agent.json`.

### Recommended loop for code agents

1. Call `advise` before writing code. It returns the applicable rules, approved patterns, examples and historic hotspots.
2. Write code, then call `review` with `files` (in-memory content) or `changed_files`/`base`.
3. If the result is `BLOCK`, apply `action_items[].fix` and call `review` again with `previous_review_id`. The `delta` field shows which findings were resolved, which persist, and which are new.
4. If `status == "awaiting_judgments"`, an **independent** reviewer must answer `pending_judgments`, then resubmit them as `semantic_judgments`. That reviewer can be:
   - this agent's LLM backend (`ARCH_REVIEW_LLM_CMD` or the `reviewer=` argument),
   - the LLM host running `arch-review.agent.md`,
   - or a human.

   A judgment whose `reviewer.id` equals `requester.agent` is rejected, so a code agent cannot certify its own change.
5. `human_review` lists approvals that only people can give (ARCH-005, API-003, SEC-005 and RES-004 in the current pack).

The response (`arch-review-result/v1`) contains:

- `decision`, `can_proceed`, `status`, `summary`
- `next_actions`, `action_items` (sorted by severity, with `history.recurring`), `guidance`
- `pending_judgments`, `human_review`, `waived`, `exceptions_expiring_soon`, `delta`
- `run_id`, `review_id`, `engine`

### Automatic run at the end of a code agent's work

Code agents do not need to call `review` themselves at the end. The AI pipeline's review stage (`scripts/review/review.mjs`) runs this agent from the Stop/SubagentStop hook (`node scripts/pipeline/pipeline.mjs hook stop`, wired for Claude, Codex, Gemini and Copilot). It runs after the implementation agent marks its work done (`pipeline done`), just before the agent completes:

- **Scope:** only code agents: the main session, or subagents listed in `review.codeAgents` in `scripts/review/config.json`.
- **Request:** `checkpoint: code_review`, the implementation diff as `changed_files`, requester `implementer`.
- **On `BLOCK`:** the agent is not allowed to finish. It gets the findings, fixes them, and the next stop reviews again with `previous_review_id`.
- **Limit:** this loops until the review passes, up to 5 rounds, then escalates to a human.
- **On `PASS`/`WARN`:** the existing `architecture-reviewer` LLM review follows.

See `docs/ai-pipeline/review.md`.

## Failure history (observability)

Every `architecture.validate` call records its result in the shared hook layer. That covers pre-commit, the agent, CI and test runs alike, so no caller implements its own tracking. Records include:

- the run: caller, checkpoint, branch, head, policy hash, decision and duration
- each BLOCK, WARN or WAIVED finding, with its stable `finding_id`
- agent review iterations

You can query the history in four ways:

- `architecture.get_failure_history(query)`
- `arch.py history --since-days 30 [--rule SEC-001] [--caller arch-review-agent]`
- the agent's `get_failure_history` operation
- `explain_finding`, which reports occurrences and first and last seen for a finding

Queries return totals, the decision trend, top rules, top files, recurring findings (with `still_open`), results by caller, and exception usage.

### Where it is persisted, and why

| Option | Verdict |
| --- | --- |
| Evidence bundles (`evidence/runs/*.json`) | Kept as per-run audit artifacts, but pruned to 50 and not queryable, so not suitable as history. |
| Committed JSONL/YAML in the repo | Rejected: merge conflicts, noisy diffs, and history that differs per branch. |
| **Local SQLite** (`evidence/history/architecture-history.sqlite`, gitignored) | **Chosen as the system of record.** Uses only the Python stdlib, is queryable (aggregations, recurrence) and concurrent-safe (WAL), with 365-day retention. `ARCH_HISTORY_DB` points it at a CI cache or shared volume. |
| **OTLP logs â†’ OpenTelemetry Collector â†’ Azure Monitor / App Insights** | **Chosen for team-wide, cross-machine history.** Opt-in through `ARCH_OTLP_ENDPOINT` or `OTEL_EXPORTER_OTLP_ENDPOINT`, plus `policy.yaml: history.otlp.enabled`. It reuses the OpenTelemetry stack the eShop services already use (Aspire ServiceDefaults), and gives dashboards and alerts through KQL. Export is best-effort with a 2-second timeout. |
| GitHub code scanning (SARIF from `ci` checkpoint) | **Recommended for CI.** It gives alert lifecycle tracking (opened, fixed, reintroduced) in each PR. |

To turn recording off, use `ARCH_HISTORY=off`, set context `record_history: false`, or pass `arch.py validate --no-history`. Recording failures never change a decision.

## Learning loop: stop agents repeating the same mistakes

`architecture.get_lessons({"agent": "test-agent"})` (agent op `get_lessons`, MCP `arch_get_lessons`, CLI `arch.py lessons --agent test-agent`) turns the history into per-agent feedback. It matches both direct calls (`caller: "test-agent"`) and calls made through this agent (`arch-review-agent:test-agent`).

| Field | Answers |
| --- | --- |
| `denial_stats` | How often the verifier denied the agent: runs, denied runs, denial rate, and the trend (improving or worsening). |
| `weekly` | Denial rate per week, for dashboards. |
| `denial_reasons` | Why it was denied, grouped by rule, ranked by denials, warnings and re-introductions. Each entry includes the top messages, files, and the policy's remediation and patterns. |
| `iterations` | Reviews per task, first-pass approval rate, findings resolved, and findings introduced while fixing. |
| `lessons` and `prompt` | Rules that failed in at least `min_occurrences` runs, or that were fixed and then reintroduced. Each lesson pairs the mistake with what to do instead, and the `prompt` field is a Markdown block ready to inject into the agent's instructions. `fingerprint` changes only when the set of lessons changes. |

The lessons are applied automatically in three ways:

1. **Each task:** `advise` with `requester.agent` returns `lessons` (the prompt plus the top lessons). The Test Agent prepends `lessons.prompt` to its working instructions before it writes tests.
2. **Each review:** an action item has `repeat_mistake: true` when the same agent was denied on that rule before. `next_actions` then tells the agent to load its lessons.
3. **Over time:** a scheduled or CI job runs `python architecture-policy/hooks/arch.py lessons --agent test-agent --out <test-agent instructions include>`. If the fingerprint changed, it commits or opens a PR with the file, so a human reviews every change to the agent's standing instructions.

The lessons are derived only from verifier outcomes and the policy's own remediation text, so they cannot drift from the rules. Fix the rule, and the lesson text changes with it.
