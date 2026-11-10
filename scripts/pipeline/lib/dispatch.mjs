import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { PIPELINE_DIR } from './config.mjs';
import { tail } from './util.mjs';

export function loadStages() {
  return JSON.parse(fs.readFileSync(path.join(PIPELINE_DIR, 'stages.json'), 'utf8')).stages;
}

// Every stage is its own CLI, run as a separate process, so a stage that crashes or hangs cannot take
// the others (or the agent) down with it.
export function runStage(config, stage, args, { input = '', inherit = false } = {}) {
  const result = spawnSync(process.execPath, [path.join(config.root, stage.cli), ...args], {
    cwd: config.root,
    input,
    encoding: 'utf8',
    env: process.env,
    maxBuffer: 64 * 1024 * 1024,
    stdio: inherit ? ['pipe', 'inherit', 'inherit'] : 'pipe',
  });
  return { status: result.status ?? 1, stdout: result.stdout ?? '', stderr: (result.stderr ?? '') + (result.error ? result.error.message : '') };
}

const forwardLogs = (result) => result.stderr.trim() && process.stderr.write(result.stderr.endsWith('\n') ? result.stderr : `${result.stderr}\n`);

// Stop / SubagentStop. Stages run in order; the first that blocks ends the turn's stop with its reason,
// so later stages (for example test automation) only run once earlier gates (the review) pass.
export function dispatchStop(config, stages, payload) {
  const messages = [];
  const input = JSON.stringify(payload ?? {});
  for (const stage of stages.filter((s) => s.hooks.includes('stop'))) {
    const result = runStage(config, stage, ['hook', 'stop'], { input });
    forwardLogs(result);
    if (result.status !== 0) {
      messages.push(`AI pipeline: the ${stage.name} stop hook failed (exit ${result.status}): ${tail(result.stderr.trim(), 500)}`);
      continue;
    }
    const out = result.stdout.trim();
    if (!out) continue;
    let output;
    try {
      output = JSON.parse(out);
    } catch {
      messages.push(`AI pipeline: the ${stage.name} stop hook printed invalid JSON: ${out.slice(0, 200)}`);
      continue;
    }
    if (output.decision === 'block') {
      return { decision: 'block', reason: output.reason, ...(messages.length ? { systemMessage: messages.join('\n') } : {}) };
    }
    if (output.systemMessage) messages.push(output.systemMessage);
  }
  return messages.length ? { systemMessage: messages.join('\n') } : null;
}

// SessionStart. Each stage prints plain text; the dispatcher joins it and wraps it once.
export function dispatchSessionStart(config, stages, payload, header = []) {
  const parts = [...header];
  const input = JSON.stringify(payload ?? {});
  for (const stage of stages.filter((s) => s.hooks.includes('session-start'))) {
    const result = runStage(config, stage, ['hook', 'session-start'], { input });
    forwardLogs(result);
    if (result.status === 0 && result.stdout.trim()) parts.push(result.stdout.trim());
  }
  return parts.join('\n');
}

// Copilot reads only a top-level additionalContext and ignores plain text; Claude Code and Cursor read
// the nested form and tolerate the extra key. Codex rejects unknown keys, so its config passes --nested.
export function sessionStartOutput(text, { nested = false } = {}) {
  if (!text) return null;
  const hookSpecificOutput = { hookEventName: 'SessionStart', additionalContext: text };
  return nested ? { hookSpecificOutput } : { additionalContext: text, hookSpecificOutput };
}

export function dispatchText(config, stages, kind, event, args = []) {
  const out = [];
  for (const stage of stages.filter((s) => s[kind].includes(event))) {
    const result = runStage(config, stage, [kind === 'hooks' ? 'hook' : event, ...(kind === 'hooks' ? [event] : []), ...args]);
    forwardLogs(result);
    if (result.status !== 0) throw new Error(`${stage.name} ${event} failed (exit ${result.status})`);
    out.push({ stage: stage.name, stdout: result.stdout });
  }
  return out;
}

// Git hooks: output goes straight to the terminal. Only pre-push can fail; it fails if any stage does.
export function dispatchGitHook(config, stages, event, input) {
  let failed = 0;
  for (const stage of stages.filter((s) => s.gitHooks.includes(event))) {
    const result = runStage(config, stage, ['git-hook', event], { input, inherit: true });
    if (result.status !== 0) failed++;
  }
  return event === 'pre-push' && failed ? 1 : 0;
}
