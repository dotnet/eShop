#!/usr/bin/env node
// QE agent pipeline. See docs/qe-pipeline.md.
import fs from 'node:fs';
import path from 'node:path';
import { isDisabled, loadConfig } from './lib/config.mjs';
import { changedFiles, fsReader, git, gitReader, mergeBase, implChanges, revExists } from './lib/git.mjs';
import { calibrateCommand, lessonsCommand } from './lib/lessons.mjs';
import { computeMetrics, formatMetrics } from './lib/metrics.mjs';
import {
  architectureReview,
  automationLoop,
  hookStop,
  planLoop,
  sessionContext,
  spawnBackground,
} from './lib/pipeline.mjs';
import { automationFiles, automationStatus, planStatus } from './lib/plans.mjs';
import { loadState, stateDir, updateState } from './lib/state.mjs';
import { CASE_ID, activeStory, listStories, resolveStory, storiesFromPaths } from './lib/stories.mjs';
import { log, nowIso } from './lib/util.mjs';

const USAGE = `Usage: node scripts/qe/qe.mjs <command> [options]

Workflow 1 (story -> verified test plan)
  plan <story...> | --stale | --all [--force] [--background]
  check [--changed-from <ref>] [--rev <rev>] [--all] [--pre-push]

Workflow 2 (implementation -> architecture review -> verified automated tests)
  start <story>                 set the story the implementation agent is working on
  done [story]                  mark the line of work complete (the Stop hook then reviews it)
  review [story]                run one architecture review now and print the verdict
  automate <story> [--impl-hash <h>] [--skip-tests] [--background]
  status [story]

Observability and improvement
  metrics [--json] [--since <iso-date>]
  lessons [--dry-run] [--local] [--if-due] [--no-push]
  calibrate [--stage plan|automation] [--sample <n>]

Hooks (called by agent and git hooks)
  hook stop | hook session-start [--nested]
  git-hook post-commit | git-hook post-merge`;

function parseArgs(argv) {
  const positional = [];
  const flags = {};
  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    if (!arg.startsWith('--')) {
      positional.push(arg);
      continue;
    }
    const [key, inline] = arg.slice(2).split('=', 2);
    if (inline !== undefined) flags[key] = inline;
    else if (argv[i + 1] && !argv[i + 1].startsWith('--') && VALUE_FLAGS.has(key)) flags[key] = argv[++i];
    else flags[key] = true;
  }
  return { positional, flags };
}

const VALUE_FLAGS = new Set(['changed-from', 'rev', 'impl-hash', 'since', 'stage', 'sample']);

function readStdin() {
  if (process.stdin.isTTY) return '';
  try {
    return fs.readFileSync(0, 'utf8');
  } catch {
    return '';
  }
}

function requireStory(config, arg, { fallbackActive = false } = {}) {
  const story = arg ? resolveStory(config, arg) : fallbackActive ? activeStory(config) : null;
  if (!story) {
    console.error(arg ? `Unknown story "${arg}".` : 'No story given and no active story (run `qe start <story>` or use a branch named after the story).');
    process.exit(2);
  }
  return story;
}

// Stories touched by a set of changed paths: the story file, its plan, or tests carrying its case ids.
function touchedStories(config, paths, reader) {
  const keys = new Set(storiesFromPaths(config, paths, reader).map((s) => s.key));
  for (const story of listStories(config, reader)) {
    if (paths.some((p) => p.startsWith(`${story.planDir}/`) || p.startsWith(`${config.scenariosDir}/${story.scenario}/`))) keys.add(story.key);
  }
  for (const file of paths) {
    if (!config.testExtensions.some((ext) => file.endsWith(ext))) continue;
    for (const match of (reader.read(file) ?? '').matchAll(CASE_ID)) keys.add(match[1]);
  }
  return listStories(config, reader).filter((s) => keys.has(s.key));
}

function checkStories(config, stories, reader, { allowMissing = false } = {}) {
  let failed = 0;
  for (const story of stories) {
    const plan = planStatus(config, story, reader);
    const automation = automationStatus(config, story, reader);
    const planOk = plan.state === 'verified' || (allowMissing && plan.state === 'missing');
    const ok = planOk && automation.state !== 'unverified';
    if (!ok) failed++;
    const detail = [...plan.reasons, ...automation.reasons].filter((r) => r !== 'no plan' || !allowMissing).join('; ');
    console.log(`${ok ? 'ok  ' : 'FAIL'} ${story.key.padEnd(10)} plan=${plan.state.padEnd(10)} automation=${automation.state.padEnd(10)} ${detail}`);
  }
  return failed;
}

