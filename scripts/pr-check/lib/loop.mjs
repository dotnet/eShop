// The adversarial PR check loop. Each round: critics review in parallel, the defender rebuts, the
// critics answer (debateTurns times), the judge rules and the round is scored. Below 10/10 the fixer
// fixes the upheld findings, the script commits them, and a new round with fresh agents starts.
import fs from 'node:fs';
import path from 'node:path';
import { runAgentAsync } from '../../pipeline/lib/agents.mjs';
import { escalate, metricsFile } from '../../pipeline/lib/state.mjs';
import { appendJsonl, extractResult, log, nowIso, sha, writeJson } from '../../pipeline/lib/util.mjs';
import { commitFixes, prDiff } from './context.mjs';

export const LENSES = {
  acceptance: { agent: 'pr-check-acceptance', code: 'ACC', title: 'Acceptance criteria' },
  flow: { agent: 'pr-check-flow', code: 'FLOW', title: 'End-to-end flow' },
  design: { agent: 'pr-check-design', code: 'UI', title: 'UI design vs mockup' },
  style: { agent: 'pr-check-style', code: 'STY', title: 'Code style and guidelines' },
  video: { agent: 'pr-check-video', code: 'VID', title: 'Video evidence' },
};
const SEVERITIES = ['blocking', 'major', 'minor'];
const CAP = { blocking: 6, major: 8, minor: 9 };
export const CHECKS_FILE = 'pr-checks.jsonl';

const rel = (ctx, p) => path.relative(ctx.workRoot, p).replace(/\\/g, '/');
const json = (value) => JSON.stringify(value, null, 2);

export function activeLenses(ctx) {
  const hasVideo = ctx.video.videos.length > 0 || ctx.video.unreadable.length > 0;
  return ['acceptance', 'flow', ...(ctx.isFrontend ? ['design'] : []), 'style', ...(hasVideo ? ['video'] : [])];
}

export function modelFor(config, role) {
  return process.env[`PR_CHECK_MODEL_${role.toUpperCase()}`] || config.prCheck.models?.[config.agent.cli]?.[role] || null;
}

// Scoring is deterministic so an agent cannot talk its way to 10: any upheld finding caps its lens
// (blocking 6, major 8, minor 9), a lens whose critic failed scores 0, and the PR scores the lowest lens.
// A finding the judge did not rule on stands unless its critic withdrew it.
export function scoreRound({ lenses, findings, rulings = [], judgeLensScores = {}, failedLenses = [] }) {
  const byId = new Map(rulings.filter((r) => r && r.id).map((r) => [String(r.id), r]));
  const upheld = [];
  for (const f of findings) {
    const r = byId.get(f.id);
    if (r) {
      if (r.ruling === 'upheld') upheld.push({ ...f, severity: SEVERITIES.includes(r.severity) ? r.severity : f.severity, reason: r.reason ?? '', fix: r.fix || f.fix });
    } else if (f.status !== 'withdrawn') {
      upheld.push({ ...f, reason: 'The judge gave no ruling, so the finding stands.' });
    }
  }
  const known = new Set(findings.map((f) => f.id));
  for (const r of rulings) {
    if (r?.ruling !== 'upheld' || known.has(String(r.id))) continue;
    const lens = lenses.find((l) => String(r.id).toLowerCase().startsWith(l));
    if (!lens) continue;
    upheld.push({ id: String(r.id), lens, severity: SEVERITIES.includes(r.severity) ? r.severity : 'major', title: 'Coverage gap', detail: r.reason ?? '', evidence: '', requirement: '', fix: r.fix ?? '', reason: r.reason ?? '' });
  }
  const lensScores = {};
  for (const lens of lenses) {
    if (failedLenses.includes(lens)) {
      lensScores[lens] = 0;
      continue;
    }
    const mine = upheld.filter((f) => f.lens === lens);
    if (!mine.length) {
      lensScores[lens] = 10;
      continue;
    }
    const cap = Math.min(...mine.map((f) => CAP[f.severity] ?? CAP.major));
    const judged = Number(judgeLensScores[lens]);
    lensScores[lens] = Math.max(0, Math.min(cap, Number.isFinite(judged) ? Math.round(judged) : cap));
  }
  return { score: Math.min(...Object.values(lensScores)), lensScores, upheld };
}

