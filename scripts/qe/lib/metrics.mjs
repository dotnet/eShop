import { readJsonl } from '../../pipeline/lib/util.mjs';
import { PRODUCER, VERIFIER, metricsFile } from './verify.mjs';

export function loadMetrics(config) {
  return {
    runs: readJsonl(metricsFile(config, 'verifier-runs.jsonl')),
    denials: readJsonl(metricsFile(config, 'verifier-denials.jsonl')),
    prechecks: readJsonl(metricsFile(config, 'precheck-failures.jsonl')),
    calibration: readJsonl(metricsFile(config, 'calibration.jsonl')),
    reviews: readJsonl(metricsFile(config, 'arch-reviews.jsonl')),
  };
}

const groupBy = (items, key) =>
  items.reduce((map, item) => {
    const k = typeof key === 'function' ? key(item) : item[key];
    (map[k] ??= []).push(item);
    return map;
  }, {});

function stageSummary(runs) {
  const passes = runs.filter((r) => r.passed);
  const firstRounds = runs.filter((r) => r.round === 1);
  return {
    verifierRounds: runs.length,
    denials: runs.length - passes.length,
    denialRate: runs.length ? round2((runs.length - passes.length) / runs.length) : 0,
    passes: passes.length,
    firstRoundPassRate: firstRounds.length ? round2(firstRounds.filter((r) => r.passed).length / firstRounds.length) : 0,
    avgRoundsToPass: passes.length ? round2(passes.reduce((sum, r) => sum + r.round, 0) / passes.length) : null,
    stories: new Set(runs.map((r) => r.story)).size,
  };
}

const round2 = (n) => Math.round(n * 100) / 100;

export function categoryStats(entries) {
  const groups = groupBy(entries, (d) => `${d.attributed_agent}\u0000${d.category}\u0000${d.subtype ?? ''}`);
  return Object.values(groups)
    .map((list) => ({
      agent: list[0].attributed_agent,
      category: list[0].category,
      subtype: list[0].subtype ?? null,
      count: list.length,
      stories: new Set(list.map((d) => d.story)).size,
      lastSeen: list.map((d) => d.ts).sort().at(-1),
    }))
    .sort((a, b) => b.stories - a.stories || b.count - a.count);
}

export function computeMetrics(config, { since, data = loadMetrics(config) } = {}) {
  const keep = (r) => !since || r.ts >= since;
  const runs = data.runs.filter(keep);
  const denials = data.denials.filter(keep);
  const stages = Object.fromEntries(['plan', 'automation'].map((stage) => [stage, stageSummary(runs.filter((r) => r.stage === stage))]));

  const byVersion = {};
  for (const [stage, producer] of Object.entries(PRODUCER)) {
    const versions = groupBy(runs.filter((r) => r.stage === stage), 'agent_prompt_ver');
    byVersion[producer] = Object.fromEntries(
      Object.entries(versions).map(([version, list]) => {
        const ds = denials.filter((d) => d.stage === stage && d.agent_prompt_ver === version);
        const perCategory = Object.fromEntries(Object.entries(groupBy(ds, 'category')).map(([c, l]) => [c, l.length]));
        return [version, { rounds: list.length, firstSeen: list.map((r) => r.ts).sort()[0], denialRate: stageSummary(list).denialRate, perCategory }];
      }),
    );
  }

  const crossStage = denials.filter((d) => d.stage === 'automation' && d.root_stage === 'plan').length;
  return {
    stages,
    categories: categoryStats(denials),
    byVersion,
    crossStage: { automationDenialsCausedByPlan: crossStage, automationDenials: denials.filter((d) => d.stage === 'automation').length },
    prechecks: Object.fromEntries(Object.entries(groupBy(data.prechecks.filter(keep), 'stage')).map(([s, l]) => [s, l.length])),
    calibration: categoryStats(data.calibration.filter((c) => keep(c) && c.category !== 'agreed')),
  };
}

export function formatMetrics(m) {
  const out = [];
  out.push('Verifier loops');
  out.push('stage       rounds  denials  denial-rate  1st-round-pass  avg-rounds-to-10  stories');
  for (const [stage, s] of Object.entries(m.stages)) {
    out.push(`${stage.padEnd(11)} ${String(s.verifierRounds).padStart(6)}  ${String(s.denials).padStart(7)}  ${String(s.denialRate).padStart(11)}  ${String(s.firstRoundPassRate).padStart(14)}  ${String(s.avgRoundsToPass ?? '-').padStart(16)}  ${String(s.stories).padStart(7)}`);
  }
  out.push('', 'Denials by category (attributed agent)');
  out.push('agent                 category                       subtype         count  stories  last seen');
  for (const c of m.categories) {
    out.push(`${c.agent.padEnd(21)} ${c.category.padEnd(30)} ${(c.subtype ?? '-').padEnd(15)} ${String(c.count).padStart(5)}  ${String(c.stories).padStart(7)}  ${c.lastSeen?.slice(0, 10) ?? ''}`);
  }
  out.push('', 'Trend by agent prompt version');
  for (const [agent, versions] of Object.entries(m.byVersion)) {
    for (const [version, v] of Object.entries(versions).sort((a, b) => (a[1].firstSeen < b[1].firstSeen ? -1 : 1))) {
      const cats = Object.entries(v.perCategory).map(([c, n]) => `${c}=${n}`).join(' ') || 'no denials';
      out.push(`${agent.padEnd(21)} ${version}  rounds=${v.rounds} denial-rate=${v.denialRate}  ${cats}`);
    }
  }
  out.push('', `Automation denials traced back to the plan: ${m.crossStage.automationDenialsCausedByPlan}/${m.crossStage.automationDenials}`);
  out.push(`Deterministic precheck failures: ${Object.entries(m.prechecks).map(([s, n]) => `${s}=${n}`).join(' ') || 'none'}`);
  if (m.calibration.length) {
    out.push('', 'Calibration misses (10/10 results an auditor rejected)');
    for (const c of m.calibration) out.push(`${c.agent.padEnd(25)} ${c.category.padEnd(30)} ${String(c.count).padStart(5)}  stories=${c.stories}`);
  }
  return out.join('\n');
}

export const AGENT_STAGE = {
  [PRODUCER.plan]: 'plan',
  [PRODUCER.automation]: 'automation',
  [VERIFIER.plan]: 'plan',
  [VERIFIER.automation]: 'automation',
};
