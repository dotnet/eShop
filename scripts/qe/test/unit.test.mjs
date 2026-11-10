import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { test } from 'node:test';
import { formatEvidence, readEvidence } from '../lib/evidence.mjs';
import { lessonCandidates, parseRules } from '../lib/lessons.mjs';
import { computeMetrics } from '../lib/metrics.mjs';
import { extractResult, parseFrontMatter, stringifyFrontMatter } from '../../pipeline/lib/util.mjs';
import { attributedAgent, normalizeVerdict } from '../lib/verify.mjs';

const config = {
  loops: { requiredScore: 10 },
  categories: { plan: ['missing-sad-path', 'other'], automation: ['missing-assertion', 'other'] },
  lessons: { storyThreshold: 3, retireAfterStories: 3, revertMinRounds: 4, examplesPerCategory: 2 },
};

test('extractResult takes the last balanced AGENT_RESULT object, even with braces in strings', () => {
  const text = 'AGENT_RESULT: {"score": 1}\nthinking...\nAGENT_RESULT: {"score": 9, "gaps": [{"detail": "use } and { \\" carefully"}]}\ntrailing';
  assert.deepEqual(extractResult(text), { score: 9, gaps: [{ detail: 'use } and { " carefully' }] });
  assert.equal(extractResult('no marker'), null);
  assert.equal(extractResult('AGENT_RESULT: {"broken": '), null);
});

test('front matter round-trips lists with commas and drops undefined fields', () => {
  const text = stringifyFrontMatter({ id: 'TC-A-1-01', covers: ['Rule, part 1', 'x'], score: 10, gone: undefined }, '# Body\n');
  const parsed = parseFrontMatter(text);
  assert.deepEqual(parsed.data, { id: 'TC-A-1-01', covers: ['Rule, part 1', 'x'], score: 10 });
  assert.equal(parsed.body, '# Body\n');
});

test('normalizeVerdict only passes a clean 10 and never lets a low score go unexplained', () => {
  assert.equal(normalizeVerdict(config, 'plan', { score: 10, gaps: [] }).passed, true);
  const gapsAt10 = normalizeVerdict(config, 'plan', { score: 10, gaps: [{ category: 'missing-sad-path', detail: 'auth' }] });
  assert.equal(gapsAt10.passed, false);

  const unexplained = normalizeVerdict(config, 'plan', { score: 8 });
  assert.equal(unexplained.passed, false);
  assert.equal(unexplained.gaps[0].subtype, 'unexplained-score');

  const unknown = normalizeVerdict(config, 'automation', { score: 7, gaps: [{ category: 'made-up', root_stage: 'plan', detail: 'x' }] });
  assert.equal(unknown.gaps[0].category, 'other');
  assert.equal(unknown.gaps[0].subtype, 'made-up');
  assert.equal(unknown.gaps[0].root_stage, 'plan');
  assert.equal(normalizeVerdict(config, 'automation', { score: 'garbage' }).score, 0);
});

test('denials that trace back to the plan are attributed to the plan agent', () => {
  assert.equal(attributedAgent('plan', 'plan'), 'qe-plan');
  assert.equal(attributedAgent('automation', 'plan'), 'qe-plan');
  assert.equal(attributedAgent('automation', 'automation'), 'qe-automation');
});

const denial = (story, ts, extra = {}) => ({ ts, stage: 'automation', story, category: 'missing-assertion', subtype: 'error-body', attributed_agent: 'qe-automation', detail: `d-${story}`, ...extra });
const runAt = (story, ts) => ({ ts, stage: 'automation', story, passed: false });

test('lessons: a category recurring across enough stories becomes an "add" request', () => {
  const data = {
    denials: [denial('A-1', '2026-01-01'), denial('B-1', '2026-01-02'), denial('C-1', '2026-01-03'), denial('C-1', '2026-01-04', { category: 'other' })],
    runs: [],
    calibration: [],
  };
  const report = lessonCandidates(config, data, 'qe-automation', '');
  assert.equal(report.actionable, true);
  assert.equal(report.changes.length, 1);
  assert.deepEqual({ ...report.changes[0], examples: report.changes[0].examples.length }, { action: 'add', rule: null, category: 'missing-assertion', subtype: 'error-body', count: 3, stories: 3, examples: 2 });

  const twoStories = { ...data, denials: data.denials.filter((d) => d.story !== 'C-1') };
  assert.equal(lessonCandidates(config, twoStories, 'qe-automation', '').actionable, false);
});

