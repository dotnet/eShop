import fs from 'node:fs';
import path from 'node:path';
import { runAgent } from '../../pipeline/lib/agents.mjs';
import { implChanges, implDiff } from '../../pipeline/lib/git.mjs';
import { clearEscalation, escalate, loadState, metricsFile, updateState } from '../../pipeline/lib/state.mjs';
import { appendJsonl, extractResult, nowIso, readJsonl } from '../../pipeline/lib/util.mjs';

const fence = (text, lang = '') => `\`\`\`\`${lang}\n${text.replace(/\r\n/g, '\n').trimEnd()}\n\`\`\`\``;
const block = (reason) => ({ decision: 'block', reason });
const notice = (message) => ({ systemMessage: message });
export const REVIEWS_FILE = 'arch-reviews.jsonl';

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
        findings: findings.map((f) => ({ severity: f.severity, category: f.category, file: f.file })),
      });
      return { verdict, findings, summary: String(raw.summary ?? '') };
    }
  }
  return { verdict: 'ERROR', findings: [], summary: 'architecture reviewer produced no parsable result' };
}

const formatFindings = (findings) =>
  findings
    .map((f, i) => `${i + 1}. [${f.severity ?? 'blocking'}] ${f.file ?? ''}${f.line ? `:${f.line}` : ''} ${f.issue ?? ''}${f.fix ? `\n   Fix: ${f.fix}` : ''}`)
    .join('\n');

export function resetReview(config, story) {
  updateState(config, story.key, (s) => Object.assign(s.review, { rounds: 0, lastHash: null, lastVerdict: null, lastReason: null, unchangedBlocks: 0 }));
}

// Stop hook: review the implementation once the agent has declared the work done. Changes requested
// block the stop and hand the findings back to the agent; approval records `review.approvedHash`,
// which later stages use as their gate.
export function hookStop(config, story, payload = {}) {
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
  const result = architectureReview(config, story, round);
  if (result.verdict === 'ERROR') {
    return notice(`Review: the architecture review for ${story.key} failed to produce a verdict; it will run again next time the agent stops.`);
  }
  if (result.verdict === 'CHANGES_REQUESTED') {
    const reason = `Architecture review round ${round}/${config.review.maxRounds} for ${story.key} requested changes. ${result.summary}\n\n${formatFindings(result.findings)}\n\nMake these corrections, then finish your turn; the review runs again automatically.`;
    updateState(config, story.key, (s) => Object.assign(s.review, { rounds: round, lastHash: hash, lastVerdict: 'CHANGES_REQUESTED', lastReason: reason, unchangedBlocks: 0 }));
    return block(reason);
  }
  updateState(config, story.key, (s) => Object.assign(s.review, { rounds: 0, lastHash: hash, lastVerdict: 'APPROVED', lastReason: result.summary, approvedHash: hash, unchangedBlocks: 0 }));
  clearEscalation(config, story, 'review');
  const extra = result.findings.length ? `\nNon-blocking notes:\n${formatFindings(result.findings)}` : '';
  return notice(`Review: architecture review approved ${story.key} (round ${round}).${extra}`);
}

export function sessionContext(config, story) {
  if (!story) return '';
  return `Architecture review for ${story.key}: ${loadState(config, story.key).review.lastVerdict ?? 'not run'}.`;
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