function normalizeFinding(raw, lens, id) {
  const text = (v) => (v == null ? '' : String(v));
  return {
    id,
    lens,
    severity: SEVERITIES.includes(raw.severity) ? raw.severity : 'major',
    title: text(raw.title).slice(0, 200) || '(untitled)',
    detail: text(raw.detail),
    evidence: text(raw.evidence),
    requirement: text(raw.requirement),
    fix: text(raw.fix),
    status: 'open',
    thread: [],
  };
}

function header(config, ctx, round, files) {
  const { pr, story } = ctx;
  const lines = [
    `PR check round ${round} of at most ${config.prCheck.maxRounds}.`,
    pr ? `Pull request: #${pr.number} "${pr.title}" (${pr.url}), branch ${pr.headRef} into ${pr.baseRef}` : `Branch: ${ctx.branch}`,
    `Story: ${story.key} - ${ctx.storyTitle} (${story.file})`,
    `Frontend story: ${ctx.isFrontend ? 'yes' : 'no'}`,
    `Round folder: ${files.roundDir}`,
    '',
    'Files (paths relative to the repository root; read what you need):',
    `- Story: ${story.file}`,
    `- PR description: ${files.prFile}`,
    `- Diff against ${ctx.baseRef} (merge base ${ctx.baseRev.slice(0, 10)}): ${files.diffFile}`,
    `- Changed files (${ctx.diff.files.length}): ${ctx.diff.files.slice(0, 200).join(', ')}${ctx.diff.files.length > 200 ? ', ...' : ''}`,
    `- Conventions: ${ctx.conventions.join(', ') || '(none)'}`,
    `- Mockups: ${ctx.mockups.join(', ') || '(none for this story)'}`,
    `- Video frames: ${ctx.video.videos.length ? `${files.framesFile} (${ctx.video.videos.length} video(s), ${ctx.video.videos.reduce((n, v) => n + v.frames.length, 0)} frames)` : '(no readable videos)'}`,
  ];
  if (ctx.video.unreadable.length) lines.push(`- Videos that could not be read: ${ctx.video.unreadable.map((u) => `${u.source} (${u.reason})`).join('; ')}`);
  if (files.previousRulings) lines.push(`- Upheld findings from the previous round: ${files.previousRulings}`);
  lines.push('', '### Diff stat', '', '```', ctx.diff.stat.trimEnd() || '(empty)', '```');
  return lines.join('\n');
}

async function callAgent(config, ctx, files, { role, agent, label, task, mode = 'read', timeoutSec, validate }) {
  for (let attempt = 0; attempt <= config.agent.resultParseRetries; attempt++) {
    const result = await runAgentAsync(config, {
      name: agent,
      task,
      mode,
      model: modelFor(config, role),
      cwd: ctx.workRoot,
      timeoutSec: timeoutSec ?? config.prCheck.timeoutSec,
      promptFile: path.join(files.roundAbs, `${label}.prompt.md`),
    });
    fs.writeFileSync(path.join(files.roundAbs, `${label}.out.md`), result.output ?? '');
    const raw = extractResult(result.output ?? '');
    if (raw && validate(raw)) return raw;
    log(`${label}: no valid AGENT_RESULT (attempt ${attempt + 1})`);
  }
  return null;
}

async function critique(config, ctx, files, round, lens, previousUpheld) {
  const earlier = previousUpheld.filter((f) => f.lens === lens).map(({ id, severity, title, evidence, fix }) => ({ id, severity, title, evidence, fix }));
  const task = [
    header(config, ctx, round, files),
    '',
    `## Lens: ${LENSES[lens].title}. First pass`,
    '',
    'Review the PR through your lens and report every finding with evidence.',
    earlier.length ? `\nThese findings were upheld last round; check each one and re-raise it with the same id if it is not fixed:\n\n${json(earlier)}` : '',
  ].join('\n');
  return callAgent(config, ctx, files, {
    role: lens,
    agent: LENSES[lens].agent,
    label: `critic-${lens}`,
    task,
    validate: (r) => Array.isArray(r.findings) && Number.isFinite(Number(r.score)),
  });
}