test('lessons: existing rules are strengthened, retired or reverted based on what happened after they were added', () => {
  const rule = (id, added) => `## ${id}: Assert the error body\n<!-- category: missing-assertion; subtype: error-body; added: ${added} -->\n\nText.\n`;
  const runs = ['A-1', 'B-1', 'C-1', 'D-1'].flatMap((s, i) => [runAt(s, `2026-01-0${i + 1}`), runAt(s, `2026-03-0${i + 1}`)]);

  // Still recurring in 3 stories after the rule: strengthen. Rate did not drop: revert.
  const recurring = { denials: ['A-1', 'B-1', 'C-1'].flatMap((s, i) => [denial(s, `2026-01-0${i + 1}`), denial(s, `2026-03-0${i + 1}`)]), runs, calibration: [] };
  const r1 = lessonCandidates(config, recurring, 'qe-automation', rule('L1', '2026-02-01'));
  assert.equal(r1.changes[0].action, 'strengthen');
  assert.equal(r1.changes[0].rule, 'L1');
  assert.equal(r1.rules[0].action, 'revert');

  // No hits in the 4 stories since the rule was added: retire.
  const fixed = { denials: ['A-1', 'B-1', 'C-1'].map((s, i) => denial(s, `2026-01-0${i + 1}`)), runs, calibration: [] };
  const r2 = lessonCandidates(config, fixed, 'qe-automation', rule('L1', '2026-02-01'));
  assert.equal(r2.changes.length, 0);
  assert.equal(r2.rules[0].action, 'retire');

  assert.deepEqual(parseRules(rule('L3', '2026-02-01')).map((r) => [r.id, r.category, r.subtype]), [['L3', 'missing-assertion', 'error-body']]);
});

test('lessons for verifiers come from calibration misses, not from producer denials', () => {
  const data = {
    denials: ['A-1', 'B-1', 'C-1'].map((s) => denial(s, '2026-01-01')),
    runs: [],
    calibration: ['A-1', 'B-1', 'C-1'].map((s) => ({ ts: '2026-01-01', stage: 'plan', story: s, category: 'missing-sad-path', subtype: 'auth', attributed_agent: 'qe-plan-verifier' })),
  };
  assert.equal(lessonCandidates(config, data, 'qe-plan-verifier', '').changes[0].category, 'missing-sad-path');
});

test('lessons for the implementer come from blocking architecture review findings, grouped by policy rule', () => {
  const review = (story, findings, extra = {}) => ({ ts: '2026-01-01', story, round: 1, verdict: 'CHANGES_REQUESTED', findings, ...extra });
  const sec = { severity: 'blocking', category: 'security', rule: 'SEC-001', file: 'src/A.cs', line: 3, issue: 'SEC-001 No secrets: literal password' };
  const data = {
    denials: ['A-1', 'B-1', 'C-1'].map((s) => denial(s, '2026-01-01')),
    runs: [],
    calibration: [],
    reviews: [
      review('A-1', [sec, { severity: 'minor', category: 'fit', file: 'src/A.cs' }]),
      review('B-1', [sec], { reviewer: 'arch-review-agent' }),
      review('C-1', [sec]),
      review('D-1', [sec], { verdict: 'APPROVED' }),
      review('E-1', [{ severity: 'blocking', category: 'fit', file: 'src/E.cs' }]),
    ],
  };
  const report = lessonCandidates(config, data, 'implementer', '');
  assert.deepEqual(report.changes.map((c) => [c.action, c.category, c.subtype, c.stories]), [['add', 'security', 'SEC-001', 3]]);
  assert.deepEqual(report.changes[0].examples.at(-1), { story: 'C-1', case: 'review round 1 (architecture-reviewer)', detail: 'SEC-001 No secrets: literal password', location: 'src/A.cs:3' });
});

