import { spawn } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { agentDefinition, runAgent } from './agents.mjs';
import { QE_SCRIPT } from './config.mjs';
import { git, implChanges, implDiff } from './git.mjs';
import {
  INDEX,
  automationFiles,
  automationSha,
  planContentSha,
  planStatus,
  precheckPlan,
  readPlan,
  stampIndex,
} from './plans.mjs';
import { lockFile, logFile, loadState, stateDir, updateState } from './state.mjs';
import { casePrefix, storySha } from './stories.mjs';
import { acquireLock, appendJsonl, extractResult, lockHolder, log, nowIso, releaseLock, run, tail } from './util.mjs';
import { PRODUCER, formatGaps, metricsFile, runVerifier } from './verify.mjs';

const fence = (text, lang = '') => `\`\`\`\`${lang}\n${text.replace(/\r\n/g, '\n').trimEnd()}\n\`\`\`\``;
const readRel = (config, rel) => fs.readFileSync(path.join(config.root, rel), 'utf8');

function langFor(file) {
  if (file.endsWith('.cs')) return 'csharp';
  if (file.endsWith('.ts')) return 'ts';
  if (file.endsWith('.yaml') || file.endsWith('.yml')) return 'yaml';
  if (file.endsWith('.sql')) return 'sql';
  return '';
}

function logPrecheck(config, stage, story, round, errors) {
  appendJsonl(metricsFile(config, 'precheck-failures.jsonl'), {
    ts: nowIso(),
    stage,
    story: story.key,
    round,
    agent_prompt_ver: agentDefinition(config, PRODUCER[stage]).version,
    errors,
  });
}

export function escalate(config, story, stage, title, details) {
  const file = path.join(stateDir(config), 'escalations', `${story.key}-${stage}.md`);
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, `# ${story.key} ${stage}: ${title}\n\n${nowIso()}\n\n${details}\n`);
  log(`ESCALATED ${story.key} ${stage}: ${title} (see ${path.relative(config.root, file)})`);
  return file;
}

function clearEscalation(config, story, stage) {
  fs.rmSync(path.join(stateDir(config), 'escalations', `${story.key}-${stage}.md`), { force: true });
}

// Finds the committed version of the story the plan was written against and diffs it with the
// current story, so an update can edit the affected cases in place instead of rewriting the plan.
export function storyDiffSince(config, story, oldSha) {
  if (!oldSha) return null;
  const revs = git(config.root, ['log', '--format=%H', '-n', '50', '--', story.file]).stdout.split('\n').filter(Boolean);
  for (const rev of revs) {
    const text = git(config.root, ['show', `${rev}:${story.file}`]);
    if (text.status === 0 && storySha(text.stdout) === oldSha) {
      return git(config.root, ['diff', '--no-ext-diff', rev, '--', story.file]).stdout || null;
    }
  }
  return null;
}

// ---------------------------------------------------------------------------------------------
// Workflow 1: story -> integration test plan, verified by a fresh verifier until it scores 10/10
// ---------------------------------------------------------------------------------------------

function planTask(config, story, { mode, reasons, storyDiff, feedback }) {
  const lines = [
    `Story: ${story.key} (${story.file})`,
    `Plan directory: ${story.planDir}`,
    `Mode: ${mode}${reasons?.length ? ` (${reasons.join('; ')})` : ''}`,
    `Case id format: ${casePrefix(story.key)}NN (two or more digits)`,
    `Allowed sad-path values: ${config.categories.sadPathSubtypes.join(', ')}`,
    '',
    '### Story',
    '',
    fence(readRel(config, story.file), 'markdown'),
  ];
  if (mode === 'update') {
    lines.push('', '### What changed in the story since the plan was written', '');
    lines.push(storyDiff ? fence(storyDiff, 'diff') : 'No diff is available; compare the story with the existing case files yourself.');
    lines.push('', 'Update the existing plan in place: keep the ids of cases that still apply, edit the ones affected by the change, delete cases the story no longer supports, and add new cases with the next unused number.');
  }
  if (feedback) lines.push('', '### Fix every item from the previous round', '', feedback);
  return lines.join('\n');
}