async function debate(config, ctx, files, round, findings, lenses) {
  const save = () => writeJson(files.debateAbs, { round, lenses, findings });
  save();
  for (let turn = 1; turn <= config.prCheck.debateTurns; turn++) {
    const open = findings.filter((f) => f.status === 'open');
    if (!open.length) break;
    const defense = await callAgent(config, ctx, files, {
      role: 'defender',
      agent: 'pr-check-defender',
      label: `defender-turn-${turn}`,
      task: [header(config, ctx, round, files), '', `## Debate turn ${turn} of ${config.prCheck.debateTurns}`, '', `Answer every finding below (the full debate so far is in ${files.debateFile}):`, '', json(open)].join('\n'),
      validate: (r) => Array.isArray(r.responses),
    });
    if (!defense) {
      log(`round ${round}: the defender gave no answer on turn ${turn}; the judge rules without it`);
      break;
    }
    const answers = new Map(defense.responses.filter((r) => r?.id).map((r) => [String(r.id), r]));
    const contested = [];
    for (const f of open) {
      const r = answers.get(f.id);
      if (!r) continue;
      f.thread.push({ turn, by: 'defender', position: r.position, argument: r.argument ?? '', evidence: r.evidence ?? '', severity: r.severity });
      if (r.position === 'concede') f.status = 'conceded';
      else contested.push(f);
    }
    save();
    if (!contested.length) break;
    const byLens = Object.groupBy ? Object.groupBy(contested, (f) => f.lens) : contested.reduce((acc, f) => ((acc[f.lens] ??= []).push(f), acc), {});
    await Promise.all(
      Object.entries(byLens).map(async ([lens, mine]) => {
        const reply = await callAgent(config, ctx, files, {
          role: lens,
          agent: LENSES[lens].agent,
          label: `critic-${lens}-rejoinder-${turn}`,
          task: [header(config, ctx, round, files), '', `## Lens: ${LENSES[lens].title}. Rejoinder, debate turn ${turn}`, '', 'The defender answered these findings of yours. Maintain, amend or withdraw each one:', '', json(mine)].join('\n'),
          validate: (r) => Array.isArray(r.responses),
        });
        const replies = new Map((reply?.responses ?? []).filter((r) => r?.id).map((r) => [String(r.id), r]));
        for (const f of mine) {
          const r = replies.get(f.id);
          if (!r) continue;
          f.thread.push({ turn, by: 'critic', action: r.action, argument: r.argument ?? '' });
          if (r.action === 'withdraw') f.status = 'withdrawn';
          if (r.action === 'amend' && r.finding && typeof r.finding === 'object') {
            Object.assign(f, normalizeFinding({ ...f, ...r.finding }, f.lens, f.id), { status: 'open', thread: f.thread });
          }
        }
      }),
    );
    save();
  }
}

async function judge(config, ctx, files, round, lenses, findings, coverage, failedLenses) {
  writeJson(path.join(files.roundAbs, 'coverage.json'), coverage);
  const task = [
    header(config, ctx, round, files),
    '',
    '## Rule on this round',
    '',
    `Lenses: ${lenses.join(', ')}.${failedLenses.length ? ` These critics failed and score 0 regardless: ${failedLenses.join(', ')}.` : ''}`,
    `Debate (${findings.length} findings; statuses open, conceded or withdrawn, with the full thread): ${files.debateFile}`,
    `Critic coverage tables: ${files.coverageFile}`,
    `Coverage gaps without a finding use the id "<lens>-coverage-<n>", for example "acceptance-coverage-1".`,
    '',
    findings.length ? json(findings.map(({ thread, ...f }) => ({ ...f, exchanges: thread.length }))) : 'No findings were raised. Check the coverage tables before you award 10.',
  ].join('\n');
  return callAgent(config, ctx, files, {
    role: 'judge',
    agent: 'pr-check-judge',
    label: 'judge',
    task,
    validate: (r) => Number.isFinite(Number(r.score)) && Array.isArray(r.rulings),
  });
}

