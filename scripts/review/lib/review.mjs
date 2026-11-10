import fs from 'node:fs';
import path from 'node:path';
import { runAgent } from '../../pipeline/lib/agents.mjs';
import { implChanges, implDiff } from '../../pipeline/lib/git.mjs';
import { clearEscalation, escalate, loadState, metricsFile, updateState } from '../../pipeline/lib/state.mjs';
import { appendJsonl, extractResult, log, nowIso, readJsonl, run } from '../../pipeline/lib/util.mjs';

const fence = (text, lang = '') => `\`\`\`\`${lang}\n${text.replace(/\r\n/g, '\n').trimEnd()}\n\`\`\`\``;
const block = (reason) => ({ decision: 'block', reason });
const notice = (message) => ({ systemMessage: message });
export const REVIEWS_FILE = 'arch-reviews.jsonl';

// What each finding contributes to arch-reviews.jsonl; `qe lessons` turns recurring ones into implementer lessons.
const loggedFinding = (f) => ({
  severity: f.severity,
  category: f.category,
  ...(f.rule ? { rule: f.rule } : {}),
  file: f.file ?? null,
  line: f.line ?? null,
  issue: f.issue ? String(f.issue).slice(0, 300) : null,
});

function reviewTask(config, story, round) {
  const conventions = ['AGENTS.md', 'CONTRIBUTING.md'].filter((f) => fs.existsSync(path.join(config.root, f)));
  return [
    `Story: ${story.key} (${story.file})`,
    `Review round: ${round} of at most ${config.review.maxRounds}`,
    `Repository conventions to apply: ${conventions.join(', ') || '(none)'}; graphify-out/ may be queried with \`graphify query\`.`,
    '',
    '### Story',
    '',
    fence(fs.readFileSync(path.join(config.root, story.file), 'utf8'), 'markdown'),
    '',
    '### Implementation diff (tests and plans excluded)',
    '',
    fence(implDiff(config), 'diff'),
  ].join('\n');
}

// One review round by a fresh reviewer process that sees only the story and the diff.
export function architectureReview(config, story, round) {
  for (let attempt = 0; attempt <= config.agent.resultParseRetries; attempt++) {
    const result = runAgent(config, {
      name: 'architecture-reviewer',
      mode: 'read',
      model: config.agent.models.reviewer,
      timeoutSec: config.review.timeoutSec,
      task: reviewTask(config, story, round),
    });
    const raw = extractResult(result.output);
    if (raw && ['APPROVED', 'CHANGES_REQUESTED'].includes(raw.verdict)) {
      const findings = Array.isArray(raw.findings) ? raw.findings : [];
      const verdict = raw.verdict === 'APPROVED' && findings.some((f) => f.severity === 'blocking') ? 'CHANGES_REQUESTED' : raw.verdict;
      appendJsonl(metricsFile(config, REVIEWS_FILE), {
        ts: nowIso(),
        story: story.key,
        round,
        verdict,
        reviewer_ver: result.version,
        findings: findings.map(loggedFinding),
      });
      return { verdict, findings, summary: String(raw.summary ?? '') };
    }
  }
  return { verdict: 'ERROR', findings: [], summary: 'architecture reviewer produced no parsable result' };
}

// Policy-pack gate: the Arch_review_Agent (architecture-policy/agents) evaluates the deterministic
// rules of the Architecture Policy Pack before the LLM reviewer. Its history store records every
// denial per requesting agent, which feeds the agent's lessons (arch_review.py get_lessons).
function runPolicy(config, template, input) {
  const p = config.review.policy;
  const agent = config.abs(p.agent);
  let [cmd, ...args] = template.map((a) => a.replaceAll('{agent}', agent));
  let result = run(cmd, args, { cwd: config.root, input, timeoutSec: p.timeoutSec });
  if (result.status === 127 && cmd === 'python') result = run((cmd = 'python3'), args, { cwd: config.root, input, timeoutSec: p.timeoutSec });
  return result;
}

// The requester's recurring denials from the policy history, as prompt-ready guidance (empty when none).
function policyLessons(config) {
  const p = config.review.policy;
  if (!p.lessonsCommand) return '';
  const result = runPolicy(config, p.lessonsCommand, JSON.stringify({ agent: p.requester, limit: p.maxLessons ?? 5 }));
  try {
    return String(JSON.parse(result.stdout).prompt ?? '').trim();
  } catch {
    return '';
  }
}

const POLICY_CATEGORY = { security: 'security', data: 'data', resiliency: 'resilience', observability: 'resilience', error_handling: 'correctness', api: 'fit', architecture: 'fit', testing: 'maintainability' };

