// End-to-end runs of both workflows in a throwaway git repo, with a scripted fake agent CLI
// standing in for Claude/Copilot/Codex (QE_AGENT_CMD).
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { after, before, test } from 'node:test';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const source = path.resolve(here, '..', '..', '..');
const fakeAgent = path.join(here, 'fixtures', 'fake-agent.mjs');
let repo;
let scriptFile;

const sh = (cmd, args, opts = {}) => {
  const r = spawnSync(cmd, args, { cwd: repo, encoding: 'utf8', ...opts });
  if (r.status !== 0 && !opts.allowFail) throw new Error(`${cmd} ${args.join(' ')} failed:\n${r.stdout}\n${r.stderr}`);
  return r;
};
const gitc = (...args) => sh('git', args);
const write = (rel, text) => {
  fs.mkdirSync(path.dirname(path.join(repo, rel)), { recursive: true });
  fs.writeFileSync(path.join(repo, rel), text);
};
const read = (rel) => fs.readFileSync(path.join(repo, rel), 'utf8');
const jsonl = (name) => {
  const file = path.join(repo, '.agent-metrics', name);
  return fs.existsSync(file) ? fs.readFileSync(file, 'utf8').trim().split('\n').filter(Boolean).map((l) => JSON.parse(l)) : [];
};
const stateOf = (key) => JSON.parse(read(`.agent-state/stories/${key}.json`));

function agentScript(script) {
  fs.writeFileSync(scriptFile, JSON.stringify(script));
  for (const suffix of ['.calls.json', '.prompts.log']) fs.rmSync(scriptFile + suffix, { force: true });
}
const calls = () => (fs.existsSync(`${scriptFile}.calls.json`) ? JSON.parse(fs.readFileSync(`${scriptFile}.calls.json`, 'utf8')) : {});
const prompts = () => fs.readFileSync(`${scriptFile}.prompts.log`, 'utf8');

function qe(args, { input = '', env = {} } = {}) {
  const base = { ...process.env };
  delete base.QE_PIPELINE_CHILD;
  delete base.QE_DISABLE;
  return spawnSync(process.execPath, ['scripts/qe/qe.mjs', ...args], {
    cwd: repo,
    input,
    encoding: 'utf8',
    env: { ...base, QE_AGENT_CMD: `"${process.execPath}" "${fakeAgent}"`, QE_FAKE_SCRIPT: scriptFile, QE_BASE_REF: 'main', ...env },
  });
}

async function waitFor(predicate, timeoutMs = 30_000) {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    if (predicate()) return;
    await new Promise((r) => setTimeout(r, 100));
  }
  throw new Error('timed out waiting for background run');
}

const STORY = 'product-research/stories/DEMO-1-apply-coupon.md';
const PLAN = 'tests/plans/DEMO-1-apply-coupon';
const happySad = [{ title: 'Valid coupon applies', type: 'happy' }, { title: 'Expired coupon rejected', type: 'sad' }];

before(() => {
  repo = fs.mkdtempSync(path.join(os.tmpdir(), 'qe-e2e-'));
  scriptFile = path.join(repo, '..', `${path.basename(repo)}-agent.json`);
  fs.cpSync(path.join(source, 'scripts/qe'), path.join(repo, 'scripts/qe'), { recursive: true, filter: (p) => !p.startsWith(path.join(source, 'scripts/qe/test')) });
  fs.cpSync(path.join(source, '.github/agents'), path.join(repo, '.github/agents'), { recursive: true });
  const config = JSON.parse(read('scripts/qe/config.json'));
  config.precheck.runTests = false;
  write('scripts/qe/config.json', JSON.stringify(config, null, 2));
  write('.gitignore', '.agent-state/\n');
  write(STORY, '# DEMO-1 Apply coupon\n\nRule: a valid coupon gives 10% off; an expired coupon is rejected with 400.\n');
  write('src/Basket/Coupons.cs', '// coupons\n');
  gitc('init', '-q', '-b', 'main');
  gitc('config', 'user.email', 'qe@example.com');
  gitc('config', 'user.name', 'QE Test');
  gitc('add', '-A');
  gitc('commit', '-qm', 'init');
});

after(() => {
  fs.rmSync(repo, { recursive: true, force: true });
  for (const suffix of ['', '.calls.json', '.prompts.log']) fs.rmSync(scriptFile + suffix, { force: true });
});