async function fixRound(config, ctx, files, round, upheld) {
  writeJson(files.rulingsAbs, { round, upheld });
  const result = await callAgent(config, ctx, files, {
    role: 'fixer',
    agent: 'pr-check-fixer',
    label: 'fixer',
    mode: 'write',
    timeoutSec: config.prCheck.fixTimeoutSec,
    task: [header(config, ctx, round, files), '', '## Fix these upheld findings', '', `Also in ${files.rulingsFile}.`, '', json(upheld)].join('\n'),
    validate: (r) => Array.isArray(r.fixed),
  });
  const commit = commitFixes(config, ctx, `${ctx.story.key}: PR check round ${round} fixes\n\nFixes upheld PR check findings: ${upheld.map((f) => f.id).join(', ')}`);
  return { fixed: result?.fixed ?? [], notFixed: result?.notFixed ?? [], summary: result?.summary ?? '', commit };
}

function roundFiles(ctx, round) {
  const roundAbs = path.join(ctx.runDir, `round-${round}`);
  fs.rmSync(roundAbs, { recursive: true, force: true });
  fs.mkdirSync(roundAbs, { recursive: true });
  return {
    roundAbs,
    roundDir: rel(ctx, roundAbs),
    prFile: rel(ctx, path.join(ctx.runDir, 'context', 'pr.md')),
    framesFile: rel(ctx, path.join(ctx.runDir, 'context', 'frames.json')),
    diffFile: rel(ctx, path.join(roundAbs, 'diff.patch')),
    debateAbs: path.join(roundAbs, 'debate.json'),
    debateFile: rel(ctx, path.join(roundAbs, 'debate.json')),
    coverageFile: rel(ctx, path.join(roundAbs, 'coverage.json')),
    rulingsAbs: path.join(roundAbs, 'rulings.json'),
    rulingsFile: rel(ctx, path.join(roundAbs, 'rulings.json')),
    previousRulings: round > 1 ? rel(ctx, path.join(ctx.runDir, `round-${round - 1}`, 'rulings.json')) : null,
  };
}

export async function runRound(config, ctx, round, previousUpheld) {
  const files = roundFiles(ctx, round);
  ctx.diff = prDiff(config, ctx);
  fs.writeFileSync(path.join(files.roundAbs, 'diff.patch'), `${ctx.diff.stat}\n${ctx.diff.diff}`);
  const lenses = activeLenses(ctx);
  log(`round ${round}: critics ${lenses.join(', ')}`);

  const reviews = await Promise.all(lenses.map((lens) => critique(config, ctx, files, round, lens, previousUpheld)));
  const findings = [];
  const coverage = {};
  const failedLenses = [];
  const used = new Set();
  const earlierIds = new Set(previousUpheld.map((f) => f.id));
  reviews.forEach((review, i) => {
    const lens = lenses[i];
    if (!review) {
      failedLenses.push(lens);
      return;
    }
    coverage[lens] = { score: Number(review.score), summary: review.summary ?? '', coverage: Array.isArray(review.coverage) ? review.coverage : [] };
    let n = 0;
    for (const raw of review.findings) {
      if (!raw || typeof raw !== 'object') continue;
      const reused = raw.id && earlierIds.has(String(raw.id)) && previousUpheld.find((f) => f.id === String(raw.id))?.lens === lens && !used.has(String(raw.id));
      let id = reused ? String(raw.id) : `${LENSES[lens].code}-${round}-${++n}`;
      while (used.has(id)) id = `${LENSES[lens].code}-${round}-${++n}`;
      used.add(id);
      findings.push(normalizeFinding(raw, lens, id));
    }
  });

  await debate(config, ctx, files, round, findings, lenses);
  const verdict = await judge(config, ctx, files, round, lenses, findings, coverage, failedLenses);
  if (!verdict) log(`round ${round}: the judge gave no ruling; every finding not withdrawn stands`);
  const scored = scoreRound({ lenses, findings, rulings: verdict?.rulings ?? [], judgeLensScores: verdict?.lensScores ?? {}, failedLenses });
  writeJson(files.rulingsAbs, { round, score: scored.score, lensScores: scored.lensScores, summary: verdict?.summary ?? '', upheld: scored.upheld, rulings: verdict?.rulings ?? [] });
  return {
    round,
    files,
    lenses,
    failedLenses,
    findings,
    ...scored,
    rulings: verdict?.rulings ?? [],
    summary: verdict?.summary ?? (failedLenses.length ? `critics failed: ${failedLenses.join(', ')}` : ''),
    diffHash: sha(ctx.diff.diff),
  };
}