test('PR check findings teach the implementer (round 1), the fixer (not fixed) and the critics (dismissed)', () => {
  const check = (story, round, upheld, dismissed = []) => ({ ts: '2026-01-02', target: `pr-${story}`, story, round, lensScores: { acceptance: 8, style: 10 }, upheld, dismissed });
  const acc = (id, repeat = false) => ({ id, lens: 'acceptance', severity: 'major', title: 'AC2 not met', detail: 'no 404 path', evidence: 'src/A.cs:9', repeat });
  const noise = { id: 'STY-1-1', lens: 'style', severity: 'minor', title: 'Naming', reason: 'matches the repo convention' };
  const data = {
    denials: [],
    runs: [],
    calibration: [],
    reviews: [],
    prChecks: ['A-1', 'B-1', 'C-1'].flatMap((s) => [check(s, 1, [acc('ACC-1-1')], [noise]), check(s, 2, [acc('ACC-1-1', true)])]),
  };
  const implementer = lessonCandidates(config, data, 'implementer', '');
  assert.deepEqual(implementer.changes.map((c) => [c.action, c.category, c.stories]), [['add', 'pr-check-acceptance', 3]]);
  assert.deepEqual(implementer.changes[0].examples.at(-1), { story: 'C-1', case: 'PR check pr-C-1 round 1 (ACC-1-1)', detail: 'AC2 not met: no 404 path', location: 'src/A.cs:9' });
  assert.deepEqual(lessonCandidates(config, data, 'pr-check-fixer', '').changes.map((c) => [c.category, c.subtype, c.count]), [['not-fixed', 'acceptance', 3]]);
  assert.deepEqual(lessonCandidates(config, data, 'pr-check-style', '').changes.map((c) => [c.category, c.stories]), [['dismissed-finding', 3]]);
  assert.equal(lessonCandidates(config, data, 'pr-check-acceptance', '').changes.length, 0);
});

test('computeMetrics summarizes loops per stage and categories per attributed agent', () => {
  const data = {
    runs: [
      { ts: '1', stage: 'plan', story: 'A-1', round: 1, passed: false, agent_prompt_ver: 'v1' },
      { ts: '2', stage: 'plan', story: 'A-1', round: 2, passed: true, agent_prompt_ver: 'v1' },
      { ts: '3', stage: 'plan', story: 'B-1', round: 1, passed: true, agent_prompt_ver: 'v2' },
    ],
    denials: [{ ts: '1', stage: 'plan', story: 'A-1', category: 'missing-sad-path', subtype: 'auth', attributed_agent: 'qe-plan' }],
    prechecks: [],
    calibration: [],
  };
  const m = computeMetrics(config, { data });
  const plan = m.stages.plan;
  assert.equal(plan.verifierRounds, 3);
  assert.equal(plan.firstRoundPassRate, 0.5);
  assert.equal(plan.denials, 1);
  assert.equal(plan.stories, 2);
  assert.equal(m.categories[0].category, 'missing-sad-path');
});

test('readEvidence ignores manifests from older runs and formats failed tests with absolute keyframe paths', () => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'qe-evidence-'));
  const cfg = { root, evidence: { manifest: 'out/manifest.json' } };
  fs.mkdirSync(path.join(root, 'out'));
  const startedAt = '2026-01-01T10:00:00.000Z';
  fs.writeFileSync(path.join(root, 'out/manifest.json'), JSON.stringify({
    startedAt,
    tests: [
      { title: 'TC-DEMO-1-01 adds', file: 'e2e/a.spec.ts', line: 3, ok: false, status: 'failed', errors: ['expected 1 got 0'],
        videos: ['p/a.failed.webm'], keyframes: [{ label: 'step failed: add', path: 'p/a.failed.keyframes/01-add.png' }] },
      { title: 'TC-OTHER-1-01 unrelated', file: 'e2e/b.spec.ts', line: 1, ok: true, status: 'passed', videos: [], keyframes: [] },
    ],
  }));

  assert.deepEqual(readEvidence(cfg, { casePrefix: 'TC-DEMO-1-', since: '2026-01-01T10:00:01.000Z' }), []);
  const tests = readEvidence(cfg, { casePrefix: 'TC-DEMO-1-', since: '2026-01-01T09:59:59.000Z' });
  assert.equal(tests.length, 1);
  const frame = path.join(root, 'out', 'p/a.failed.keyframes/01-add.png');
  assert.equal(tests[0].keyframes[0].path, frame);

  const text = formatEvidence(tests);
  assert.match(text, /FAILED: failed/);
  assert.match(text, /expected 1 got 0/);
  assert.ok(text.includes(`1. step failed: add: \`${frame}\``));
  fs.rmSync(root, { recursive: true, force: true });
});
