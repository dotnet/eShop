#!/usr/bin/env node
// Runs a Claude Code-format hook command under Gemini CLI, which (unlike Copilot, Cursor and
// Devin) does not load .claude/settings.json or speak Claude's hook contract. It rewrites
// Gemini's stdin payload into Claude's shape, runs the command from the project root, and
// rewrites the command's Claude-format stdout into Gemini's output contract.
//
//   node scripts/hooks/gemini-adapter.mjs <Stop|SessionStart|PreToolUse> -- <command...>
//
// Always exits 0 with JSON on stdout (Gemini treats non-JSON as a message); a broken hook must
// never stop the agent.
import { spawnSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const TOOL_NAMES = {
  run_shell_command: 'Bash',
  read_file: 'Read',
  read_many_files: 'Read',
  search_file_content: 'Grep',
  grep_search: 'Grep',
  glob: 'Glob',
  list_directory: 'Glob',
};

export function toClaudePayload(event, input, projectDir) {
  const payload = {
    hook_event_name: event,
    session_id: input.session_id ?? '',
    cwd: input.cwd ?? projectDir,
    stop_hook_active: Boolean(input.stop_hook_active),
  };
  if (event === 'SessionStart') payload.source = input.source ?? 'startup';
  if (input.tool_name) {
    payload.tool_name = TOOL_NAMES[input.tool_name] ?? input.tool_name;
    const toolInput = { ...(input.tool_input ?? {}) };
    toolInput.file_path ??= toolInput.absolute_path ?? toolInput.path;
    payload.tool_input = toolInput;
  }
  return payload;
}

function parseClaudeOutput(stdout) {
  const text = stdout.trim();
  if (!text) return {};
  try {
    return JSON.parse(text);
  } catch {
    // Plain stdout is session context in Claude's SessionStart contract.
    return { hookSpecificOutput: { additionalContext: text } };
  }
}

export function toGeminiOutput(event, out) {
  const specific = out.hookSpecificOutput ?? {};
  const context = specific.additionalContext;
  if (event === 'SessionStart') {
    return context ? { hookSpecificOutput: { hookEventName: 'SessionStart', additionalContext: context } } : {};
  }
  const denied = specific.permissionDecision === 'deny' || out.decision === 'block';
  const result = denied ? { decision: 'deny', reason: specific.permissionDecisionReason ?? out.reason ?? '' } : {};
  if (!denied && context) result.hookSpecificOutput = { additionalContext: context };
  if (out.systemMessage) result.systemMessage = out.systemMessage;
  return result;
}

function main(argv) {
  const sep = argv.indexOf('--');
  const event = argv[0];
  const command = argv.slice(sep + 1);
  if (sep !== 1 || !command.length) {
    process.stderr.write('usage: gemini-adapter.mjs <event> -- <command...>\n');
    return {};
  }
  let input = {};
  try {
    input = JSON.parse(fs.readFileSync(0, 'utf8') || '{}');
  } catch {
    // no payload
  }
  const projectDir = process.env.GEMINI_PROJECT_DIR || process.cwd();
  const child = spawnSync(command[0], command.slice(1), {
    cwd: projectDir,
    input: JSON.stringify(toClaudePayload(event, input, projectDir)),
    encoding: 'utf8',
    stdio: ['pipe', 'pipe', 'inherit'],
  });
  if (child.error) {
    process.stderr.write(`[hooks] ${command[0]}: ${child.error.message}\n`);
    return {};
  }
  return toGeminiOutput(event, parseClaudeOutput(child.stdout ?? ''));
}

if (path.resolve(process.argv[1] ?? '') === fileURLToPath(import.meta.url)) {
  let output = {};
  try {
    output = main(process.argv.slice(2));
  } catch (error) {
    process.stderr.write(`[hooks] ${error.message}\n`);
  }
  process.stdout.write(JSON.stringify(output));
  process.exit(0);
}