// The finding details the lessons process (scripts/qe/lib/lessons.mjs) learns from: upheld findings
// teach the implementer (round 1) or the fixer (a finding it failed to fix), dismissed ones teach the
// critic that raised them.
export function lessonFields(result, previousUpheld = []) {
  const clip = (v, n = 300) => (v ? String(v).slice(0, n) : '');
  const earlier = new Set(previousUpheld.map((f) => f.id));
  const rulings = new Map((result.rulings ?? []).filter((r) => r && r.id).map((r) => [String(r.id), r]));
  return {
    upheld: result.upheld.map((f) => ({
      id: f.id,
      lens: f.lens,
      severity: f.severity,
      title: clip(f.title, 200),
      detail: clip(f.detail),
      requirement: clip(f.requirement, 200),
      evidence: clip(f.evidence, 200),
      repeat: earlier.has(f.id),
    })),
    dismissed: result.findings
      .filter((f) => rulings.has(f.id) && rulings.get(f.id).ruling !== 'upheld')
      .map((f) => ({ id: f.id, lens: f.lens, severity: f.severity, title: clip(f.title, 200), reason: clip(rulings.get(f.id).reason) })),
  };
}

// Runs rounds until 10/10, the round limit, or a stall (the fixer changes nothing, or the same finding
// survives three rounds in a row). Without fixing, one round is the whole check.
export async function runPrCheck(config, ctx, { fix }) {
  const { maxRounds } = config.prCheck;
  const history = [];
  let previousUpheld = [];
  const streak = new Map();
  let verdict = 'FAIL';
  let reason = '';
  for (let round = 1; round <= maxRounds; round++) {
    const result = await runRound(config, ctx, round, previousUpheld);
    const entry = { round, score: result.score, lensScores: result.lensScores, raised: result.findings.length, upheld: result.upheld, failedLenses: result.failedLenses, summary: result.summary };
    history.push(entry);
    appendJsonl(metricsFile(config, CHECKS_FILE), {
      ts: nowIso(),
      target: ctx.id,
      story: ctx.story.key,
      round,
      score: result.score,
      lensScores: result.lensScores,
      raised: result.findings.length,
      withdrawn: result.findings.filter((f) => f.status === 'withdrawn').length,
      ...lessonFields(result, previousUpheld),
    });
    log(`round ${round}: ${result.score}/10 (${Object.entries(result.lensScores).map(([l, s]) => `${l} ${s}`).join(', ')}); ${result.upheld.length} upheld of ${result.findings.length} raised`);

    if (result.score === 10) {
      verdict = 'PASS';
      break;
    }
    if (!fix) break;
    if (round === maxRounds) {
      verdict = 'ESCALATED';
      reason = `not 10/10 after ${maxRounds} rounds`;
      break;
    }
    for (const id of [...streak.keys()]) if (!result.upheld.some((f) => f.id === id)) streak.delete(id);
    for (const f of result.upheld) streak.set(f.id, (streak.get(f.id) ?? 0) + 1);
    const stuck = [...streak].filter(([, n]) => n >= 3).map(([id]) => id);
    if (stuck.length) {
      verdict = 'ESCALATED';
      reason = `findings upheld three rounds in a row: ${stuck.join(', ')}`;
      break;
    }
    if (result.upheld.length) {
      entry.fix = await fixRound(config, ctx, result.files, round, result.upheld);
      log(`round ${round}: fixer ${entry.fix.commit ? `committed ${entry.fix.commit}` : 'changed nothing'}`);
      if (!entry.fix.commit) {
        const humanNeeded = entry.fix.notFixed.length >= result.upheld.length;
        const previousNoop = history.length > 1 && history[history.length - 2].fix && !history[history.length - 2].fix.commit;
        if (humanNeeded || previousNoop) {
          verdict = 'ESCALATED';
          reason = humanNeeded ? 'the fixer could not fix the upheld findings without a human decision' : 'the fixer changed nothing two rounds in a row';
          break;
        }
      }
    }
    previousUpheld = result.upheld;
  }
  if (verdict === 'ESCALATED') {
    const last = history[history.length - 1];
    escalate(config, { key: ctx.id }, 'pr-check', reason, last.upheld.map((f) => `- [${f.severity}] ${f.id} (${f.lens}) ${f.title}: ${f.fix}`).join('\n'));
  }
  return { verdict, reason, history };
}
