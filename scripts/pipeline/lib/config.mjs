import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { run } from './util.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
export const PIPELINE_DIR = path.resolve(here, '..');
export const SCRIPTS_DIR = path.resolve(PIPELINE_DIR, '..');

export function repoRoot(cwd = process.cwd()) {
  if (process.env.PIPELINE_ROOT) return path.resolve(process.env.PIPELINE_ROOT);
  const result = run('git', ['rev-parse', '--show-toplevel'], { cwd });
  if (result.status !== 0) throw new Error('the AI pipeline must run inside a git repository');
  return result.stdout.trim();
}

const isObject = (v) => v && typeof v === 'object' && !Array.isArray(v);

export function deepMerge(base, extra) {
  const out = { ...base };
  for (const [key, value] of Object.entries(extra ?? {})) {
    out[key] = isObject(value) && isObject(base?.[key]) ? deepMerge(base[key], value) : value;
  }
  return out;
}

const readConfig = (file) => (fs.existsSync(file) ? JSON.parse(fs.readFileSync(file, 'utf8')) : {});

const cache = new Map();

// Shared settings come from scripts/pipeline/config.json; a stage's scripts/<stage>/config.json is
// merged on top, so each stage only lists what it adds or overrides.
export function loadConfig(stage = null, root = repoRoot()) {
  const cacheKey = `${root}\0${stage ?? ''}`;
  if (cache.has(cacheKey)) return cache.get(cacheKey);
  let config = readConfig(path.join(PIPELINE_DIR, 'config.json'));
  if (stage) config = deepMerge(config, readConfig(path.join(SCRIPTS_DIR, stage, 'config.json')));
  config.root = root;
  config.stage = stage;
  config.agent.cli = process.env.PIPELINE_AGENT_CLI || config.agent.cli;
  config.agent.cmd = process.env.PIPELINE_AGENT_CMD || null;
  config.baseRef = process.env.PIPELINE_BASE_REF || config.baseRef;
  config.abs = (rel) => path.join(root, rel);
  cache.set(cacheKey, config);
  return config;
}

export const isDisabled = () => process.env.PIPELINE_DISABLE === '1' || process.env.PIPELINE_CHILD === '1';
