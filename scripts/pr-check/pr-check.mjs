#!/usr/bin/env node
// PR check: an adversarial review loop that scores a pull request against its story (acceptance
// criteria, flow, UI design, code style, video evidence) and fixes it until it scores 10/10.
// See docs/ai-pipeline/pr-check.md.
import fs from 'node:fs';
import path from 'node:path';
import { parseArgs, runMain } from '../pipeline/lib/cli.mjs';
import { loadConfig } from '../pipeline/lib/config.mjs';
import { git } from '../pipeline/lib/git.mjs';
import { lockFile, metricsFile } from '../pipeline/lib/state.mjs';
import { acquireLock, log, lockHolder, nowIso, readJson, readJsonl, releaseLock, setLogTag, writeJson } from '../pipeline/lib/util.mjs';
import { isClean, removeCheckout, resolveTarget } from './lib/context.mjs';
import { commentOnPullRequest } from './lib/github.mjs';
import { CHECKS_FILE, runPrCheck } from './lib/loop.mjs';
import { renderReport } from './lib/report.mjs';
import { extractVideoFrames, videoSources } from './lib/video.mjs';

setLogTag('pr-check');

const USAGE = `Usage: node scripts/pr-check/pr-check.mjs <command> [options]

  run [--pr <n>] [--story <KEY>] [--base <ref>] [--videos <path|url>,...]
      [--no-fix] [--max-rounds <n>] [--debate-turns <n>] [--comment] [--push] [--json]
        Review the PR (or the current branch) against its story until it scores 10/10.
        Without --pr it checks the current branch. --no-fix runs one report-only round.
  status [pr-<n>|KEY]           last result of each check (or one)
  metrics [--json] [--since <iso-date>]

Models: PR_CHECK_MODEL_<ROLE> (JUDGE, ACCEPTANCE, FLOW, DESIGN, STYLE, VIDEO, DEFENDER, FIXER)
overrides scripts/pr-check/config.json.`;

const VALUE_FLAGS = new Set(['pr', 'story', 'base', 'videos', 'max-rounds', 'debate-turns', 'since']);

const resultDir = (config, id) => path.join(config.root, config.stateDir, 'pr-check', id);

function positiveInt(value, name) {
  const n = Number(value);
  if (!Number.isInteger(n) || n < 1) throw new Error(`--${name} must be a positive whole number`);
  return n;
}

async function runCheck(config, flags) {
  const fix = !flags['no-fix'];
  if (flags['max-rounds']) config.prCheck.maxRounds = positiveInt(flags['max-rounds'], 'max-rounds');
  if (flags['debate-turns']) config.prCheck.debateTurns = Number(flags['debate-turns']);
  if (!fix) config.prCheck.maxRounds = 1;

  const ctx = await resolveTarget(config, { pr: flags.pr ? positiveInt(flags.pr, 'pr') : null, story: flags.story, base: flags.base, fix });
  const lock = lockFile(config, ctx.id, 'pr-check');
  if (!acquireLock(lock)) throw new Error(`a PR check of ${ctx.id} is already running (pid ${lockHolder(lock)})`);
  try {
    // The fixer's changes are committed with `git add -A`; refuse to sweep someone's uncommitted work in.
    if (fix && !isClean(config, ctx)) throw new Error('the working tree has uncommitted changes; commit or stash them, or pass --no-fix');
    ctx.runDir = path.join(ctx.workRoot, config.stateDir, 'pr-check', ctx.id);
    fs.rmSync(ctx.runDir, { recursive: true, force: true });
    const contextDir = path.join(ctx.runDir, 'context');
    fs.mkdirSync(contextDir, { recursive: true });
    fs.writeFileSync(path.join(contextDir, 'pr.md'), ctx.pr ? `# #${ctx.pr.number} ${ctx.pr.title}\n\n${ctx.pr.url}\n\n${ctx.pr.body || '(no description)'}\n` : `No pull request: checking branch ${ctx.branch}.\n`);

    const values = typeof flags.videos === 'string' ? flags.videos.split(',').map((v) => v.trim()).filter(Boolean) : [];
    ctx.video = await extractVideoFrames(config, ctx.runDir, videoSources(config, { values, prBody: ctx.pr?.body ?? '' }), ctx.workRoot);
    writeJson(path.join(contextDir, 'frames.json'), ctx.video);

    log(`checking ${ctx.id} against ${ctx.story.key} (base ${ctx.baseRef}, ${fix ? `fixing, up to ${config.prCheck.maxRounds} rounds` : 'report only'})`);
    const started = Date.now();
    const result = await runPrCheck(config, ctx, { fix });
    const last = result.history[result.history.length - 1];
    const report = renderReport(ctx, result);

    let pushed = false;
    const commits = result.history.map((h) => h.fix?.commit).filter(Boolean);
    if (flags.push && commits.length && ctx.pr?.fromFork) log('not pushing: the PR comes from a fork; push the fix commits to the fork yourself');
    else if (flags.push && commits.length) {
      pushed = git(ctx.workRoot, ['push', '--quiet', 'origin', `HEAD:${ctx.branch}`]).status === 0;
      log(pushed ? `pushed ${commits.length} fix commit(s) to ${ctx.branch}` : `push to ${ctx.branch} failed`);
    }
    if (flags.comment && ctx.pr) await commentOnPullRequest(config.root, ctx.pr.number, report);

    const out = resultDir(config, ctx.id);
    fs.mkdirSync(out, { recursive: true });
    fs.writeFileSync(path.join(out, 'report.md'), report);
    const summary = {
      id: ctx.id,
      story: ctx.story.key,
      pr: ctx.pr?.url ?? null,
      verdict: result.verdict,
      score: last?.score ?? 0,
      lensScores: last?.lensScores ?? {},
      rounds: result.history.length,
      reason: result.reason,
      openFindings: last?.upheld.map(({ id, lens, severity, title }) => ({ id, lens, severity, title })) ?? [],
      commits,
      pushed,
      report: path.relative(config.root, path.join(out, 'report.md')).replace(/\\/g, '/'),
      runDir: path.relative(config.root, ctx.runDir).replace(/\\/g, '/'),
      seconds: Math.round((Date.now() - started) / 1000),
      finishedAt: nowIso(),
    };
    writeJson(path.join(out, 'result.json'), summary);
    if (ctx.workRoot !== config.root) fs.cpSync(ctx.runDir, path.join(out, 'run'), { recursive: true });
    if (flags.json) console.log(JSON.stringify(summary, null, 2));
    else console.log(`${report}\nReport: ${summary.report}`);
    return summary.verdict === 'PASS' ? 0 : 1;
  } finally {
    releaseLock(lock);
    removeCheckout(config, ctx);
  }
}