function planVerifierTask(config, story, plan) {
  const lines = [
    `Story: ${story.key}`,
    `Required score: ${config.loops.requiredScore}/10`,
    `Allowed categories: ${config.categories.plan.join(', ')}`,
    `Allowed missing-sad-path subtypes: ${config.categories.sadPathSubtypes.join(', ')}`,
    '',
    '### Story',
    '',
    fence(readRel(config, story.file), 'markdown'),
    '',
    `### Plan index (${story.planDir}/${INDEX})`,
    '',
    fence(plan.index?.body ?? '(missing)', 'markdown'),
    '',
    '### Case files',
  ];
  for (const c of plan.cases) lines.push('', `#### ${c.file}`, '', fence(c.text, 'markdown'));
  return lines.join('\n');
}

export async function planLoop(config, story, { force = false, seedFeedback = null, maxRounds = config.loops.planMaxRounds } = {}) {
  const lock = lockFile(config, story.key, 'plan');
  if (!acquireLock(lock)) {
    log(`${story.key}: plan run already in progress (pid ${lockHolder(lock)}).`);
    return { status: 'busy' };
  }
  try {
    const initial = planStatus(config, story);
    if (initial.state === 'verified' && !force && !seedFeedback) {
      log(`${story.key}: plan is verified and up to date.`);
      return { status: 'verified' };
    }
    updateState(config, story.key, (s) => Object.assign(s.plan, { status: 'running', pid: process.pid, startedAt: nowIso() }));
    const mode = initial.state === 'missing' ? 'create' : 'update';
    const storyDiff = mode === 'update' ? storyDiffSince(config, story, initial.plan.index?.data['story-sha']) : null;
    let feedback = seedFeedback;
    let consecutive = 0;
    let lastGaps = [];
    for (let round = 1; round <= maxRounds; round++) {
      log(`${story.key}: plan round ${round}/${maxRounds} (${mode})`);
      const producer = runAgent(config, {
        name: PRODUCER.plan,
        mode: 'write',
        timeoutSec: config.timeoutsSec.plan,
        task: planTask(config, story, { mode: round === 1 ? mode : 'update', reasons: initial.reasons, storyDiff, feedback }),
      });
      if (!producer.ok) {
        feedback = `The previous run ${producer.timedOut ? 'timed out' : `failed (exit ${producer.status})`}. Finish the plan.`;
        continue;
      }
      stampIndex(config, story, { 'plan-sha': undefined, 'verified-score': undefined, 'verified-at': undefined, 'verified-by': undefined });
      const pre = precheckPlan(config, story);
      if (pre.errors.length) {
        logPrecheck(config, 'plan', story, round, pre.errors);
        feedback = `Structural check failed:\n${pre.errors.map((e) => `- ${e}`).join('\n')}`;
        continue;
      }
      let verdict;
      do {
        verdict = runVerifier(config, {
          stage: 'plan',
          story,
          round,
          producerVersion: producer.version,
          task: planVerifierTask(config, story, readPlan(config, story)),
        });
        if (verdict.error) break;
        consecutive = verdict.passed ? consecutive + 1 : 0;
      } while (verdict.passed && consecutive < config.loops.consecutivePasses);
      if (verdict.error) {
        feedback = null;
        continue;
      }
      if (verdict.passed) {
        const plan = readPlan(config, story);
        stampIndex(config, story, {
          'plan-sha': planContentSha(plan),
          'verified-score': verdict.score,
          'verified-at': nowIso(),
          'verified-by': `${PRODUCER.plan}@${producer.version}/${verdict.verifierVersion}`,
        });
        clearEscalation(config, story, 'plan');
        updateState(config, story.key, (s) => Object.assign(s.plan, { status: 'verified', pid: null, rounds: round, finishedAt: nowIso() }));
        log(`${story.key}: plan verified ${verdict.score}/10 after ${round} round(s).`);
        return { status: 'verified', rounds: round };
      }
      lastGaps = verdict.gaps;
      feedback = `The verifier scored the plan ${verdict.score}/10. Fix every gap:\n${formatGaps(verdict.gaps)}`;
    }
    escalate(config, story, 'plan', `plan did not reach ${config.loops.requiredScore}/10 in ${maxRounds} rounds`, lastGaps.length ? formatGaps(lastGaps) : feedback ?? '');
    updateState(config, story.key, (s) => Object.assign(s.plan, { status: 'escalated', pid: null, finishedAt: nowIso() }));
    return { status: 'escalated' };
  } finally {
    releaseLock(lock);
  }
}

