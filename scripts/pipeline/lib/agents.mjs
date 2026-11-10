import { spawn, spawnSync } from 'node:child_process';
import { randomUUID } from 'node:crypto';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { log, parseFrontMatter, run, sha } from './util.mjs';

export function agentDefinition(config, name) {
  const file = path.join(config.root, config.agentsDir, `${name}.agent.md`);
  const text = fs.readFileSync(file, 'utf8');
  const lessonsFile = path.join(config.root, config.lessonsDir, `${name}.md`);
  const lessons = fs.existsSync(lessonsFile) ? fs.readFileSync(lessonsFile, 'utf8') : '';
  return { name, body: parseFrontMatter(text).body.trim(), lessons: lessons.trim(), version: sha(`${text}\n${lessons}`, 8) };
}

export function buildPrompt(definition, task) {
  const parts = [definition.body];
  if (definition.lessons) {
    parts.push(`## Lessons from earlier verifier denials (follow these)\n\n${definition.lessons}`);
  }
  parts.push(`## Task\n\n${task}`);
  return parts.join('\n\n');
}

const childEnv = (extra = {}) => ({ ...process.env, PIPELINE_CHILD: '1', ...extra });

function cliInvocation(config, { prompt, mode, model }) {
  const cli = config.agent.cli;
  if (config.agent.cmd) {
    return { cmd: 'sh', args: ['-c', config.agent.cmd], input: prompt };
  }
  if (cli === 'claude') {
    const args = ['-p', '--output-format', 'text'];
    if (model) args.push('--model', model);
    if (mode === 'write') {
      args.push(
        '--permission-mode', 'acceptEdits',
        '--allowedTools', 'Bash,Edit,Write,MultiEdit,Read,Glob,Grep,TodoWrite',
        '--disallowedTools', 'Bash(git push:*),Bash(git commit:*),Bash(git reset:*),Bash(git checkout:*),Bash(git stash:*)',
      );
    } else {
      args.push(
        '--allowedTools', 'Read,Glob,Grep,Bash(graphify:*)',
        '--disallowedTools', 'Edit,Write,MultiEdit,NotebookEdit,WebFetch,WebSearch',
      );
    }
    return { cmd: 'claude', args, input: prompt };
  }
  if (cli === 'copilot') {
    const args = ['-p', prompt, '-s', '--no-color'];
    if (model) args.push('--model', model);
    if (mode === 'write') {
      args.push('--allow-all-tools', '--deny-tool', 'shell(git push)', '--deny-tool', 'shell(git commit)', '--deny-tool', 'shell(git reset)');
    } else {
      args.push('--deny-tool', 'write', '--deny-tool', 'shell');
    }
    return { cmd: 'copilot', args };
  }
  if (cli === 'codex') {
    const outFile = path.join(os.tmpdir(), `pipeline-codex-${process.pid}-${randomUUID()}.txt`);
    const args = ['exec', '--skip-git-repo-check', '--sandbox', mode === 'write' ? 'workspace-write' : 'read-only', '--output-last-message', outFile];
    if (model) args.push('-m', model);
    args.push('-');
    return { cmd: 'codex', args, input: prompt, outFile };
  }
  throw new Error(`Unknown agent CLI "${cli}". Use claude, copilot, codex or set PIPELINE_AGENT_CMD.`);
}

// Every call is a brand-new headless process, so each agent (and each verifier round) starts with a
// clean context containing only the prompt built here.
export function runAgent(config, { name, task, mode = 'read', model = null, cwd = config.root, timeoutSec = 900, extraEnv = {} }) {
  const definition = agentDefinition(config, name);
  const prompt = buildPrompt(definition, task);
  const invocation = cliInvocation(config, { prompt, mode, model: model ?? config.agent.models.default });
  log(`agent ${name} (${mode}, ${config.agent.cmd ? 'custom' : config.agent.cli}, prompt ${definition.version}) started`);
  const started = Date.now();
  const result = run(invocation.cmd, invocation.args, {
    cwd,
    input: invocation.input,
    timeoutSec,
    env: childEnv({ PIPELINE_AGENT_NAME: name, PIPELINE_AGENT_MODE: mode, ...extraEnv }),
  });
  let output = result.stdout;
  if (invocation.outFile) {
    try {
      output = fs.readFileSync(invocation.outFile, 'utf8');
      fs.unlinkSync(invocation.outFile);
    } catch {
      // fall back to stdout
    }
  }
  const seconds = Math.round((Date.now() - started) / 1000);
  log(`agent ${name} finished in ${seconds}s (exit ${result.status}${result.timedOut ? ', timed out' : ''})`);
  if (result.status !== 0 && result.stderr.trim()) log(result.stderr.trim().slice(-2000));
  return { ok: result.status === 0, status: result.status, timedOut: result.timedOut, output, version: definition.version, seconds };
}

