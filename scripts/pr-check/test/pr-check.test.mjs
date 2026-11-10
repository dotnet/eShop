// PR check: deterministic scoring, and end-to-end runs of the adversarial loop in a throwaway git repo
// with the scripted fake agent standing in for the critics, defender, judge and fixer.
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { after, before, test } from 'node:test';
import { fileURLToPath } from 'node:url';
import { videoLinks } from '../lib/github.mjs';
import { scoreRound } from '../lib/loop.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const source = path.resolve(here, '..', '..', '..');
const fakeAgent = path.join(source, 'scripts', 'pipeline', 'test', 'fixtures', 'fake-agent.mjs');
let repo;
let scriptFile;

const finding = (id, lens, severity, status = 'open') => ({ id, lens, severity, status, title: id, detail: '', evidence: '', requirement: '', fix: 'fix it', thread: [] });

test('scoreRound: nothing upheld scores 10; each upheld severity caps its lens and the PR takes the lowest lens', () => {
  const lenses = ['acceptance', 'flow', 'style'];
  assert.equal(scoreRound({ lenses, findings: [] }).score, 10);
  const r = scoreRound({
    lenses,
    findings: [finding('ACC-1-1', 'acceptance', 'major'), finding('STY-1-1', 'style', 'minor'), finding('FLOW-1-1', 'flow', 'blocking')],
    rulings: [
      { id: 'ACC-1-1', ruling: 'upheld', severity: 'major' },
      { id: 'STY-1-1', ruling: 'upheld', severity: 'minor' },
      { id: 'FLOW-1-1', ruling: 'dismissed' },
    ],
    judgeLensScores: { acceptance: 10, style: 7 },
  });
  assert.deepEqual(r.lensScores, { acceptance: 8, flow: 10, style: 7 });
  assert.equal(r.score, 7);
  assert.deepEqual(r.upheld.map((f) => f.id), ['ACC-1-1', 'STY-1-1']);
});

test('scoreRound: unruled findings stand unless withdrawn, judge coverage gaps count, failed critics score 0', () => {
  const r = scoreRound({
    lenses: ['acceptance', 'flow', 'design'],
    findings: [finding('ACC-1-1', 'acceptance', 'minor'), finding('FLOW-1-1', 'flow', 'blocking', 'withdrawn')],
    rulings: [{ id: 'flow-coverage-1', ruling: 'upheld', severity: 'major', reason: 'AC 2 unverified', fix: 'add it' }],
    failedLenses: ['design'],
  });
  assert.deepEqual(r.lensScores, { acceptance: 9, flow: 8, design: 0 });
  assert.deepEqual(r.upheld.map((f) => [f.id, f.lens]), [['ACC-1-1', 'acceptance'], ['flow-coverage-1', 'flow']]);
});

test('videoLinks finds direct video links and GitHub attachments, once each', () => {
  const body = 'Demo: https://example.com/a/run.webm and ![x](https://github.com/user-attachments/assets/1234-abcd) again https://example.com/a/run.webm. Docs: https://example.com/readme.md';
  assert.deepEqual(videoLinks(body, ['.webm', '.mp4']), ['https://example.com/a/run.webm', 'https://github.com/user-attachments/assets/1234-abcd']);
});

const sh = (cmd, args) => {
  const r = spawnSync(cmd, args, { cwd: repo, encoding: 'utf8' });
  if (r.status !== 0) throw new Error(`${cmd} ${args.join(' ')} failed:\n${r.stdout}\n${r.stderr}`);
  return r.stdout;
};
const write = (rel, text) => {
  fs.mkdirSync(path.dirname(path.join(repo, rel)), { recursive: true });
  fs.writeFileSync(path.join(repo, rel), text);
};
const calls = () => JSON.parse(fs.readFileSync(`${scriptFile}.calls.json`, 'utf8'));
const prCheck = (args, script) => {
  fs.writeFileSync(scriptFile, JSON.stringify(script));
  for (const suffix of ['.calls.json', '.prompts.log']) fs.rmSync(scriptFile + suffix, { force: true });
  const env = { ...process.env, PIPELINE_AGENT_CMD: `"${process.execPath}" "${fakeAgent}"`, PIPELINE_FAKE_SCRIPT: scriptFile, PIPELINE_BASE_REF: 'main' };
  delete env.PIPELINE_CHILD;
  delete env.PIPELINE_DISABLE;
  return spawnSync(process.execPath, ['scripts/pr-check/pr-check.mjs', 'run', '--story', 'DEMO-1', ...args], { cwd: repo, encoding: 'utf8', env });
};