// ---------------------------------------------------------------------------------------------
// Workflow 2: verified plan -> scenario + automated tests, verified until 10/10
// ---------------------------------------------------------------------------------------------

function expand(template, values) {
  return template.flatMap((part) => {
    if (part === '{files}') return values.files ?? [];
    return [part.replace(/\{(\w+)\}/g, (_, key) => values[key] ?? '')];
  });
}

export function precheckAutomation(config, story, { skipTests = false } = {}) {
  const errors = [];
  const status = planStatus(config, story);
  if (status.state !== 'verified') errors.push(`Plan for ${story.key} is ${status.state}: ${status.reasons.join('; ')}.`);
  const planIds = new Set(status.plan.cases.map((c) => c.data.id).filter(Boolean));
  const { tests, scenarioDir, scenario } = automationFiles(config, story);
  const covered = new Set([...tests.values()].flatMap((ids) => [...ids]));
  for (const id of planIds) if (!covered.has(id)) errors.push(`${id} has no automated test (put the id in the test name).`);
  for (const id of covered) if (!planIds.has(id)) errors.push(`${id} is referenced by tests but is not in the plan (remove or rename it).`);
  if (!scenario.some((f) => /scenario\.ya?ml$/.test(f))) errors.push(`Missing scenario: create ${scenarioDir}/scenario.yaml.`);

  for (const [file] of tests) {
    if (file.endsWith('.cs') && !new RegExp(`Trait\\(\\s*"Story"\\s*,\\s*"${story.key}"\\s*\\)`).test(readRel(config, file))) {
      errors.push(`${file}: tag the story's tests with [Trait("Story", "${story.key}")] so they can be filtered.`);
    }
  }
  if (errors.length) return { errors, tests };

  const validate = config.precheck.validateScenario;
  if (validate?.length && fs.existsSync(path.join(config.root, validate[0]))) {
    const result = run(validate[0], expand(validate.slice(1), { scenario: story.scenario }), { cwd: config.root, timeoutSec: 600 });
    if (result.status !== 0) errors.push(`Scenario validation failed:\n${tail(result.stdout + result.stderr, 3000)}`);
  }
  if (skipTests || !config.precheck.runTests || errors.length) return { errors, tests };

  const specFiles = [...tests.keys()].filter((f) => f.endsWith('.ts'));
  const projects = [...new Set([...tests.keys()].filter((f) => f.endsWith('.cs')).map((f) => f.split('/').slice(0, 2).join('/')))];
  const commands = [];
  if (specFiles.length) commands.push(expand(config.precheck.playwright, { files: specFiles, casePrefix: casePrefix(story.key), story: story.key }));
  for (const project of projects) commands.push(expand(config.precheck.dotnet, { project, story: story.key }));
  for (const [cmd, ...args] of commands) {
    log(`${story.key}: running ${cmd} ${args.join(' ')}`);
    const result = run(cmd, args, { cwd: config.root, timeoutSec: config.timeoutsSec.tests });
    if (result.status !== 0) errors.push(`\`${cmd} ${args.join(' ')}\` failed:\n${tail(result.stdout + result.stderr, 4000)}`);
  }
  return { errors, tests };
}