function metrics(config, since) {
  const records = readJsonl(metricsFile(config, CHECKS_FILE)).filter((r) => !since || r.ts >= since);
  const targets = new Map();
  for (const r of records) targets.set(r.target, r);
  const finals = [...targets.values()];
  const upheldByLens = {};
  const upheldBySeverity = {};
  for (const r of records) {
    for (const u of r.upheld ?? []) {
      upheldByLens[u.lens] = (upheldByLens[u.lens] ?? 0) + 1;
      upheldBySeverity[u.severity] = (upheldBySeverity[u.severity] ?? 0) + 1;
    }
  }
  const raised = records.reduce((n, r) => n + (r.raised ?? 0), 0);
  const upheld = records.reduce((n, r) => n + (r.upheld?.length ?? 0), 0);
  return {
    rounds: records.length,
    checks: finals.length,
    perfect: finals.filter((r) => r.score === 10).length,
    firstRoundPerfect: records.filter((r) => r.round === 1 && r.score === 10).length,
    averageRounds: finals.length ? Math.round((records.length / finals.length) * 10) / 10 : 0,
    raised,
    upheld,
    upheldRate: raised ? Math.round((upheld / raised) * 100) / 100 : 0,
    upheldByLens,
    upheldBySeverity,
  };
}

async function main() {
  const [command, ...rest] = process.argv.slice(2);
  const { positional, flags } = parseArgs(rest, VALUE_FLAGS);
  if (!command || command === 'help' || flags.help) {
    console.log(USAGE);
    return 0;
  }
  const config = loadConfig('pr-check');

  switch (command) {
    case 'run':
      return runCheck(config, flags);

    case 'status': {
      const root = path.join(config.root, config.stateDir, 'pr-check');
      const ids = positional.length ? positional : fs.existsSync(root) ? fs.readdirSync(root).filter((d) => fs.existsSync(path.join(root, d, 'result.json'))) : [];
      if (!ids.length) console.log('No PR checks have run yet.');
      for (const id of ids) {
        const r = readJson(path.join(resultDir(config, id), 'result.json'), null);
        if (!r) {
          console.log(`${id.padEnd(10)} no result`);
          continue;
        }
        console.log(`${id.padEnd(10)} ${r.verdict} ${r.score}/10 after ${r.rounds} round(s), ${r.openFindings.length} open finding(s)  ${r.report}`);
        if (positional.length) for (const f of r.openFindings) console.log(`  - [${f.severity}] ${f.id} ${f.title}`);
      }
      return 0;
    }

    case 'metrics': {
      const m = metrics(config, typeof flags.since === 'string' ? flags.since : null);
      if (flags.json) console.log(JSON.stringify(m, null, 2));
      else {
        console.log(`PR checks: ${m.checks} checks, ${m.rounds} rounds (average ${m.averageRounds}), ${m.perfect} reached 10/10, ${m.firstRoundPerfect} on the first round`);
        console.log(`Findings: ${m.raised} raised, ${m.upheld} upheld (rate ${m.upheldRate}); by lens ${Object.entries(m.upheldByLens).map(([k, v]) => `${k}=${v}`).join(' ') || 'none'}`);
      }
      return 0;
    }

    default:
      console.error(`Unknown command "${command}".\n\n${USAGE}`);
      return 2;
  }
}

runMain(main, 'pr-check');