const clean = (lens) => ({ result: { lens, score: 10, summary: 'ok', coverage: [], findings: [] } });
const expiredCoupon = { severity: 'blocking', title: 'Expired coupon is accepted', detail: 'AC 2 says 400', evidence: 'src/Basket/Coupons.cs:1', requirement: 'AC 2', fix: 'reject expired coupons' };
const upheldRound = { result: { score: 6, lensScores: { acceptance: 6 }, summary: 'AC 2 unmet', rulings: [{ id: 'ACC-1-1', ruling: 'upheld', severity: 'blocking', reason: 'conceded', fix: 'reject expired coupons' }] } };

before(() => {
  repo = fs.mkdtempSync(path.join(os.tmpdir(), 'pr-check-e2e-'));
  scriptFile = path.join(repo, '..', `${path.basename(repo)}-agent.json`);
  for (const dir of ['scripts/pipeline', 'scripts/pr-check']) {
    fs.cpSync(path.join(source, dir), path.join(repo, dir), { recursive: true, filter: (p) => !p.startsWith(path.join(source, dir, 'test')) });
  }
  fs.cpSync(path.join(source, '.github/agents'), path.join(repo, '.github/agents'), { recursive: true });
  write('.gitignore', '.agent-state/\n.agent-metrics/\n');
  write('product-research/stories/DEMO-1-apply-coupon.md', '# DEMO-1 Apply coupon\n\nAC 1: a valid coupon gives 10% off. AC 2: an expired coupon is rejected with 400.\n');
  sh('git', ['init', '-q', '-b', 'main']);
  sh('git', ['config', 'user.email', 'qe@example.com']);
  sh('git', ['config', 'user.name', 'QE Test']);
  sh('git', ['add', '-A']);
  sh('git', ['commit', '-qm', 'init']);
  sh('git', ['switch', '-q', '-c', 'feature/DEMO-1']);
  write('src/Basket/Coupons.cs', '// coupons\n');
  sh('git', ['add', '-A']);
  sh('git', ['commit', '-qm', 'DEMO-1: coupons']);
});

after(() => {
  fs.rmSync(repo, { recursive: true, force: true });
  for (const suffix of ['', '.calls.json', '.prompts.log']) fs.rmSync(scriptFile + suffix, { force: true });
});

test('report-only (--no-fix) runs one round, never calls the fixer, and fails below 10', () => {
  const r = prCheck(['--no-fix', '--json'], {
    'pr-check-acceptance': [{ result: { lens: 'acceptance', score: 6, summary: 'AC 2', coverage: [], findings: [expiredCoupon] } }],
    'pr-check-flow': [clean('flow')],
    'pr-check-style': [clean('style')],
    'pr-check-defender': [{ result: { responses: [{ id: 'ACC-1-1', position: 'concede', argument: 'true' }] } }],
    'pr-check-judge': [upheldRound],
  });
  assert.equal(r.status, 1, r.stderr);
  const result = JSON.parse(r.stdout);
  assert.deepEqual([result.verdict, result.score, result.rounds], ['FAIL', 6, 1]);
  assert.deepEqual(result.openFindings.map((f) => f.id), ['ACC-1-1']);
  assert.equal(calls()['pr-check-fixer'], undefined);
});

