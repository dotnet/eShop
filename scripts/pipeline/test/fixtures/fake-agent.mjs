#!/usr/bin/env node
// Scripted stand-in for an agent CLI, used by the end-to-end tests through PIPELINE_AGENT_CMD.
// Behavior per agent comes from PIPELINE_FAKE_SCRIPT (JSON: { "<agent>": [response, response, ...] });
// each call consumes the next response for that agent (the last one repeats).
import fs from 'node:fs';
import path from 'node:path';

if (!process.env.PIPELINE_FAKE_SCRIPT) process.exit(0); // not a test file; only runs under the e2e tests

const prompt = fs.readFileSync(0, 'utf8');
const agent = process.env.PIPELINE_AGENT_NAME;
const script = JSON.parse(fs.readFileSync(process.env.PIPELINE_FAKE_SCRIPT, 'utf8'));
const counterFile = `${process.env.PIPELINE_FAKE_SCRIPT}.calls.json`;
// Agents can run in parallel (the PR check's critics), so the shared counter update is locked.
const lock = `${counterFile}.lock`;
for (let i = 0; ; i++) {
  try {
    fs.writeFileSync(lock, String(process.pid), { flag: 'wx' });
    break;
  } catch {
    if (i > 2000) break;
    Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, 5);
  }
}
const calls = fs.existsSync(counterFile) ? JSON.parse(fs.readFileSync(counterFile, 'utf8')) : {};
const n = calls[agent] ?? 0;
calls[agent] = n + 1;
fs.writeFileSync(counterFile, JSON.stringify(calls));
fs.rmSync(lock, { force: true });
fs.appendFileSync(`${process.env.PIPELINE_FAKE_SCRIPT}.prompts.log`, `\n=== ${agent} #${n + 1} (child=${process.env.PIPELINE_CHILD}) ===\n${prompt}\n`);

const steps = script[agent] ?? [];
const step = steps[Math.min(n, steps.length - 1)] ?? {};
const field = (name) => new RegExp(`^${name}: (\\S+)`, 'm').exec(prompt)?.[1];

if (step.writePlan) {
  const dir = field('Plan directory');
  const key = field('Story');
  fs.mkdirSync(dir, { recursive: true });
  const indexFile = path.join(dir, '_index.md');
  const existing = fs.existsSync(indexFile) ? fs.readFileSync(indexFile, 'utf8') : '';
  const frontMatter = /^---\n[\s\S]*?\n---\n/.exec(existing)?.[0] ?? '';
  const ids = step.writePlan.map((_, i) => `TC-${key}-0${i + 1}`);
  const rows = step.writePlan.map((c, i) => `| ${ids[i]} | ${c.title} | ${c.type} | functional | rule |`).join('\n');
  fs.writeFileSync(indexFile, `${frontMatter}# Test plan: ${key}\n\n| ID | Title | Type | Layer | Covers |\n| --- | --- | --- | --- | --- |\n${rows}\n`);
  for (const f of fs.readdirSync(dir).filter((f) => f.startsWith('TC-'))) fs.rmSync(path.join(dir, f));
  step.writePlan.forEach((c, i) => {
    const sad = c.type === 'sad' ? '\nsad-path: validation' : '';
    fs.writeFileSync(
      path.join(dir, `${ids[i]}-case.md`),
      `---\nid: ${ids[i]}\ntitle: ${c.title}\ntype: ${c.type}${sad}\nlayer: functional\ncovers: ["Rule, part 1"]\n---\n\n# ${ids[i]}: ${c.title}\n\n## Preconditions\n\n- data\n\n## Steps\n\n1. call\n\n## Expected result\n\n- ${c.expected ?? '200'}\n`,
    );
  });
}

if (step.writeTests) {
  const key = field('Story');
  const scenario = /\(name: (\S+)\)/.exec(prompt)[1];
  fs.mkdirSync(`testing/scenarios/${scenario}`, { recursive: true });
  fs.writeFileSync(`testing/scenarios/${scenario}/scenario.yaml`, 'extends: default\n');
  fs.mkdirSync('e2e/teststack', { recursive: true });
  const tests = step.writeTests.map((i) => `test('TC-${key}-0${i} works', async () => {});`).join('\n');
  fs.writeFileSync(`e2e/teststack/${scenario}.spec.ts`, `${tests}\n`);
}

if (step.writeImpl) {
  fs.mkdirSync(path.dirname(step.writeImpl), { recursive: true });
  fs.writeFileSync(step.writeImpl, `// fixed ${n}\n`);
}

process.stdout.write(`Working...\n${step.result ? `AGENT_RESULT: ${JSON.stringify(step.result)}\n` : ''}`);
process.exit(step.exit ?? 0);
