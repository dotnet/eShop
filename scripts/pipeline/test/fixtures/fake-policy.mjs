#!/usr/bin/env node
// Scripted stand-in for architecture-policy/agents/arch_review.py, used by the e2e tests.
// PIPELINE_FAKE_POLICY is a JSON array of review responses; each call consumes the next (the last one repeats).
import fs from 'node:fs';

const file = process.env.PIPELINE_FAKE_POLICY;
const request = JSON.parse(fs.readFileSync(0, 'utf8'));
if (process.argv.includes('get_lessons')) {
  fs.appendFileSync(`${file}.lessons.jsonl`, `${JSON.stringify(request)}\n`);
  process.stdout.write(JSON.stringify({ agent: request.agent, prompt: process.env.PIPELINE_FAKE_LESSONS ?? '' }));
  process.exit(0);
}
const log = `${file}.requests.jsonl`;
const n = fs.existsSync(log) ? fs.readFileSync(log, 'utf8').trim().split('\n').filter(Boolean).length : 0;
fs.appendFileSync(log, `${JSON.stringify(request)}\n`);
const responses = JSON.parse(fs.readFileSync(file, 'utf8'));
const response = responses[Math.min(n, responses.length - 1)];
process.stdout.write(JSON.stringify(response));
process.exit(response.decision === 'BLOCK' ? 1 : 0);
