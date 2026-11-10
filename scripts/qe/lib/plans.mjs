import fs from 'node:fs';
import path from 'node:path';
import { fsReader } from './git.mjs';
import { caseIdsIn, storySha } from './stories.mjs';
import { parseFrontMatter, sha, stringifyFrontMatter } from './util.mjs';

export const INDEX = '_index.md';
const SECTIONS = ['Preconditions', 'Steps', 'Expected result'];
const LAYERS = ['functional', 'e2e'];

export function readPlan(config, story, reader = fsReader(config.root)) {
  const indexText = reader.read(`${story.planDir}/${INDEX}`);
  const index = indexText == null ? null : { text: indexText, ...parseFrontMatter(indexText) };
  const cases = reader
    .list(story.planDir)
    .filter((name) => name.startsWith('TC-') && name.endsWith('.md'))
    .map((name) => {
      const text = reader.read(`${story.planDir}/${name}`) ?? '';
      return { file: `${story.planDir}/${name}`, name, text, ...parseFrontMatter(text) };
    });
  return { index, cases };
}

export function planContentSha(plan) {
  const parts = plan.cases.map((c) => `${c.name}\n${c.text.replace(/\r\n/g, '\n')}`);
  parts.push(plan.index ? plan.index.body.replace(/\r\n/g, '\n') : '');
  return sha(parts.join('\n\u0000\n'));
}

function sectionBody(body, title) {
  const match = new RegExp(`^##\\s+${title}\\s*$([\\s\\S]*?)(?=^##\\s|(?![\\s\\S]))`, 'mi').exec(body);
  return match ? match[1].trim() : null;
}

// Deterministic structural checks that run before any verifier tokens are spent.
export function precheckPlan(config, story, reader = fsReader(config.root)) {
  const plan = readPlan(config, story, reader);
  const errors = [];
  if (!plan.index) errors.push(`${story.planDir}/${INDEX} is missing.`);
  if (plan.cases.length === 0) errors.push(`No case files (TC-${story.key}-NN-<slug>.md) in ${story.planDir}.`);

  const seen = new Map();
  const idPattern = new RegExp(`^TC-${story.key}-\\d{2,}$`);
  for (const c of plan.cases) {
    const where = c.file;
    const id = c.data.id;
    if (!c.hasFrontMatter) errors.push(`${where}: missing front matter.`);
    if (!idPattern.test(String(id ?? ''))) errors.push(`${where}: id "${id ?? ''}" must look like TC-${story.key}-01.`);
    else if (!c.name.startsWith(`${id}-`) && c.name !== `${id}.md`) errors.push(`${where}: file name must start with its id ${id}.`);
    if (id && seen.has(id)) errors.push(`${where}: duplicate id ${id} (also in ${seen.get(id)}).`);
    if (id) seen.set(id, where);
    if (!String(c.data.title ?? '').trim()) errors.push(`${where}: title is empty.`);
    if (!['happy', 'sad'].includes(c.data.type)) errors.push(`${where}: type must be happy or sad.`);
    if (c.data.type === 'sad' && !config.categories.sadPathSubtypes.includes(c.data['sad-path'])) {
      errors.push(`${where}: sad cases need sad-path: one of ${config.categories.sadPathSubtypes.join(', ')}.`);
    }
    const covers = Array.isArray(c.data.covers) ? c.data.covers : [c.data.covers].filter(Boolean);
    if (covers.length === 0) errors.push(`${where}: covers must list the story requirement(s) this case checks.`);
    if (!LAYERS.includes(c.data.layer)) errors.push(`${where}: layer must be one of ${LAYERS.join(', ')}.`);
    if (!/^#\s+\S/m.test(c.body)) errors.push(`${where}: body must start with a "# <id>: <title>" heading.`);
    for (const section of SECTIONS) {
      const content = sectionBody(c.body, section);
      if (content == null) errors.push(`${where}: missing "## ${section}" section.`);
      else if (!content) errors.push(`${where}: "## ${section}" is empty.`);
    }
  }

  const types = new Set(plan.cases.map((c) => c.data.type));
  if (plan.cases.length && !types.has('happy')) errors.push('The plan has no happy-path case.');
  if (plan.cases.length && !types.has('sad')) errors.push('The plan has no sad-path case.');

  if (plan.index) {
    const listed = caseIdsIn(plan.index.body, story.key);
    for (const id of seen.keys()) if (!listed.has(id)) errors.push(`${INDEX} does not list ${id}.`);
    for (const id of listed) if (!seen.has(id)) errors.push(`${INDEX} lists ${id} but no case file exists.`);
  }
  return { plan, errors };
}

export function planStatus(config, story, reader = fsReader(config.root)) {
  const storyText = reader.read(story.file);
  const plan = readPlan(config, story, reader);
  if (!plan.index || plan.cases.length === 0) return { state: 'missing', plan, reasons: ['no plan'] };
  const fm = plan.index.data;
  const reasons = [];
  if (storyText != null && fm['story-sha'] !== storySha(storyText)) reasons.push('story changed since the plan was written');
  if (reasons.length) return { state: 'stale', plan, reasons };
  if (Number(fm['verified-score']) < config.loops.requiredScore) reasons.push('plan has not passed the verifier');
  else if (fm['plan-sha'] !== planContentSha(plan)) reasons.push('plan edited after verification');
  if (reasons.length) return { state: 'unverified', plan, reasons };
  return { state: 'verified', plan, reasons };
}

// Pipeline-owned front matter. Agents write the index body; the runner owns these fields.
export function stampIndex(config, story, fields) {
  const file = path.join(config.root, story.planDir, INDEX);
  const text = fs.existsSync(file) ? fs.readFileSync(file, 'utf8') : `# Test plan: ${story.slug}\n`;
  const { data, body } = parseFrontMatter(text);
  const storyText = fs.readFileSync(path.join(config.root, story.file), 'utf8');
  const next = {
    ...data,
    story: story.key,
    'story-file': story.file,
    'story-sha': storySha(storyText),
    ...fields,
  };
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, stringifyFrontMatter(next, body));
}

