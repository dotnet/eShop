import { spawn } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { logFile } from './state.mjs';

// Detached run of a stage CLI that outlives the hook that started it. Clearing PIPELINE_CHILD lets a
// background run started from inside a pipeline agent still do its own work.
export function spawnBackground(config, { script, key, stage, args, file = logFile(config, key, stage) }) {
  fs.mkdirSync(path.dirname(file), { recursive: true });
  const fd = fs.openSync(file, 'a');
  const env = { ...process.env, PIPELINE_BACKGROUND: '1' };
  delete env.PIPELINE_CHILD;
  const child = spawn(process.execPath, [script, ...args], { cwd: config.root, detached: true, stdio: ['ignore', fd, fd], env });
  child.unref();
  fs.closeSync(fd);
  return { pid: child.pid, log: path.relative(config.root, file) };
}