test('the loop debates, fixes, commits and re-reviews with fresh critics until 10/10', () => {
  const head = sh('git', ['rev-parse', 'HEAD']).trim();
  const r = prCheck(['--json'], {
    'pr-check-acceptance': [
      { result: { lens: 'acceptance', score: 6, summary: 'AC 2', coverage: [], findings: [expiredCoupon] } },
      { result: { responses: [{ id: 'ACC-1-1', action: 'maintain', argument: 'the code has no expiry check' }] } },
      clean('acceptance'),
    ],
    'pr-check-flow': [clean('flow')],
    'pr-check-style': [clean('style')],
    'pr-check-defender': [
      { result: { responses: [{ id: 'ACC-1-1', position: 'rebut', argument: 'expiry is checked upstream', evidence: 'none' }] } },
      { result: { responses: [{ id: 'ACC-1-1', position: 'concede', argument: 'it is not' }] } },
    ],
    'pr-check-judge': [upheldRound, { result: { score: 10, lensScores: {}, summary: 'all met', rulings: [] } }],
    'pr-check-fixer': [{ writeImpl: 'src/Basket/Coupons.cs', result: { fixed: ['ACC-1-1'], notFixed: [], summary: 'reject expired' } }],
  });
  assert.equal(r.status, 0, r.stderr);
  const result = JSON.parse(r.stdout);
  assert.deepEqual([result.verdict, result.score, result.rounds, result.commits.length], ['PASS', 10, 2, 1]);
  assert.deepEqual(calls(), { 'pr-check-acceptance': 3, 'pr-check-flow': 2, 'pr-check-style': 2, 'pr-check-defender': 2, 'pr-check-judge': 2, 'pr-check-fixer': 1 });
  assert.match(sh('git', ['log', '-1', '--format=%s']), /DEMO-1: PR check round 1 fixes/);
  assert.notEqual(sh('git', ['rev-parse', 'HEAD^']).trim(), sh('git', ['rev-parse', 'HEAD']).trim());
  assert.equal(sh('git', ['rev-parse', 'HEAD^']).trim(), head);

  const debate = JSON.parse(fs.readFileSync(path.join(repo, '.agent-state/pr-check/DEMO-1/round-1/debate.json'), 'utf8'));
  assert.deepEqual(debate.findings[0].thread.map((t) => t.position ?? t.action), ['rebut', 'maintain', 'concede']);
  const report = fs.readFileSync(path.join(repo, result.report), 'utf8');
  assert.match(report, /# PR check: PASS 10\/10/);
  assert.match(report, /## Resolved during the loop\n\n- ACC-1-1/);
  const metrics = JSON.parse(spawnSync(process.execPath, ['scripts/pr-check/pr-check.mjs', 'metrics', '--json'], { cwd: repo, encoding: 'utf8' }).stdout);
  assert.equal(metrics.perfect, 1);
});

test('the loop escalates when the fixer cannot fix the upheld findings', () => {
  const r = prCheck(['--json'], {
    'pr-check-acceptance': [{ result: { lens: 'acceptance', score: 6, summary: 'AC 2', coverage: [], findings: [expiredCoupon] } }],
    'pr-check-flow': [clean('flow')],
    'pr-check-style': [clean('style')],
    'pr-check-defender': [{ result: { responses: [{ id: 'ACC-1-1', position: 'concede', argument: 'true' }] } }],
    'pr-check-judge': [upheldRound],
    'pr-check-fixer': [{ result: { fixed: [], notFixed: [{ id: 'ACC-1-1', reason: 'needs a product decision' }], summary: 'blocked' } }],
  });
  assert.equal(r.status, 1, r.stderr);
  const result = JSON.parse(r.stdout);
  assert.deepEqual([result.verdict, result.rounds], ['ESCALATED', 1]);
  assert.match(fs.readFileSync(path.join(repo, '.agent-state/escalations/DEMO-1-pr-check.md'), 'utf8'), /ACC-1-1/);
});
