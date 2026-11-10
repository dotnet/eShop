#!/usr/bin/env node
// Review stage of the AI pipeline: architecture review of the implementation. See docs/ai-pipeline/review.md.
import { parseArgs, readPayload, requireStory, runMain } from '../pipeline/lib/cli.mjs';
import { isDisabled, loadConfig } from '../pipeline/lib/config.mjs';
import { loadState } from '../pipeline/lib/state.mjs';
import { activeStory, listStories } from '../pipeline/lib/stories.mjs';
import { setLogTag } from '../pipeline/lib/util.mjs';
import { architectureReview, hookStop, resetReview, reviewMetrics, sessionContext } from './lib/review.mjs';

setLogTag('review');

const USAGE = `Usage: node scripts/review/review.mjs <command> [options]

  run [story]                   run one architecture review round now and print the verdict
  status [story]
  metrics [--json] [--since <iso-date>]

Hooks (called by scripts/pipeline/pipeline.mjs)
  hook stop | hook session-start | hook story-start <story>`;

async function main() {
  const [command, ...rest] = process.argv.slice(2);
  const { positional, flags } = parseArgs(rest, new Set(['since']));
  if (!command || command === 'help' || flags.help) {
    console.log(USAGE);
    return 0;
  }
  const config = loadConfig('review');

  switch (command) {
    case 'run': {
      const story = requireStory(config, positional[0], { fallbackActive: true });
      console.log(JSON.stringify(architectureReview(config, story, loadState(config, story.key).review.rounds + 1), null, 2));
      return 0;
    }

    case 'status': {
      const stories = positional.length ? positional.map((a) => requireStory(config, a)) : listStories(config);
      for (const story of stories) {
        const { review } = loadState(config, story.key);
        console.log(`${story.key.padEnd(10)} review=${review.lastVerdict ?? '-'}${review.rounds ? ` (round ${review.rounds})` : ''}${review.approvedHash ? ` approved=${review.approvedHash}` : ''}`);
        if (positional.length && review.lastReason) console.log(`\nLast review:\n${review.lastReason}`);
      }
      return 0;
    }

    case 'metrics': {
      const m = reviewMetrics(config, { since: flags.since });
      if (flags.json) console.log(JSON.stringify(m, null, 2));
      else {
        console.log(`Architecture reviews: ${m.rounds} rounds over ${m.stories} stories, ${m.approvals} approvals, first-round approval rate ${m.firstRoundApprovalRate}`);
        const cats = Object.entries(m.findingsByCategory).map(([c, n]) => `${c}=${n}`).join(' ');
        console.log(`Findings by category: ${cats || 'none'}`);
      }
      return 0;
    }

    case 'hook': {
      if (isDisabled()) return 0;
      const event = positional[0];
      if (event === 'story-start') {
        resetReview(config, requireStory(config, positional[1]));
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
        process.stdout.write(sessionContext(config, story));
        return 0;
      }
      console.error(`Unknown hook event "${event}".`);
      return 2;
    }

    default:
      console.error(`Unknown command "${command}".\n\n${USAGE}`);
      return 2;
  }
}

runMain(main, 'review');