function automationTask(config, story, { mode, feedback, implFiles }) {
  const plan = readPlan(config, story);
  const lines = [
    `Story: ${story.key} (${story.file})`,
    `Mode: ${mode}`,
    `Verified plan: ${story.planDir} (${plan.cases.length} cases: ${plan.cases.map((c) => c.data.id).join(', ')})`,
    `Scenario directory: ${config.scenariosDir}/${story.scenario} (name: ${story.scenario})`,
    `Every test name must contain its case id; .NET tests also need [Trait("Story", "${story.key}")] and [Trait("TestCase", "<id>")].`,
    '',
    '### Implementation files changed for this story',
    '',
    implFiles.length ? implFiles.map((f) => `- ${f}`).join('\n') : '- (none detected; read the code the plan exercises)',
  ];
  if (mode === 'update') {
    lines.push('', 'Tests for this story already exist. Update them in place so they match the current plan and implementation; do not duplicate tests.');
  }
  if (feedback) lines.push('', '### Fix every item from the previous round', '', feedback);
  return lines.join('\n');
}

function automationVerifierTask(config, story, tests) {
  const plan = readPlan(config, story);
  const { scenario } = automationFiles(config, story);
  const lines = [
    `Story: ${story.key}`,
    `Required score: ${config.loops.requiredScore}/10`,
    `Allowed categories: ${config.categories.automation.join(', ')}`,
    'Set "root_stage": "plan" on a gap only when the test case itself is wrong or untestable, not the test.',
    '',
    '### Test cases (the specification the tests must match)',
  ];
  for (const c of plan.cases) lines.push('', `#### ${c.file}`, '', fence(c.text, 'markdown'));
  lines.push('', '### Automated tests');
  for (const [file, ids] of tests) lines.push('', `#### ${file} (references ${[...ids].join(', ')})`, '', fence(readRel(config, file), langFor(file)));
  lines.push('', `### Scenario data (${config.scenariosDir}/${story.scenario})`);
  for (const file of scenario) lines.push('', `#### ${file}`, '', fence(readRel(config, file), langFor(file)));
  lines.push('', 'Helper modules imported by the tests may be opened with your read tools if you need them.');
  return lines.join('\n');
}

export const planVerifierTaskFor = (config, story) => planVerifierTask(config, story, readPlan(config, story));
export const automationVerifierTaskFor = (config, story) => automationVerifierTask(config, story, automationFiles(config, story).tests);

export async function automationLoop(config, story, { implHash = null, skipTests = false, maxRounds = config.loops.automationMaxRounds } = {}) {
  const lock = lockFile(config, story.key, 'automation');
  if (!acquireLock(lock)) {
    updateState(config, story.key, (s) => (s.automation.requestedHash = implHash ?? s.automation.requestedHash));
    log(`${story.key}: automation already running (pid ${lockHolder(lock)}); queued a follow-up for the latest changes.`);
    return { status: 'busy' };
  }
  try {
    let targetHash = implHash ?? implChanges(config).hash;
    for (;;) {
      updateState(config, story.key, (s) => Object.assign(s.automation, { status: 'running', pid: process.pid, requestedHash: null, workingHash: targetHash, startedAt: nowIso(), bugs: [] }));
      const result = await automationPass(config, story, { skipTests, maxRounds });
      const state = updateState(config, story.key, (s) => {
        Object.assign(s.automation, { status: result.status, pid: null, finishedAt: nowIso() });
        if (result.status === 'verified') s.automation.verifiedHash = targetHash;
        if (result.bugs) s.automation.bugs = result.bugs;
      });
      const next = state.automation.requestedHash;
      if (!next || next === targetHash || result.status === 'product-bug') return result;
      log(`${story.key}: implementation changed while tests were being written; updating tests.`);
      targetHash = next;
    }
  } finally {
    releaseLock(lock);
  }
}