test('workflow 1: plan loops with a fresh verifier until 10/10, logs denials, and goes stale when the story changes', () => {
  agentScript({
    'qe-plan': [{ writePlan: [happySad[0]] }, { writePlan: happySad }],
    'qe-plan-verifier': [
      { result: { score: 7, summary: 'no auth case', gaps: [{ case: null, category: 'missing-sad-path', subtype: 'auth', detail: 'no unauthenticated case' }] } },
      { result: { score: 10, summary: 'complete', gaps: [] } },
    ],
  });
  const result = qe(['plan', 'DEMO-1']);
  assert.equal(result.status, 0, result.stderr);

  // Round 1: deterministic precheck (no sad case) fails before any verifier call.
  // Round 2: verifier denies 7/10. Round 3: verifier passes 10/10.
  assert.deepEqual(calls(), { 'qe-plan': 3, 'qe-plan-verifier': 2 });
  assert.match(jsonl('precheck-failures.jsonl')[0].errors.join('\n'), /no sad-path case/);
  const denials = jsonl('verifier-denials.jsonl');
  assert.equal(denials.length, 1);
  assert.deepEqual([denials[0].category, denials[0].subtype, denials[0].attributed_agent, denials[0].round], ['missing-sad-path', 'auth', 'qe-plan', 2]);
  assert.deepEqual(jsonl('verifier-runs.jsonl').map((r) => [r.score, r.passed]), [[7, false], [10, true]]);
  assert.match(prompts(), /Fix every gap:\n1\. \[missing-sad-path\/auth\] \(plan-wide\): no unauthenticated case/);
  // Every agent call is a separate child process flagged so hooks inside it stay quiet.
  assert.doesNotMatch(prompts(), /child=undefined/);

  const index = read(`${PLAN}/_index.md`);
  assert.match(index, /verified-score: 10/);
  assert.match(qe(['check', '--all']).stdout, /ok +DEMO-1 +plan=verified/);

  gitc('add', '-A');
  gitc('commit', '-qm', 'plan');
  gitc('checkout', '-qb', 'feature/DEMO-1');
  write(STORY, `${read(STORY)}\nRule: coupons cannot be combined (409).\n`);
  gitc('commit', '-qam', 'story change');

  const check = qe(['check', '--changed-from', 'main']);
  assert.equal(check.status, 1);
  assert.match(check.stdout, /FAIL DEMO-1 +plan=stale/);
  const prePush = qe(['check', '--pre-push'], { input: `refs/heads/feature/DEMO-1 ${gitc('rev-parse', 'HEAD').stdout.trim()} refs/heads/feature/DEMO-1 0000000000000000000000000000000000000000\n` });
  assert.equal(prePush.status, 1);
  assert.equal(qe(['check', '--pre-push'], { input: 'x', env: { QE_SKIP: '1' } }).status, 0);

  // Update in place: the agent gets the story diff.
  agentScript({ 'qe-plan': [{ writePlan: [...happySad, { title: 'Combined coupons rejected', type: 'sad' }] }], 'qe-plan-verifier': [{ result: { score: 10, gaps: [] } }] });
  assert.equal(qe(['plan', '--stale']).status, 0);
  assert.match(prompts(), /Mode: update \(story changed since the plan was written\)/);
  assert.match(prompts(), /\+Rule: coupons cannot be combined \(409\)\./);
  gitc('add', '-A');
  gitc('commit', '-qm', 'replan');
  assert.equal(qe(['check', '--changed-from', 'main']).status, 0);
});

