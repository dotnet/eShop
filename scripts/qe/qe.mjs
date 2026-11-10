#!/usr/bin/env node
// QE stage of the AI pipeline: verified test plans and automated tests. See docs/ai-pipeline/qe.md.
import { parseArgs as parse, readPayload, readStdin, requireStory, runMain } from '../pipeline/lib/cli.mjs';
import { isDisabled, loadConfig } from '../pipeline/lib/config.mjs';
import { changedFiles, fsReader, git, gitReader, mergeBase, revExists } from '../pipeline/lib/git.mjs';
import { loadState } from '../pipeline/lib/state.mjs';
import { CASE_ID, activeStory, listStories, storiesFromPaths } from '../pipeline/lib/stories.mjs';
import { log, setLogTag } from '../pipeline/lib/util.mjs';
import { calibrateCommand, lessonsCommand } from './lib/lessons.mjs';
import { computeMetrics, formatMetrics } from './lib/metrics.mjs';
import { automationFiles, automationStatus, planStatus } from './lib/plans.mjs';
import { automationLoop, hookStop, planLoop, sessionContext, spawnQe } from './lib/workflows.mjs';

setLogTag('qe');

const USAGE = `Usage: node scripts/qe/qe.mjs <command> [options]

Workflow 1 (story -> verified test plan)
  plan <story...> | --stale | --all [--force] [--background]
  check [--changed-from <ref>] [--rev <rev>] [--all] [--pre-push]

Workflow 2 (review-approved implementation -> verified automated tests)
  automate <story> [--impl-hash <h>] [--skip-tests] [--background]
  status [story]

Observability and improvement
  metrics [--json] [--since <iso-date>]
  lessons [--dry-run] [--local] [--if-due] [--no-push]
  calibrate [--stage plan|automation] [--sample <n>]

Hooks (called by scripts/pipeline/pipeline.mjs)
  hook stop | hook session-start | hook story-start <story>
  git-hook post-commit | post-merge | pre-push`;

const VALUE_FLAGS = new Set(['changed-from', 'rev', 'impl-hash', 'since', 'stage', 'sample']);
const parseArgs = (argv) => parse(argv, VALUE_FLAGS);

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

// Plans and tests for every story touched by the pushed commits must be verified and current.
function prePush(config, input) {
  if (process.env.QE_SKIP === '1') return 0;
  let failed = 0;
  for (const line of input.split('\n').filter(Boolean)) {
    const [, localSha, , remoteSha] = line.split(' ');
    if (!localSha || /^0+$/.test(localSha)) continue;
    const from = /^0+$/.test(remoteSha ?? '') || !revExists(config.root, remoteSha) ? mergeBase(config.root, config.baseRef) : remoteSha;
    const reader = gitReader(config.root, localSha);
    failed += checkStories(config, touchedStories(config, changedFiles(config.root, from, localSha), reader), reader);
  }
  if (failed) console.error('\nQE: push blocked. Run `node scripts/qe/qe.mjs plan <story>` (and commit the plan) or re-run `automate`. Bypass once with QE_SKIP=1.');
  return failed ? 1 : 0;
}

async function main() {
  const [command, ...rest] = process.argv.slice(2);
  const { positional, flags } = parseArgs(rest);
  if (!command || command === 'help' || flags.help) {
    console.log(USAGE);
    return 0;
  }
  const config = loadConfig('qe');
  if (process.env.QE_REQUIRED_SCORE) config.loops.requiredScore = Number(process.env.QE_REQUIRED_SCORE);

  switch (command) {
    case 'plan': {
      if (flags.background) {
        const args = ['plan', ...positional, ...Object.keys(flags).filter((f) => f !== 'background').map((f) => `--${f}`)];
        const bg = spawnQe(config, positional[0] ?? 'batch', 'plan', args);
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
      if (flags['pre-push']) return prePush(config, readStdin());
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

    case 'automate': {
      const story = requireStory(config, positional[0], { fallbackActive: true });
      if (flags.background) {
        const args = ['automate', story.key, ...(flags['impl-hash'] ? ['--impl-hash', flags['impl-hash']] : []), ...(flags['skip-tests'] ? ['--skip-tests'] : [])];
        const bg = spawnQe(config, story.key, 'automation', args);
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
        console.log(`${story.key.padEnd(10)} plan=${plan.state.padEnd(10)} cases=${String(plan.plan.cases.length).padEnd(3)} tests=${String(tests).padEnd(3)} automation=${automation.state}/${state.automation.status}`);
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
      if (event === 'story-start') {
        const story = requireStory(config, positional[1]);
        console.log(`Test plan for ${story.key}: ${planStatus(config, story).state}.`);
        return 0;
      }
      const payload = readPayload();
      const story = activeStory(config);
      if (event === 'stop') {
        const output = story ? hookStop(config, story, payload) : null;
        if (output) process.stdout.write(JSON.stringify(output));
        return 0;
      }
      if (event === 'session-start') {
        process.stdout.write(sessionContext(config, story, listStories(config)));
        return 0;
      }
      console.error(`Unknown hook event "${event}".`);
      return 2;
    }

    case 'git-hook': {
      const event = positional[0];
      if (event === 'pre-push') return prePush(config, readStdin());
      if (isDisabled() || git(config.root, ['config', '--bool', 'qe.autoPlan']).stdout.trim() === 'false') return 0;
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
        const bg = spawnQe(config, stories[0].key, 'plan', ['plan', ...stories.map((s) => s.key)]);
        log(`stories changed (${stories.map((s) => s.key).join(', ')}); writing test plans in the background (log ${bg.log}).`);
      }
      if (event === 'post-merge' && git(config.root, ['config', '--bool', 'qe.autoLessons']).stdout.trim() === 'true') {
        spawnQe(config, 'lessons', 'lessons', ['lessons', '--if-due']);
      }
      return 0;
    }

    default:
      console.error(`Unknown command "${command}".\n\n${USAGE}`);
      return 2;
  }
}

runMain(main, 'qe');