async function automationPass(config, story, { skipTests, maxRounds }) {
  const status = planStatus(config, story);
  if (status.state !== 'verified') {
    log(`${story.key}: plan is ${status.state}; running the plan loop first.`);
    const plan = await planLoop(config, story);
    if (plan.status !== 'verified') return { status: 'blocked-on-plan' };
  }
  const implFiles = implChanges(config).files;
  let feedback = null;
  let lastGaps = [];
  for (let round = 1; round <= maxRounds; round++) {
    const mode = automationFiles(config, story).tests.size ? 'update' : 'create';
    log(`${story.key}: automation round ${round}/${maxRounds} (${mode})`);
    const producer = runAgent(config, {
      name: PRODUCER.automation,
      mode: 'write',
      timeoutSec: config.timeoutsSec.automation,
      task: automationTask(config, story, { mode, feedback, implFiles }),
    });
    const reported = extractResult(producer.output);
    if (reported?.status === 'product-bug') {
      const bugs = Array.isArray(reported.bugs) ? reported.bugs : [];
      escalate(config, story, 'automation', 'tests found implementation bugs', bugs.map((b) => `- ${typeof b === 'string' ? b : JSON.stringify(b)}`).join('\n'));
      return { status: 'product-bug', bugs };
    }
    if (!producer.ok) {
      feedback = `The previous run ${producer.timedOut ? 'timed out' : `failed (exit ${producer.status})`}. Finish the tests.`;
      continue;
    }
    const pre = precheckAutomation(config, story, { skipTests });
    if (pre.errors.length) {
      logPrecheck(config, 'automation', story, round, pre.errors);
      feedback = `Deterministic checks failed:\n${pre.errors.map((e) => `- ${e}`).join('\n')}`;
      continue;
    }
    let verdict;
    let consecutive = 0;
    do {
      verdict = runVerifier(config, { stage: 'automation', story, round, producerVersion: producer.version, task: automationVerifierTask(config, story, pre.tests) });
      if (verdict.error) break;
      consecutive = verdict.passed ? consecutive + 1 : 0;
    } while (verdict.passed && consecutive < config.loops.consecutivePasses);
    if (verdict.error) continue;
    if (verdict.passed) {
      stampIndex(config, story, {
        'automation-sha': automationSha(config, story),
        'automation-plan-sha': readPlan(config, story).index.data['plan-sha'],
        'automation-score': verdict.score,
        'automation-verified-at': nowIso(),
      });
      clearEscalation(config, story, 'automation');
      log(`${story.key}: automated tests verified ${verdict.score}/10 after ${round} round(s).`);
      return { status: 'verified', rounds: round };
    }
    lastGaps = verdict.gaps;
    const planGaps = verdict.gaps.filter((g) => g.root_stage === 'plan');
    if (planGaps.length) {
      log(`${story.key}: ${planGaps.length} gap(s) trace back to the plan; sending them to the plan agent.`);
      const plan = await planLoop(config, story, { seedFeedback: `The automation verifier traced these problems to the test cases themselves:\n${formatGaps(planGaps)}` });
      if (plan.status !== 'verified') return { status: 'blocked-on-plan' };
    }
    feedback = `The verifier scored the tests ${verdict.score}/10. Fix every gap:\n${formatGaps(verdict.gaps.filter((g) => g.root_stage !== 'plan'))}${planGaps.length ? '\n\nThe plan was also updated to fix case-level problems; re-read the case files.' : ''}`;
  }
  escalate(config, story, 'automation', `tests did not reach ${config.loops.requiredScore}/10 in ${maxRounds} rounds`, lastGaps.length ? formatGaps(lastGaps) : feedback ?? '');
  return { status: 'escalated' };
}

// ---------------------------------------------------------------------------------------------
// Architecture review of the implementation (fresh reviewer per round)
// ---------------------------------------------------------------------------------------------