function policyFinding(item) {
  const patterns = item.patterns?.length ? ` Approved pattern(s): ${item.patterns.join(', ')}.` : '';
  const repeat = item.repeat_mistake ? ' (repeat mistake: this agent was denied for this rule before)' : '';
  return {
    severity: item.outcome === 'BLOCK' ? 'blocking' : 'minor',
    category: POLICY_CATEGORY[item.category] ?? 'fit',
    rule: item.rule_id,
    repeat: Boolean(item.repeat_mistake),
    file: item.file ?? null,
    line: item.line ?? null,
    issue: `${item.rule_id} ${item.rule_title}: ${item.message}${repeat}`,
    fix: `${item.fix ?? ''}${patterns} Details: python architecture-policy/agents/arch_review.py explain_finding '{"finding_id": "${item.finding_id}"}'`.trim(),
  };
}

export function policyReview(config, story, round, { files, base, previousReviewId = null } = {}) {
  const p = config.review.policy;
  if (!p?.enabled) return { verdict: 'SKIPPED', findings: [], summary: 'policy review disabled' };
  if (!fs.existsSync(config.abs(p.agent))) return { verdict: 'SKIPPED', findings: [], summary: `${p.agent} not found` };
  const input = JSON.stringify({
    requester: { agent: p.requester, task_id: story.key, iteration: round },
    checkpoint: 'code_review',
    intent: `Implementation of story ${story.key} (${story.file})`,
    changed_files: files,
    ...(base ? { base } : {}),
    ...(previousReviewId ? { previous_review_id: previousReviewId } : {}),
    options: { semantic: p.semantic ?? 'auto' },
  });
  const result = runPolicy(config, p.command, input);
  let response;
  try {
    response = JSON.parse(result.stdout);
  } catch {
    response = null;
  }
  if (!response || !['PASS', 'WARN', 'BLOCK'].includes(response.decision)) {
    const why = response?.message ?? (result.stderr.trim().split('\n').at(-1) || `exit ${result.status}`);
    return { verdict: 'ERROR', findings: [], summary: `policy review failed: ${why}` };
  }
  // needs_review items are pending semantic judgments, not findings; the LLM reviewer covers that ground.
  const items = (response.action_items ?? []).filter((i) => ['BLOCK', 'WARN'].includes(i.outcome) && i.status !== 'needs_review');
  const findings = items.map(policyFinding);
  const verdict = response.decision === 'BLOCK' ? 'CHANGES_REQUESTED' : 'APPROVED';
  if (verdict === 'CHANGES_REQUESTED') {
    appendJsonl(metricsFile(config, REVIEWS_FILE), {
      ts: nowIso(),
      story: story.key,
      round,
      verdict,
      reviewer: 'arch-review-agent',
      reviewer_ver: response.agent_version ?? null,
      review_id: response.review_id,
      findings: findings.map(loggedFinding),
    });
  }
  // A repeated mistake means the findings alone did not stick; hand the agent its lessons from the history.
  const lessons = verdict === 'CHANGES_REQUESTED' && findings.some((f) => f.repeat) ? policyLessons(config) : '';
  return { verdict, findings, summary: String(response.summary ?? ''), reviewId: response.review_id, decision: response.decision, lessons };
}

// One review round: the policy gate first, then (unless it blocked) the LLM architecture-reviewer.
export function reviewRound(config, story, round) {
  const state = loadState(config, story.key);
  const { base, files } = implChanges(config);
  const policy = policyReview(config, story, round, { files, base, previousReviewId: state.review.policyReviewId });
  if (policy.reviewId) updateState(config, story.key, (s) => (s.review.policyReviewId = policy.reviewId));
  if (policy.verdict === 'CHANGES_REQUESTED') return { ...policy, summary: `Architecture Policy Pack: ${policy.summary}` };
  const llm = architectureReview(config, story, round);
  if (policy.verdict === 'ERROR') log(`${story.key}: ${policy.summary}`);
  const note = policy.verdict === 'ERROR' ? ` (Architecture Policy Pack skipped: ${policy.summary})` : '';
  return { ...llm, findings: [...llm.findings, ...policy.findings], summary: `${llm.summary}${note}` };
}

// Only code agents are reviewed: the main session (Stop) or a subagent whose type is listed in
// review.codeAgents (SubagentStop). Pipeline child agents never reach here (PIPELINE_CHILD).
export function isCodeAgentStop(config, payload = {}) {
  if (payload.hook_event_name !== 'SubagentStop' || !payload.agent_type) return true;
  return (config.review.codeAgents ?? []).includes(payload.agent_type);
}

const formatFindings = (findings) =>
  findings
    .map((f, i) => `${i + 1}. [${f.severity ?? 'blocking'}] ${f.file ?? ''}${f.line ? `:${f.line}` : ''} ${f.issue ?? ''}${f.fix ? `\n   Fix: ${f.fix}` : ''}`)
    .join('\n');

