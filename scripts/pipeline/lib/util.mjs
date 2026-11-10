import { createHash } from 'node:crypto';
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';

export const sha = (text, length = 12) => createHash('sha256').update(text).digest('hex').slice(0, length);

export const nowIso = () => new Date().toISOString();

let logTag = 'pipeline';
export const setLogTag = (tag) => (logTag = tag);

export function log(...args) {
  process.stderr.write(`[${logTag}] ${args.join(' ')}\n`);
}

export function run(cmd, args, { cwd, input, env, timeoutSec, allowFail = true } = {}) {
  const result = spawnSync(cmd, args, {
    cwd,
    input,
    env: env ?? process.env,
    encoding: 'utf8',
    maxBuffer: 256 * 1024 * 1024,
    timeout: timeoutSec ? timeoutSec * 1000 : undefined,
  });
  const out = {
    status: result.status ?? (result.error ? 127 : 1),
    stdout: result.stdout ?? '',
    stderr: (result.stderr ?? '') + (result.error ? `\n${result.error.message}` : ''),
    timedOut: result.error?.code === 'ETIMEDOUT',
  };
  if (!allowFail && out.status !== 0) {
    throw new Error(`${cmd} ${args.join(' ')} failed (${out.status}): ${out.stderr.trim()}`);
  }
  return out;
}

export function readJson(file, fallback) {
  try {
    return JSON.parse(fs.readFileSync(file, 'utf8'));
  } catch {
    return fallback;
  }
}

export function writeJson(file, value) {
  fs.mkdirSync(path.dirname(file), { recursive: true });
  const tmp = `${file}.${process.pid}.tmp`;
  fs.writeFileSync(tmp, `${JSON.stringify(value, null, 2)}\n`);
  fs.renameSync(tmp, file);
}

export function appendJsonl(file, records) {
  const list = Array.isArray(records) ? records : [records];
  if (list.length === 0) return;
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.appendFileSync(file, list.map((r) => JSON.stringify(r)).join('\n') + '\n');
}

export function readJsonl(file) {
  if (!fs.existsSync(file)) return [];
  return fs
    .readFileSync(file, 'utf8')
    .split('\n')
    .filter((line) => line.trim())
    .flatMap((line) => {
      try {
        return [JSON.parse(line)];
      } catch {
        return [];
      }
    });
}

// Minimal YAML front matter support: scalars, quoted strings and inline [a, b] lists.
export function parseFrontMatter(text) {
  const match = /^---\r?\n([\s\S]*?)\r?\n---\r?\n?/.exec(text);
  if (!match) return { data: {}, body: text, hasFrontMatter: false };
  const data = {};
  for (const line of match[1].split(/\r?\n/)) {
    const kv = /^([A-Za-z0-9_-]+):\s*(.*)$/.exec(line);
    if (!kv) continue;
    data[kv[1]] = parseScalar(kv[2].trim());
  }
  return { data, body: text.slice(match[0].length), hasFrontMatter: true };
}

function parseScalar(value) {
  if (value === '') return '';
  if (value.startsWith('[') && value.endsWith(']')) {
    try {
      const parsed = JSON.parse(value);
      if (Array.isArray(parsed)) return parsed;
    } catch {
      // not JSON; fall back to a plain comma split
    }
    return value
      .slice(1, -1)
      .split(',')
      .map((v) => parseScalar(v.trim()))
      .filter((v) => v !== '');
  }
  if (/^".*"$/.test(value) || /^'.*'$/.test(value)) return value.slice(1, -1);
  if (/^-?\d+(\.\d+)?$/.test(value)) return Number(value);
  if (value === 'true' || value === 'false') return value === 'true';
  if (value === 'null') return null;
  return value;
}

export function stringifyFrontMatter(data, body) {
  const lines = Object.entries(data)
    .filter(([, v]) => v !== undefined)
    .map(([k, v]) => `${k}: ${formatScalar(v)}`);
  return `---\n${lines.join('\n')}\n---\n${body.startsWith('\n') ? body.slice(1) : body}`;
}

function formatScalar(value) {
  if (Array.isArray(value)) return `[${value.map((v) => JSON.stringify(v)).join(', ')}]`;
  if (value === null) return 'null';
  if (typeof value === 'string' && (value === '' || /[:#[\]{},"']|^\s|\s$/.test(value))) return JSON.stringify(value);
  return String(value);
}

// Agents end their final message with `AGENT_RESULT: {json}`. Take the last marker and parse the
// balanced JSON object that follows it (it may span several lines or sit in a code fence).
export function extractResult(text, marker = 'AGENT_RESULT') {
  const index = text.lastIndexOf(`${marker}:`);
  if (index < 0) return null;
  const start = text.indexOf('{', index);
  if (start < 0) return null;
  let depth = 0;
  let inString = false;
  let escaped = false;
  for (let i = start; i < text.length; i++) {
    const ch = text[i];
    if (inString) {
      if (escaped) escaped = false;
      else if (ch === '\\') escaped = true;
      else if (ch === '"') inString = false;
      continue;
    }
    if (ch === '"') inString = true;
    else if (ch === '{') depth++;
    else if (ch === '}') {
      depth--;
      if (depth === 0) {
        try {
          return JSON.parse(text.slice(start, i + 1));
        } catch {
          return null;
        }
      }
    }
  }
  return null;
}

export function tail(text, chars = 4000) {
  return text.length > chars ? `…${text.slice(-chars)}` : text;
}

export function isPidAlive(pid) {
  if (!pid) return false;
  try {
    process.kill(pid, 0);
    return true;
  } catch (error) {
    return error.code === 'EPERM';
  }
}

export function acquireLock(file) {
  fs.mkdirSync(path.dirname(file), { recursive: true });
  try {
    fs.writeFileSync(file, String(process.pid), { flag: 'wx' });
    return true;
  } catch {
    if (lockHolder(file)) return false;
    fs.writeFileSync(file, String(process.pid));
    return true;
  }
}

export function releaseLock(file) {
  try {
    if (Number(fs.readFileSync(file, 'utf8')) === process.pid) fs.unlinkSync(file);
  } catch {
    // already released
  }
}

export function lockHolder(file) {
  try {
    const pid = Number(fs.readFileSync(file, 'utf8'));
    return isPidAlive(pid) ? pid : null;
  } catch {
    return null;
  }
}

export const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