function reviewTask(config, story, round) {
  const conventions = ['AGENTS.md', 'CONTRIBUTING.md'].filter((f) => fs.existsSync(path.join(config.root, f)));
  return [
    `Story: ${story.key} (${story.file})`,
    `Review round: ${round} of at most ${config.loops.reviewMaxRounds}`,
    `Repository conventions to apply: ${conventions.join(', ') || '(none)'}; graphify-out/ may be queried with \`graphify query\`.`,
    '',
    '### Story',
    '',
    fence(readRel(config, story.file), 'markdown'),
    '',
    '### Implementation diff (tests and plans excluded)',
    '',
    fence(implDiff(config), 'diff'),
  ].join('\n');
}

export function architectureReview(config, story, round) {
  for (let attempt = 0; attempt <= config.loops.verifierParseRetries; attempt++) {
    const result = runAgent(config, {
      name: 'architecture-reviewer',
      mode: 'read',
      model: config.agent.models.reviewer,
      timeoutSec: config.timeoutsSec.reviewer,
      task: reviewTask(config, story, round),
    });
    const raw = extractResult(result.output);
    if (raw && ['APPROVED', 'CHANGES_REQUESTED'].includes(raw.verdict)) {
      const findings = Array.isArray(raw.findings) ? raw.findings : [];
      const verdict = raw.verdict === 'APPROVED' && findings.some((f) => f.severity === 'blocking') ? 'CHANGES_REQUESTED' : raw.verdict;
      appendJsonl(metricsFile(config, 'arch-reviews.jsonl'), {
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

// ---------------------------------------------------------------------------------------------
// Background runs
// ---------------------------------------------------------------------------------------------

export function spawnBackground(config, key, stage, args, file = logFile(config, key, stage)) {
  fs.mkdirSync(path.dirname(file), { recursive: true });
  const fd = fs.openSync(file, 'a');
  const env = { ...process.env, QE_BACKGROUND: '1' };
  delete env.QE_PIPELINE_CHILD;
  const child = spawn(process.execPath, [QE_SCRIPT, ...args], { cwd: config.root, detached: true, stdio: ['ignore', fd, fd], env });
  child.unref();
  fs.closeSync(fd);
  return { pid: child.pid, log: path.relative(config.root, file) };
}

export function startAutomation(config, story, implHash) {
  const holder = lockHolder(lockFile(config, story.key, 'automation'));
  if (holder) {
    updateState(config, story.key, (s) => (s.automation.requestedHash = implHash));
    return { queued: true, pid: holder };
  }
  // Saved before spawning: the child updates the same state file (and records its own pid), so a
  // write after the spawn could race it and drop the log path or reset the status.
  const file = logFile(config, story.key, 'automation');
  const log = path.relative(config.root, file);
  updateState(config, story.key, (s) => Object.assign(s.automation, { status: 'starting', pid: null, log, requestedHash: null }));
  return spawnBackground(config, story.key, 'automation', ['automate', story.key, '--impl-hash', implHash], file);
}

// ---------------------------------------------------------------------------------------------
// Agent lifecycle hook: implementation agent Stop / SubagentStop
// ---------------------------------------------------------------------------------------------

const block = (reason) => ({ decision: 'block', reason });
const notice = (message) => ({ systemMessage: message });

export function hookStop(config, story, payload = {}) {
  const state = loadState(config, story.key);
  if (!state.done) return null;
  const { hash } = implChanges(config);
  if (!hash) return null;
  const review = state.review;
  const cmd = 'node scripts/qe/qe.mjs';

  if (review.lastHash === hash) {
    if (review.lastVerdict === 'APPROVED') {
      if (state.automation.verifiedHash !== hash && !['running', 'starting'].includes(state.automation.status) && state.automation.workingHash !== hash) {
        const bg = startAutomation(config, story, hash);
        return notice(`QE: architecture approved ${story.key}; test automation ${bg.queued ? 'queued' : `started (log: ${bg.log})`}.`);
      }
      return null;
    }
    if (review.lastVerdict === 'CHANGES_REQUESTED') {
      if (payload.stop_hook_active || review.unchangedBlocks >= 1) {
        return notice(`QE: ${story.key} still has unresolved architecture findings and the code has not changed. Run \`${cmd} status ${story.key}\` for details.`);
      }
      updateState(config, story.key, (s) => (s.review.unchangedBlocks += 1));
      return block(`The architecture review findings for ${story.key} are still open and nothing has changed since the review. Address them:\n\n${review.lastReason}`);
    }
    if (review.lastVerdict === 'ESCALATED') return null;
  }

  if (review.rounds >= config.loops.reviewMaxRounds) {
    updateState(config, story.key, (s) => Object.assign(s.review, { lastHash: hash, lastVerdict: 'ESCALATED' }));
    escalate(config, story, 'review', `no approval after ${review.rounds} review rounds`, review.lastReason ?? '');
    return notice(`QE: ${story.key} was not approved after ${review.rounds} architecture review rounds. A human needs to decide; see .agent-state/escalations/${story.key}-review.md.`);
  }

  const round = review.rounds + 1;
  const result = architectureReview(config, story, round);
  if (result.verdict === 'ERROR') {
    return notice(`QE: the architecture review for ${story.key} failed to produce a verdict; it will run again next time the agent stops.`);
  }
  if (result.verdict === 'CHANGES_REQUESTED') {
    const reason = `Architecture review round ${round}/${config.loops.reviewMaxRounds} for ${story.key} requested changes. ${result.summary}\n\n${formatFindings(result.findings)}\n\nMake these corrections, then finish your turn; the review runs again automatically.`;
    updateState(config, story.key, (s) => Object.assign(s.review, { rounds: round, lastHash: hash, lastVerdict: 'CHANGES_REQUESTED', lastReason: reason, unchangedBlocks: 0 }));
    return block(reason);
  }
  updateState(config, story.key, (s) => Object.assign(s.review, { rounds: 0, lastHash: hash, lastVerdict: 'APPROVED', lastReason: result.summary, approvedHash: hash, unchangedBlocks: 0 }));
  clearEscalation(config, story, 'review');
  const bg = startAutomation(config, story, hash);
  const extra = result.findings.length ? `\nNon-blocking notes:\n${formatFindings(result.findings)}` : '';
  return notice(`QE: architecture review approved ${story.key} (round ${round}). Test automation ${bg.queued ? 'queued behind the running job' : `started in the background (log: ${bg.log})`}.${extra}`);
}

// Context injected at session start so agents see pipeline state without asking.
export function sessionContext(config, story, stories) {
  const lines = [];
  if (story) {
    const plan = planStatus(config, story);
    const state = loadState(config, story.key);
    lines.push(`QE pipeline: active story ${story.key}. Plan: ${plan.state}. Architecture review: ${state.review.lastVerdict ?? 'not run'}. Test automation: ${state.automation.status}.`);
    if (!state.done) lines.push(`When your line of work for ${story.key} is complete, run \`node scripts/qe/qe.mjs done\` before finishing so the architecture review and test automation run.`);
    if (state.automation.bugs?.length) lines.push(`QE found implementation bugs:\n${state.automation.bugs.map((b) => `- ${typeof b === 'string' ? b : JSON.stringify(b)}`).join('\n')}`);
  }
  const escalations = path.join(stateDir(config), 'escalations');
  if (fs.existsSync(escalations)) {
    const files = fs.readdirSync(escalations).filter((f) => f.endsWith('.md'));
    if (files.length) lines.push(`QE escalations needing a human: ${files.map((f) => f.replace(/\.md$/, '')).join(', ')} (.agent-state/escalations/).`);
  }
  const stale = stories.filter((s) => planStatus(config, s).state === 'stale').map((s) => s.key);
  if (stale.length) lines.push(`Stories whose test plans are stale: ${stale.join(', ')} (\`node scripts/qe/qe.mjs plan --stale\`).`);
  return lines.join('\n');
}