export function resetReview(config, story) {
  updateState(config, story.key, (s) => Object.assign(s.review, { rounds: 0, lastHash: null, lastVerdict: null, lastReason: null, unchangedBlocks: 0, policyReviewId: null }));
}

// Stop hook: review the implementation once the agent has declared the work done. Changes requested
// block the stop and hand the findings back to the agent; approval records `review.approvedHash`,
// which later stages use as their gate.
export function hookStop(config, story, payload = {}) {
  if (!isCodeAgentStop(config, payload)) return null;
  const state = loadState(config, story.key);
  if (!state.done) return null;
  const { hash } = implChanges(config);
  if (!hash) return null;
  const review = state.review;

  if (review.lastHash === hash) {
    if (review.lastVerdict === 'CHANGES_REQUESTED') {
      if (payload.stop_hook_active || review.unchangedBlocks >= 1) {
        return notice(`Review: ${story.key} still has unresolved architecture findings and the code has not changed. Run \`node scripts/review/review.mjs status ${story.key}\` for details.`);
      }
      updateState(config, story.key, (s) => (s.review.unchangedBlocks += 1));
      return block(`The architecture review findings for ${story.key} are still open and nothing has changed since the review. Address them:\n\n${review.lastReason}`);
    }
    return null;
  }

  if (review.rounds >= config.review.maxRounds) {
    updateState(config, story.key, (s) => Object.assign(s.review, { lastHash: hash, lastVerdict: 'ESCALATED' }));
    escalate(config, story, 'review', `no approval after ${review.rounds} review rounds`, review.lastReason ?? '');
    return notice(`Review: ${story.key} was not approved after ${review.rounds} architecture review rounds. A human needs to decide; see .agent-state/escalations/${story.key}-review.md.`);
  }

  const round = review.rounds + 1;
  const result = reviewRound(config, story, round);
  if (result.verdict === 'ERROR') {
    return notice(`Review: the architecture review for ${story.key} failed to produce a verdict; it will run again next time the agent stops.`);
  }
  if (result.verdict === 'CHANGES_REQUESTED') {
    const lessons = result.lessons ? `\n\nYou have been denied for these rules before. Lessons from your review history (follow them for the rest of this story):\n\n${result.lessons}` : '';
    const reason = `Architecture review round ${round}/${config.review.maxRounds} for ${story.key} requested changes. ${result.summary}\n\n${formatFindings(result.findings)}${lessons}\n\nMake these corrections, then finish your turn; the review runs again automatically.`;
    updateState(config, story.key, (s) => Object.assign(s.review, { rounds: round, lastHash: hash, lastVerdict: 'CHANGES_REQUESTED', lastReason: reason, unchangedBlocks: 0 }));
    return block(reason);
  }
  updateState(config, story.key, (s) => Object.assign(s.review, { rounds: 0, lastHash: hash, lastVerdict: 'APPROVED', lastReason: result.summary, approvedHash: hash, unchangedBlocks: 0, policyReviewId: null }));
  clearEscalation(config, story, 'review');
  const extra = result.findings.length ? `\nNon-blocking notes:\n${formatFindings(result.findings)}` : '';
  return notice(`Review: architecture review approved ${story.key} (round ${round}).${extra}`);
}

export function sessionContext(config, story) {
  if (!story) return '';
  const status = `Architecture review for ${story.key}: ${loadState(config, story.key).review.lastVerdict ?? 'not run'}.`;
  // The main session can be the code agent; give it the same lessons the implementer agent reads.
  const file = path.join(config.root, config.lessonsDir, 'implementer.md');
  const lessons = fs.existsSync(file) ? fs.readFileSync(file, 'utf8').trim() : '';
  return lessons ? `${status}\n\nArchitecture lessons from earlier review denials (follow these when changing code; from ${config.lessonsDir}/implementer.md):\n\n${lessons}` : status;
}

export function reviewMetrics(config, { since } = {}) {
  const reviews = readJsonl(metricsFile(config, REVIEWS_FILE)).filter((r) => !since || r.ts >= since);
  const first = reviews.filter((r) => r.round === 1);
  const findings = {};
  for (const r of reviews) for (const f of r.findings ?? []) findings[f.category ?? 'other'] = (findings[f.category ?? 'other'] ?? 0) + 1;
  return {
    rounds: reviews.length,
    approvals: reviews.filter((r) => r.verdict === 'APPROVED').length,
    firstRoundApprovalRate: first.length ? Math.round((first.filter((r) => r.verdict === 'APPROVED').length / first.length) * 100) / 100 : 0,
    stories: new Set(reviews.map((r) => r.story)).size,
    findingsByCategory: findings,
  };
}
