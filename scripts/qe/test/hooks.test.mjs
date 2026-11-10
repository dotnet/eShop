import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import test from 'node:test';
import { fileURLToPath } from 'node:url';

const adapter = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../hooks/gemini-adapter.mjs');

// Runs the adapter around an inline node "hook" that echoes its stdin into a Claude-format reply.
function run(event, geminiInput, hookBody) {
  const hook = `let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{const p=JSON.parse(s);${hookBody}});`;
  const res = spawnSync(process.execPath, [adapter, event, '--', process.execPath, '-e', hook], {
    input: JSON.stringify(geminiInput),
    encoding: 'utf8',
  });
  assert.equal(res.status, 0);
  return JSON.parse(res.stdout);
}

test('gemini adapter maps a Claude Stop block to a Gemini AfterAgent deny', () => {
  const out = run('Stop', { session_id: 's1', stop_hook_active: true }, `
    process.stdout.write(JSON.stringify({ decision: p.stop_hook_active ? 'block' : 'none', reason: 'fix ' + p.hook_event_name }));`);
  assert.deepEqual(out, { decision: 'deny', reason: 'fix Stop' });
});

test('gemini adapter maps tool names and paths in, and a nested PreToolUse deny out', () => {
  const out = run('PreToolUse', { session_id: 's2', tool_name: 'read_file', tool_input: { absolute_path: '/x/y.cs' } }, `
    process.stdout.write(JSON.stringify({ hookSpecificOutput: { hookEventName: 'PreToolUse', permissionDecision: 'deny',
      permissionDecisionReason: p.tool_name + ' ' + p.tool_input.file_path + ' ' + p.session_id } }));`);
  assert.deepEqual(out, { decision: 'deny', reason: 'Read /x/y.cs s2' });
});

test('gemini adapter passes advisory context through and treats plain SessionStart text as context', () => {
  const advice = run('PreToolUse', { tool_name: 'search_file_content' }, `
    process.stdout.write(JSON.stringify({ hookSpecificOutput: { additionalContext: 'use ' + p.tool_name } }));`);
  assert.deepEqual(advice, { hookSpecificOutput: { additionalContext: 'use Grep' } });
  const start = run('SessionStart', { source: 'resume' }, `process.stdout.write('ctx ' + p.source);`);
  assert.deepEqual(start, { hookSpecificOutput: { hookEventName: 'SessionStart', additionalContext: 'ctx resume' } });
});

test('gemini adapter fails open with empty JSON when the hook command is missing', () => {
  const res = spawnSync(process.execPath, [adapter, 'Stop', '--', 'definitely-not-a-command-xyz'], { input: '{}', encoding: 'utf8' });
  assert.equal(res.status, 0);
  assert.equal(res.stdout, '{}');
});