async function main() {
  const [command, ...rest] = process.argv.slice(2);
  const { positional, flags } = parseArgs(rest);
  if (!command || command === 'help' || flags.help) {
    console.log(USAGE);
    return 0;
  }
  const config = loadConfig();

  switch (command) {
    case 'plan': {
      if (flags.background) {
        const args = ['plan', ...positional, ...Object.keys(flags).filter((f) => f !== 'background').map((f) => `--${f}`)];
        const bg = spawnBackground(config, positional[0] ?? 'batch', 'plan', args);
        console.log(`Plan run started in the background (pid ${bg.pid}, log ${bg.log}).`);
        return 0;
      }
      let stories = positional.map((arg) => requireStory(config, arg));
      if (flags.stale || flags.all) {
        stories = listStories(config).filter((s) => {
          const state = planStatus(config, s).state;
          return flags.all ? state !== 'verified' : state === 'stale' || state === 'unverified';
        });
      }
      if (stories.length === 0) {
        console.log('No stories to plan.');
        return 0;
      }
      let failed = 0;
      for (const story of stories) {
        const result = await planLoop(config, story, { force: Boolean(flags.force) });
        console.log(`${story.key}: ${result.status}`);
        if (!['verified', 'busy'].includes(result.status)) failed++;
      }
      return failed ? 1 : 0;
    }

    case 'check': {
      let reader = fsReader(config.root);
      let stories;
      if (flags['pre-push']) {
        if (process.env.QE_SKIP === '1') return 0;
        let failed = 0;
        for (const line of readStdin().split('\n').filter(Boolean)) {
          const [, localSha, , remoteSha] = line.split(' ');
          if (!localSha || /^0+$/.test(localSha)) continue;
          const from = /^0+$/.test(remoteSha ?? '') || !revExists(config.root, remoteSha) ? mergeBase(config.root, config.baseRef) : remoteSha;
          reader = gitReader(config.root, localSha);
          stories = touchedStories(config, changedFiles(config.root, from, localSha), reader);
          failed += checkStories(config, stories, reader);
        }
        if (failed) console.error('\nQE: push blocked. Run `node scripts/qe/qe.mjs plan <story>` (and commit the plan) or re-run `automate`. Bypass once with QE_SKIP=1.');
        return failed ? 1 : 0;
      }
      if (flags.rev) reader = gitReader(config.root, flags.rev);
      if (flags.all) {
        stories = listStories(config, reader);
        return checkStories(config, stories, reader, { allowMissing: true }) ? 1 : 0;
      }
      const from = flags['changed-from'] ? git(config.root, ['merge-base', flags.rev ?? 'HEAD', flags['changed-from']]).stdout.trim() : mergeBase(config.root, config.baseRef);
      const paths = changedFiles(config.root, from, flags.rev);
      stories = touchedStories(config, paths, reader);
      if (stories.length === 0) {
        console.log('No stories, plans or story tests changed.');
        return 0;
      }
      return checkStories(config, stories, reader) ? 1 : 0;
    }

    case 'start': {
      const story = requireStory(config, positional[0]);
      fs.mkdirSync(stateDir(config), { recursive: true });
      fs.writeFileSync(path.join(stateDir(config), 'active-story'), `${story.key}\n`);
      updateState(config, story.key, (s) => {
        s.done = null;
        Object.assign(s.review, { rounds: 0, lastHash: null, lastVerdict: null, lastReason: null, unchangedBlocks: 0 });
      });
      const plan = planStatus(config, story);
      console.log(`Active story: ${story.key}. Plan: ${plan.state}. Run \`node scripts/qe/qe.mjs done\` when the line of work is complete.`);
      return 0;
    }

    case 'done': {
      const story = requireStory(config, positional[0], { fallbackActive: true });
      const { hash, files } = implChanges(config);
      updateState(config, story.key, (s) => (s.done = { at: nowIso(), hash }));
      console.log(`${story.key} marked complete (${files.length} implementation file(s) changed). The architecture review runs when this agent turn ends.`);
      return 0;
    }

    case 'review': {
      const story = requireStory(config, positional[0], { fallbackActive: true });
      const state = loadState(config, story.key);
      console.log(JSON.stringify(architectureReview(config, story, state.review.rounds + 1), null, 2));
      return 0;
    }

    case 'automate': {
      const story = requireStory(config, positional[0], { fallbackActive: true });
      if (flags.background) {
        const args = ['automate', story.key, ...(flags['impl-hash'] ? ['--impl-hash', flags['impl-hash']] : []), ...(flags['skip-tests'] ? ['--skip-tests'] : [])];
        const bg = spawnBackground(config, story.key, 'automation', args);
        console.log(`Automation started in the background (pid ${bg.pid}, log ${bg.log}).`);
        return 0;
      }
      const result = await automationLoop(config, story, { implHash: flags['impl-hash'] ?? null, skipTests: Boolean(flags['skip-tests']) });
      console.log(`${story.key}: ${result.status}`);
      return ['verified', 'busy'].includes(result.status) ? 0 : 1;
    }

    case 'status': {
      const stories = positional.length ? positional.map((a) => requireStory(config, a)) : [activeStory(config)].filter(Boolean);
      const list = stories.length ? stories : listStories(config);
      for (const story of list) {
        const plan = planStatus(config, story);
        const automation = automationStatus(config, story);
        const state = loadState(config, story.key);
        const tests = automationFiles(config, story).tests.size;
        console.log(`${story.key.padEnd(10)} plan=${plan.state.padEnd(10)} cases=${String(plan.plan.cases.length).padEnd(3)} tests=${String(tests).padEnd(3)} automation=${automation.state}/${state.automation.status} review=${state.review.lastVerdict ?? '-'}${state.review.rounds ? ` (round ${state.review.rounds})` : ''}`);
        if (stories.length && state.review.lastReason) console.log(`\nLast review:\n${state.review.lastReason}\n`);
        if (stories.length && state.automation.log) console.log(`Automation log: ${state.automation.log}`);
      }
      return 0;
    }

    case 'metrics': {
      const metrics = computeMetrics(config, { since: flags.since });
      console.log(flags.json ? JSON.stringify(metrics, null, 2) : formatMetrics(metrics));
      return 0;
    }

    case 'lessons': {
      const result = await lessonsCommand(config, { dryRun: Boolean(flags['dry-run']), local: Boolean(flags.local), ifDue: Boolean(flags['if-due']), push: !flags['no-push'] });
      console.log(JSON.stringify(result, null, 2));
      return 0;
    }

    case 'calibrate': {
      const results = calibrateCommand(config, { stage: flags.stage ?? null, sample: Number(flags.sample ?? 3) });
      console.log(JSON.stringify(results, null, 2));
      return 0;
    }

    case 'hook': {
      if (isDisabled()) return 0;
      const event = positional[0];
      if (event === 'stop') {
        let payload = {};
        try {
          payload = JSON.parse(readStdin() || '{}');
        } catch {
          // no payload
        }
        const story = activeStory(config);
        if (!story) return 0;
        const output = hookStop(config, story, payload);
        if (output) process.stdout.write(JSON.stringify(output));
        return 0;
      }
      if (event === 'session-start') {
        readStdin();
        const text = sessionContext(config, activeStory(config), listStories(config));
        // Copilot reads only top-level additionalContext and ignores plain text; Claude Code and
        // Cursor read the nested form and tolerate the extra key. Codex rejects unknown keys, so
        // its config passes --nested.
        if (text) {
          const nested = { hookSpecificOutput: { hookEventName: 'SessionStart', additionalContext: text } };
          process.stdout.write(JSON.stringify(flags.nested ? nested : { additionalContext: text, ...nested }));
        }
        return 0;
      }
      console.error(`Unknown hook event "${event}".`);
      return 2;
    }

    case 'git-hook': {
      if (isDisabled() || git(config.root, ['config', '--bool', 'qe.autoPlan']).stdout.trim() === 'false') return 0;
      const event = positional[0];
      let paths = [];
      if (event === 'post-commit') {
        paths = revExists(config.root, 'HEAD~1')
          ? changedFiles(config.root, 'HEAD~1', 'HEAD')
          : git(config.root, ['show', '--name-only', '--format=', 'HEAD']).stdout.split('\n').filter(Boolean);
      } else if (event === 'post-merge') {
        paths = changedFiles(config.root, 'ORIG_HEAD', 'HEAD');
      }
      const stories = storiesFromPaths(config, paths).filter((s) => planStatus(config, s).state !== 'verified');
      if (stories.length) {
        const bg = spawnBackground(config, stories[0].key, 'plan', ['plan', ...stories.map((s) => s.key)]);
        log(`stories changed (${stories.map((s) => s.key).join(', ')}); writing test plans in the background (log ${bg.log}).`);
      }
      if (event === 'post-merge' && git(config.root, ['config', '--bool', 'qe.autoLessons']).stdout.trim() === 'true') {
        spawnBackground(config, 'lessons', 'lessons', ['lessons', '--if-due']);
      }
      return 0;
    }

    default:
      console.error(`Unknown command "${command}".\n\n${USAGE}`);
      return 2;
  }
}

main().then(
  (code) => process.exit(code ?? 0),
  (error) => {
    if (process.argv[2] === 'hook' || process.argv[2] === 'git-hook') {
      // Hooks must never break the agent or git; surface the problem and let the turn finish.
      if (process.argv[3] === 'stop') process.stdout.write(JSON.stringify({ systemMessage: `QE hook error: ${error.message}` }));
      else console.error(`[qe] hook error: ${error.message}`);
      process.exit(0);
    }
    console.error(error.stack ?? error.message);
    process.exit(1);
  },
);
