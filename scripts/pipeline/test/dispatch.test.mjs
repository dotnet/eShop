import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { after, test } from 'node:test';
import { dispatchGitHook, dispatchSessionStart, dispatchStop, sessionStartOutput } from '../lib/dispatch.mjs';

const root = fs.mkdtempSync(path.join(os.tmpdir(), 'pipeline-dispatch-'));
after(() => fs.rmSync(root, { recursive: true, force: true }));

// A stage is any CLI; these inline ones record that they ran and reply with a fixed hook output.
function stage(name, { stop, context = '', exit = 0, gitExit = 0 }) {
  const cli = `${name}.mjs`;
  fs.writeFileSync(
    path.join(root, cli),
    `import fs from 'node:fs';
const [cmd, event] = process.argv.slice(2);
fs.appendFileSync(${JSON.stringify(path.join(root, 'ran.log'))}, '${name}:' + event + '\\n');
if (cmd === 'git-hook') process.exit(${gitExit});
if (${exit}) { console.error('${name} exploded'); process.exit(${exit}); }
if (event === 'stop' && ${JSON.stringify(stop ?? null)}) process.stdout.write(JSON.stringify(${JSON.stringify(stop ?? null)}));
if (event === 'session-start') process.stdout.write(${JSON.stringify(context)});
`,
  );
  return { name, cli, hooks: ['stop', 'session-start'], gitHooks: ['pre-push'] };
}
const ran = () => {
  const file = path.join(root, 'ran.log');
  const lines = fs.existsSync(file) ? fs.readFileSync(file, 'utf8').trim().split('\n') : [];
  fs.rmSync(file, { force: true });
  return lines;
};
const config = { root };

test('stop: a blocking stage ends dispatch, and earlier messages travel with the block', () => {
  const stages = [
    stage('a', { stop: { systemMessage: 'a ran' } }),
    stage('b', { stop: { decision: 'block', reason: 'fix it' } }),
    stage('c', { stop: { systemMessage: 'c ran' } }),
  ];
  assert.deepEqual(dispatchStop(config, stages, {}), { decision: 'block', reason: 'fix it', systemMessage: 'a ran' });
  assert.deepEqual(ran(), ['a:stop', 'b:stop'], 'stages after the block do not run');
});

test('stop: a crashing stage becomes a message and the next stage still runs', () => {
  const out = dispatchStop(config, [stage('boom', { exit: 3 }), stage('ok', { stop: { systemMessage: 'ok ran' } })], {});
  assert.match(out.systemMessage, /the boom stop hook failed \(exit 3\): boom exploded\nok ran/);
  assert.equal(out.decision, undefined);
  ran();
});

test('session-start: stage contexts are joined under the header and wrapped in both shapes', () => {
  const text = dispatchSessionStart(config, [stage('a', { context: 'A says hi' }), stage('b', {}), stage('c', { context: 'C says hi' })], {}, ['header']);
  assert.equal(text, 'header\nA says hi\nC says hi');
  assert.deepEqual(sessionStartOutput(text, { nested: true }), { hookSpecificOutput: { hookEventName: 'SessionStart', additionalContext: text } });
  assert.equal(sessionStartOutput(''), null);
  ran();
});

test('git hooks: pre-push fails when any stage fails, and every stage still runs', () => {
  assert.equal(dispatchGitHook(config, [stage('a', { gitExit: 1 }), stage('b', {})], 'pre-push', ''), 1);
  assert.deepEqual(ran(), ['a:pre-push', 'b:pre-push']);
  assert.equal(dispatchGitHook(config, [stage('a', {})], 'pre-push', ''), 0);
  ran();
});