// Non-blocking variant of runAgent, so a stage can run several fresh agents in parallel. Copilot takes
// its prompt as a command-line argument (capped at 32K chars on Windows, and cmd.exe mangles newlines),
// so its prompt is written to promptFile and the agent is told to read it.
export function runAgentAsync(config, { name, task, mode = 'read', model = null, cwd = config.root, timeoutSec = 900, extraEnv = {}, promptFile = null }) {
  const definition = agentDefinition(config, name);
  let prompt = buildPrompt(definition, task);
  if (!config.agent.cmd && config.agent.cli === 'copilot') {
    const file = promptFile ?? path.join(config.root, config.stateDir, 'prompts', `${name}-${process.pid}-${Date.now()}.md`);
    fs.mkdirSync(path.dirname(file), { recursive: true });
    fs.writeFileSync(file, prompt);
    prompt = `Read the file ${path.relative(cwd, file).replace(/\\/g, '/')} and follow the instructions in it exactly; it is your complete task.`;
  }
  const invocation = cliInvocation(config, { prompt, mode, model: model ?? config.agent.models.default });
  // On Windows, npm-installed CLIs are .cmd shims that only start through a shell.
  const useShell = process.platform === 'win32' && !config.agent.cmd;
  const quote = (a) => (/[\s"&|<>^()]/.test(a) ? `"${a.replace(/"/g, '\\"')}"` : a);
  log(`agent ${name} (${mode}, ${config.agent.cmd ? 'custom' : config.agent.cli}, prompt ${definition.version}) started`);
  const started = Date.now();
  return new Promise((resolve) => {
    const child = useShell
      ? spawn([invocation.cmd, ...invocation.args].map(quote).join(' '), { cwd, shell: true, env: childEnv({ PIPELINE_AGENT_NAME: name, PIPELINE_AGENT_MODE: mode, ...extraEnv }) })
      : spawn(invocation.cmd, invocation.args, { cwd, env: childEnv({ PIPELINE_AGENT_NAME: name, PIPELINE_AGENT_MODE: mode, ...extraEnv }) });
    let stdout = '';
    let stderr = '';
    let timedOut = false;
    const timer = setTimeout(() => {
      timedOut = true;
      // Through a shell the agent is a grandchild that holds the output pipes open, so kill the whole
      // tree and stop waiting on the pipes.
      if (useShell) spawnSync('taskkill', ['/pid', String(child.pid), '/T', '/F'], { stdio: 'ignore' });
      child.kill('SIGKILL');
      child.stdout.destroy();
      child.stderr.destroy();
    }, timeoutSec * 1000);
    child.stdout.setEncoding('utf8').on('data', (d) => (stdout += d));
    child.stderr.setEncoding('utf8').on('data', (d) => (stderr += d));
    child.on('error', (error) => (stderr += `\n${error.message}`));
    child.stdin.on('error', () => {});
    child.stdin.end(invocation.input ?? '');
    child.on('close', (code) => {
      clearTimeout(timer);
      let output = stdout;
      if (invocation.outFile) {
        try {
          output = fs.readFileSync(invocation.outFile, 'utf8');
          fs.unlinkSync(invocation.outFile);
        } catch {
          // fall back to stdout
        }
      }
      const status = code ?? 1;
      const seconds = Math.round((Date.now() - started) / 1000);
      log(`agent ${name} finished in ${seconds}s (exit ${status}${timedOut ? ', timed out' : ''})`);
      if (status !== 0 && stderr.trim()) log(stderr.trim().slice(-2000));
      resolve({ ok: status === 0, status, timedOut, output, version: definition.version, seconds });
    });
  });
}