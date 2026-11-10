import { runAgent } from '../../pipeline/lib/agents.mjs';
import { metricsFile } from '../../pipeline/lib/state.mjs';
import { appendJsonl, extractResult, log, nowIso } from '../../pipeline/lib/util.mjs';

export const VERIFIER = { plan: 'qe-plan-verifier', automation: 'qe-automation-verifier' };
export const PRODUCER = { plan: 'qe-plan', automation: 'qe-automation' };

export { metricsFile };

export function attributedAgent(stage, rootStage) {
  if (stage === 'plan' || rootStage === 'plan') return PRODUCER.plan;
  return PRODUCER.automation;
}

export function normalizeVerdict(config, stage, raw) {
  const categories = config.categories[stage];
  const required = config.loops.requiredScore;
  let score = Math.max(0, Math.min(10, Math.round(Number(raw?.score))));
  if (!Number.isFinite(score)) score = 0;
  const gaps = (Array.isArray(raw?.gaps) ? raw.gaps : []).map((gap) => {
    const known = categories.includes(gap?.category);
    return {
      case: gap?.case ?? null,
      category: known ? gap.category : 'other',
      subtype: known ? gap?.subtype ?? null : gap?.category ?? gap?.subtype ?? null,
      detail: String(gap?.detail ?? '').trim() || '(no detail given)',
      location: gap?.location ?? null,
      root_stage: stage === 'automation' && gap?.root_stage === 'plan' ? 'plan' : stage,
    };
  });
  if (score < required && gaps.length === 0) {
    gaps.push({ case: null, category: 'other', subtype: 'unexplained-score', detail: `Verifier scored ${score}/10 without listing gaps.`, location: null, root_stage: stage });
  }
  return { score, gaps, summary: String(raw?.summary ?? ''), passed: score >= required && gaps.length === 0 };
}

// Runs one verifier round in a fresh process and records the outcome for observability.
export function runVerifier(config, { stage, story, round, task, producerVersion, model, metrics = true }) {
  const name = VERIFIER[stage];
  let attempt = 0;
  let result;
  let verdict = null;
  while (attempt <= config.agent.resultParseRetries) {
    result = runAgent(config, {
      name,
      task,
      mode: 'read',
      model: model ?? config.agent.models.verifier,
      timeoutSec: config.timeoutsSec.verifier,
    });
    const raw = extractResult(result.output);
    if (raw) {
      verdict = normalizeVerdict(config, stage, raw);
      break;
    }
    attempt++;
    log(`${name} returned no parsable AGENT_RESULT (attempt ${attempt}).`);
  }
  if (!verdict) return { error: 'verifier produced no parsable result', output: result?.output ?? '' };

  if (metrics) {
    const ts = nowIso();
    const common = { ts, stage, story: story.key, round, agent_prompt_ver: producerVersion, verifier_ver: result.version, cli: config.agent.cmd ? 'custom' : config.agent.cli };
    appendJsonl(metricsFile(config, 'verifier-runs.jsonl'), { ...common, score: verdict.score, passed: verdict.passed, gaps: verdict.gaps.length });
    appendJsonl(
      metricsFile(config, 'verifier-denials.jsonl'),
      verdict.gaps.map((gap) => ({ ...common, score: verdict.score, ...gap, attributed_agent: attributedAgent(stage, gap.root_stage) })),
    );
  }
  log(`${name} round ${round}: ${verdict.score}/10, ${verdict.gaps.length} gap(s)`);
  return { ...verdict, verifierVersion: result.version };
}

export function formatGaps(gaps) {
  return gaps
    .map((g, i) => `${i + 1}. [${g.category}${g.subtype ? `/${g.subtype}` : ''}] ${g.case ?? '(plan-wide)'}${g.location ? ` @ ${g.location}` : ''}: ${g.detail}`)
    .join('\n');
}
