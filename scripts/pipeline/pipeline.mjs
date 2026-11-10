#!/usr/bin/env node
// AI development pipeline: one entry point for agent and git lifecycle hooks, dispatched to the
// stages listed in stages.json. See docs/ai-pipeline/README.md.
import { readPayload, readStdin, parseArgs, requireStory, runMain } from './lib/cli.mjs';
import { isDisabled, loadConfig } from './lib/config.mjs';
import { dispatchGitHook, dispatchSessionStart, dispatchStop, dispatchText, loadStages, sessionStartOutput } from './lib/dispatch.mjs';
import { implChanges } from './lib/git.mjs';
import { listEscalations, loadState, setActiveStory, updateState } from './lib/state.mjs';
import { activeStory } from './lib/stories.mjs';
import { nowIso } from './lib/util.mjs';

const CMD = 'node scripts/pipeline/pipeline.mjs';

const USAGE = `Usage: ${CMD} <command> [options]

Implementation agent
  start <story>                 set the story you are working on (resets its review)
  done [story]                  mark the line of work complete; the stages run when your turn ends
  status [story]                state of every stage for a story

Pipeline
  stages                        list the stages and the events they handle
  metrics [--json] [--since <iso-date>]

Hooks (called by agent tool configs and .githooks)
  hook stop | hook session-start [--nested]
  git-hook post-commit | post-merge | pre-push`;

async function main() {
  const [command, ...rest] = process.argv.slice(2);
  const { positional, flags } = parseArgs(rest, new Set(['since']));
  if (!command || command === 'help' || flags.help) {
    console.log(USAGE);
    return 0;
  }
  const config = loadConfig();
  const stages = loadStages();

  switch (command) {
    case 'start': {
      const story = requireStory(config, positional[0]);
      setActiveStory(config, story.key);
      updateState(config, story.key, (s) => (s.done = null));
      console.log(`Active story: ${story.key}.`);
      for (const { stdout } of dispatchText(config, stages, 'hooks', 'story-start', [story.key])) if (stdout.trim()) console.log(stdout.trim());
      console.log(`Run \`${CMD} done\` when the line of work is complete.`);
      return 0;
    }

    case 'done': {
      const story = requireStory(config, positional[0], { fallbackActive: true });
      const { hash, files } = implChanges(config);
      updateState(config, story.key, (s) => (s.done = { at: nowIso(), hash }));
      console.log(`${story.key} marked complete (${files.length} implementation file(s) changed). The pipeline stages run when this agent turn ends.`);
      return 0;
    }

    case 'status': {
      const story = positional[0] ? requireStory(config, positional[0]) : activeStory(config);
      if (story) console.log(`Active story: ${story.key} (${loadState(config, story.key).done ? 'done' : 'in progress'})`);
      for (const { stage, stdout } of dispatchText(config, stages, 'commands', 'status', story ? [story.key] : [])) {
        console.log(`\n[${stage}]\n${stdout.trimEnd()}`);
      }
      const escalations = listEscalations(config);
      if (escalations.length) console.log(`\nEscalations: ${escalations.join(', ')}`);
      return 0;
    }

    case 'metrics': {
      const args = [...(flags.json ? ['--json'] : []), ...(flags.since ? ['--since', flags.since] : [])];
      const results = dispatchText(config, stages, 'commands', 'metrics', args);
      if (flags.json) console.log(JSON.stringify(Object.fromEntries(results.map((r) => [r.stage, JSON.parse(r.stdout)])), null, 2));
      else for (const { stage, stdout } of results) console.log(`== ${stage} ==\n${stdout.trimEnd()}\n`);
      return 0;
    }

    case 'stages': {
      for (const s of stages) console.log(`${s.name.padEnd(8)} ${s.cli.padEnd(28)} hooks=${s.hooks.join(',') || '-'} git=${s.gitHooks.join(',') || '-'}\n         ${s.description}`);
      return 0;
    }

    case 'hook': {
      if (isDisabled()) return 0;
      const event = positional[0];
      const payload = readPayload();
      if (event === 'stop') {
        const output = dispatchStop(config, stages, payload);
        if (output) process.stdout.write(JSON.stringify(output));
        return 0;
      }
      if (event === 'session-start') {
        const story = activeStory(config);
        const header = [];
        if (story) {
          header.push(`AI pipeline: active story ${story.key}.`);
          if (!loadState(config, story.key).done) header.push(`When your line of work for ${story.key} is complete, run \`${CMD} done\` before finishing so the review and test stages run.`);
        }
        const escalations = listEscalations(config);
        if (escalations.length) header.push(`Pipeline escalations needing a human: ${escalations.join(', ')} (.agent-state/escalations/).`);
        const output = sessionStartOutput(dispatchSessionStart(config, stages, payload, header), { nested: Boolean(flags.nested) });
        if (output) process.stdout.write(JSON.stringify(output));
        return 0;
      }
      console.error(`Unknown hook event "${event}".`);
      return 2;
    }

    case 'git-hook': {
      if (isDisabled()) return 0;
      const event = positional[0];
      if (!['post-commit', 'post-merge', 'pre-push'].includes(event)) {
        console.error(`Unknown git hook "${event}".`);
        return 2;
      }
      return dispatchGitHook(config, stages, event, event === 'pre-push' ? readStdin() : '');
    }

    default:
      console.error(`Unknown command "${command}".\n\n${USAGE}`);
      return 2;
  }
}

runMain(main, 'pipeline');
