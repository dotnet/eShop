import fs from 'node:fs';
import path from 'node:path';
import { log, readJson, writeJson } from './util.mjs';

const emptyState = (key) => ({
  story: key,
  done: null,
  review: { rounds: 0, lastHash: null, lastVerdict: null, lastReason: null, approvedHash: null, unchangedBlocks: 0 },
  automation: { status: 'idle', requestedHash: null, verifiedHash: null, pid: null, log: null, bugs: [] },
  plan: { status: 'idle', pid: null, log: null },
});

export const stateDir = (config) => path.join(config.root, config.stateDir);
export const metricsFile = (config, name) => path.join(config.root, config.metricsDir, name);
const escalationFile = (config, key, stage) => path.join(stateDir(config), 'escalations', `${key}-${stage}.md`);

// A loop that cannot converge stops and leaves a note for a human; the next agent session is told.
export function escalate(config, story, stage, title, details) {
  const file = escalationFile(config, story.key, stage);
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, `# ${story.key} ${stage}: ${title}\n\n${new Date().toISOString()}\n\n${details}\n`);
  log(`ESCALATED ${story.key} ${stage}: ${title} (see ${path.relative(config.root, file)})`);
  return file;
}

export function clearEscalation(config, story, stage) {
  fs.rmSync(escalationFile(config, story.key, stage), { force: true });
}

export function listEscalations(config) {
  const dir = path.join(stateDir(config), 'escalations');
  return fs.existsSync(dir) ? fs.readdirSync(dir).filter((f) => f.endsWith('.md')).map((f) => f.replace(/\.md$/, '')) : [];
}

export function setActiveStory(config, key) {
  fs.mkdirSync(stateDir(config), { recursive: true });
  fs.writeFileSync(path.join(stateDir(config), 'active-story'), `${key}\n`);
}
const stateFile = (config, key) => path.join(stateDir(config), 'stories', `${key}.json`);

export function loadState(config, key) {
  const state = readJson(stateFile(config, key), null) ?? emptyState(key);
  const base = emptyState(key);
  return { ...base, ...state, review: { ...base.review, ...state.review }, automation: { ...base.automation, ...state.automation }, plan: { ...base.plan, ...state.plan } };
}

export function saveState(config, state) {
  writeJson(stateFile(config, state.story), state);
}

export function updateState(config, key, mutate) {
  const state = loadState(config, key);
  mutate(state);
  saveState(config, state);
  return state;
}

export function allStates(config) {
  const dir = path.join(stateDir(config), 'stories');
  if (!fs.existsSync(dir)) return [];
  return fs
    .readdirSync(dir)
    .filter((f) => f.endsWith('.json'))
    .map((f) => readJson(path.join(dir, f), null))
    .filter(Boolean);
}

export const lockFile = (config, key, stage) => path.join(stateDir(config), 'locks', `${key}-${stage}.lock`);

export function logFile(config, key, stage) {
  const stamp = new Date().toISOString().replace(/[:.]/g, '-');
  return path.join(stateDir(config), 'logs', `${key}-${stage}-${stamp}.log`);
}