// Test files and scenario files that implement a story's cases.
export function automationFiles(config, story, reader = fsReader(config.root)) {
  const tests = new Map();
  for (const root of config.testRoots) {
    for (const file of reader.walk(root)) {
      if (file.startsWith(`${config.plansDir}/`)) continue;
      if (!config.testExtensions.some((ext) => file.endsWith(ext))) continue;
      const text = reader.read(file) ?? '';
      if (!text.includes(`TC-${story.key}-`)) continue;
      const ids = caseIdsIn(text, story.key);
      if (ids.size) tests.set(file, ids);
    }
  }
  const scenarioDir = `${config.scenariosDir}/${story.scenario}`;
  const scenario = reader.walk(scenarioDir);
  return { tests, scenarioDir, scenario };
}

export function automationSha(config, story, reader = fsReader(config.root)) {
  const { tests, scenario } = automationFiles(config, story, reader);
  const files = [...tests.keys(), ...scenario].sort();
  if (files.length === 0) return '';
  return sha(files.map((f) => `${f}\n${(reader.read(f) ?? '').replace(/\r\n/g, '\n')}`).join('\n\u0000\n'));
}

export function automationStatus(config, story, reader = fsReader(config.root)) {
  const { tests } = automationFiles(config, story, reader);
  if (tests.size === 0) return { state: 'none', reasons: [] };
  const fm = readPlan(config, story, reader).index?.data ?? {};
  if (Number(fm['automation-score']) < config.loops.requiredScore || !fm['automation-sha']) {
    return { state: 'unverified', reasons: ['automated tests have not passed the verifier'] };
  }
  if (fm['automation-sha'] !== automationSha(config, story, reader)) {
    return { state: 'unverified', reasons: ['tests or scenario edited after verification'] };
  }
  if (fm['automation-plan-sha'] !== fm['plan-sha']) {
    return { state: 'unverified', reasons: ['plan changed since the tests were verified'] };
  }
  return { state: 'verified', reasons: [] };
}
