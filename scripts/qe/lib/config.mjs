import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { run } from './util.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
export const QE_SCRIPT = path.resolve(here, '..', 'qe.mjs');

export function repoRoot(cwd = process.cwd()) {
  if (process.env.QE_ROOT) return path.resolve(process.env.QE_ROOT);
  const result = run('git', ['rev-parse', '--show-toplevel'], { cwd });
  if (result.status !== 0) throw new Error('qe must run inside a git repository');
  return result.stdout.trim();
}

let cached;

export function loadConfig(root = repoRoot()) {
  if (cached?.root === root) return cached;
  const repoConfig = path.join(root, 'scripts/qe/config.json');
  const file = fs.existsSync(repoConfig) ? repoConfig : path.resolve(here, '..', 'config.json');
  const config = JSON.parse(fs.readFileSync(file, 'utf8'));
  config.root = root;
  config.agent.cli = process.env.QE_AGENT_CLI || config.agent.cli;
  config.agent.cmd = process.env.QE_AGENT_CMD || null;
  config.baseRef = process.env.QE_BASE_REF || config.baseRef;
  if (process.env.QE_REQUIRED_SCORE) config.loops.requiredScore = Number(process.env.QE_REQUIRED_SCORE);
  config.abs = (rel) => path.join(root, rel);
  cached = config;
  return config;
}

export const isDisabled = () => process.env.QE_DISABLE === '1' || process.env.QE_PIPELINE_CHILD === '1';