test('workflow 2: Stop hook gates on review, then automation loops to 10/10 and routes plan-level gaps back to the plan agent', async () => {
  const blocking = { verdict: 'CHANGES_REQUESTED', summary: 'bypasses ownership check', findings: [{ severity: 'blocking', category: 'security', file: 'src/Basket/Coupons.cs', line: 1, issue: 'no owner check', fix: 'check buyer id' }] };
  agentScript({
    'architecture-reviewer': [{ result: blocking }, { result: { verdict: 'APPROVED', summary: 'ok', findings: [] } }],
    'qe-automation': [{ writeTests: [1], result: { status: 'done' } }, { writeTests: [1, 2, 3], result: { status: 'done' } }],
    'qe-automation-verifier': [
      { result: { score: 6, gaps: [{ case: 'TC-DEMO-1-02', category: 'missing-assertion', detail: 'status only', location: 'e2e/teststack/demo-1.spec.ts:2' }, { case: 'TC-DEMO-1-03', category: 'tests-different-path', root_stage: 'plan', detail: 'case does not say which coupons' }] } },
      { result: { score: 10, gaps: [] } },
    ],
    'qe-plan': [{ writePlan: [...happySad, { title: 'Combined coupons rejected', type: 'sad', expected: '409 with code COUPON_COMBINE' }] }],
    'qe-plan-verifier': [{ result: { score: 10, gaps: [] } }],
  });
  const stop = (payload = {}, env) => {
    const r = qe(['hook', 'stop'], { input: JSON.stringify(payload), env });
    assert.equal(r.status, 0, r.stderr);
    return r.stdout ? JSON.parse(r.stdout) : null;
  };

  assert.equal(qe(['start', 'DEMO-1']).status, 0);
  const context = JSON.parse(qe(['hook', 'session-start']).stdout);
  assert.match(context.additionalContext, /active story DEMO-1/, 'Copilot reads only the top-level key');
  assert.equal(context.hookSpecificOutput.additionalContext, context.additionalContext);
  assert.deepEqual(Object.keys(JSON.parse(qe(['hook', 'session-start', '--nested']).stdout)), ['hookSpecificOutput'], 'Codex rejects unknown keys');
  write('src/Basket/Coupons.cs', '// coupons v2\n');
  assert.equal(stop(), null, 'no review until the agent declares the work done');
  assert.equal(stop({}, { QE_PIPELINE_CHILD: '1' }), null);
  assert.equal(qe(['done']).status, 0);
  assert.equal(stop({}, { QE_PIPELINE_CHILD: '1' }), null, 'pipeline child agents never trigger the hook');

  const first = stop();
  assert.equal(first.decision, 'block');
  assert.match(first.reason, /round 1\/5 .*requested changes[\s\S]*no owner check[\s\S]*Fix: check buyer id/);
  assert.equal(stop().decision, 'block', 'blocks once more if nothing changed');
  assert.match(stop().systemMessage, /still has unresolved architecture findings/);
  assert.equal(calls()['architecture-reviewer'], 1, 'cached verdict, no re-review for the same code');

  write('src/Basket/Coupons.cs', '// coupons v3 with owner check\n');
  const approved = stop({ stop_hook_active: true });
  assert.match(approved.systemMessage, /approved DEMO-1 \(round 2\)\. Test automation started/);
  await waitFor(() => !['starting', 'running'].includes(stateOf('DEMO-1').automation.status));

  const state = stateOf('DEMO-1');
  assert.equal(state.automation.status, 'verified', read(state.automation.log));
  assert.deepEqual(calls(), { 'architecture-reviewer': 2, 'qe-automation': 3, 'qe-automation-verifier': 2, 'qe-plan': 1, 'qe-plan-verifier': 1 });
  assert.match(jsonl('precheck-failures.jsonl').at(-1).errors.join('\n'), /TC-DEMO-1-02 has no automated test/);

  const autoDenials = jsonl('verifier-denials.jsonl').filter((d) => d.stage === 'automation');
  assert.deepEqual(autoDenials.map((d) => [d.case, d.attributed_agent, d.root_stage]), [
    ['TC-DEMO-1-02', 'qe-automation', 'automation'],
    ['TC-DEMO-1-03', 'qe-plan', 'plan'],
  ]);
  assert.match(prompts(), /traced these problems to the test cases themselves:\n1\. \[tests-different-path\] TC-DEMO-1-03/);
  assert.deepEqual(jsonl('arch-reviews.jsonl').map((r) => r.verdict), ['CHANGES_REQUESTED', 'APPROVED']);

  assert.match(read(`${PLAN}/_index.md`), /automation-score: 10/);
  assert.match(qe(['status', 'DEMO-1']).stdout, /plan=verified .*tests=1 +automation=verified\/verified review=APPROVED/);
  assert.equal(stop(), null, 'approved and verified: nothing left to do');

  // Editing a verified test invalidates the stamp so CI and pre-push catch it.
  write('e2e/teststack/demo-1.spec.ts', `${read('e2e/teststack/demo-1.spec.ts')}// tweak\n`);
  assert.match(qe(['check', '--all']).stdout, /FAIL DEMO-1 .*tests or scenario edited after verification/);

  const metrics = JSON.parse(qe(['metrics', '--json']).stdout);
  assert.equal(metrics.crossStage.automationDenialsCausedByPlan, 1);
  assert.equal(metrics.stages.automation.verifierRounds, 2);
});
