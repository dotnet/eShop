import fs from 'node:fs';
import { activeStory, resolveStory } from './stories.mjs';

export function parseArgs(argv, valueFlags = new Set()) {
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
    else if (argv[i + 1] && !argv[i + 1].startsWith('--') && valueFlags.has(key)) flags[key] = argv[++i];
    else flags[key] = true;
  }
  return { positional, flags };
}

export function readStdin() {
  if (process.stdin.isTTY) return '';
  try {
    return fs.readFileSync(0, 'utf8');
  } catch {
    return '';
  }
}

export function readPayload() {
  try {
    return JSON.parse(readStdin() || '{}');
  } catch {
    return {};
  }
}

export function requireStory(config, arg, { fallbackActive = false } = {}) {
  const story = arg ? resolveStory(config, arg) : fallbackActive ? activeStory(config) : null;
  if (!story) {
    console.error(
      arg
        ? `Unknown story "${arg}".`
        : 'No story given and no active story (run `node scripts/pipeline/pipeline.mjs start <story>` or use a branch named after the story).',
    );
    process.exit(2);
  }
  return story;
}

// Hooks must never break the agent or git: on a crash the stop hook surfaces the error as a message
// and every hook exits 0.
export function runMain(main, tag) {
  main().then(
    (code) => process.exit(code ?? 0),
    (error) => {
      const [command, event] = process.argv.slice(2);
      if (command === 'hook' || (command === 'git-hook' && event !== 'pre-push')) {
        if (event === 'stop') process.stdout.write(JSON.stringify({ systemMessage: `${tag} hook error: ${error.message}` }));
        else console.error(`[${tag}] hook error: ${error.message}`);
        process.exit(0);
      }
      console.error(error.stack ?? error.message);
      process.exit(1);
    },
  );
}
