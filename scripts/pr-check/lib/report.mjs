// Renders the PR check result as Markdown: for people reading report.md and for the PR comment.
import { LENSES } from './loop.mjs';

const cell = (text) => String(text ?? '').replace(/\|/g, '\\|').replace(/\r?\n/g, ' ');

export function renderReport(ctx, result) {
  const { verdict, reason, history } = result;
  const last = history[history.length - 1];
  const lenses = Object.keys(last?.lensScores ?? {});
  const lines = [
    `# PR check: ${verdict} ${last ? `${last.score}/10` : ''}`.trim(),
    '',
    `- Story: ${ctx.story.key} - ${ctx.storyTitle}`,
    ctx.pr ? `- Pull request: [#${ctx.pr.number} ${cell(ctx.pr.title)}](${ctx.pr.url})` : `- Branch: ${ctx.branch}`,
    `- Base: ${ctx.baseRef} (merge base ${ctx.baseRev.slice(0, 10)})`,
    `- Frontend story: ${ctx.isFrontend ? 'yes' : 'no'}; rounds: ${history.length}`,
  ];
  if (reason) lines.push(`- Escalated: ${reason}`);

  if (history.length) {
    lines.push('', '## Rounds', '', `| Round | Score | ${lenses.map((l) => LENSES[l].title).join(' | ')} | Raised | Upheld | Fix commit |`, `|---|---|${lenses.map(() => '---|').join('')}---|---|---|`);
    for (const h of history) {
      lines.push(`| ${h.round} | ${h.score}/10 | ${lenses.map((l) => h.lensScores[l] ?? '-').join(' | ')} | ${h.raised} | ${h.upheld.length} | ${h.fix ? (h.fix.commit ?? 'none') : '-'} |`);
    }
  }

  if (last?.upheld.length) {
    lines.push('', '## Open findings', '');
    for (const f of last.upheld) {
      lines.push(`### ${f.id} [${f.severity}] ${cell(f.title)}`, '', `Lens: ${LENSES[f.lens].title}`);
      if (f.requirement) lines.push(`Requirement: ${f.requirement}`);
      if (f.detail) lines.push('', f.detail);
      if (f.evidence) lines.push('', `Evidence: ${f.evidence}`);
      if (f.reason) lines.push('', `Judge: ${f.reason}`);
      if (f.fix) lines.push('', `Fix: ${f.fix}`);
      lines.push('');
    }
  }

  const openIds = new Set(last?.upheld.map((f) => f.id) ?? []);
  const resolved = new Map();
  for (const h of history.slice(0, -1)) for (const f of h.upheld) if (!openIds.has(f.id)) resolved.set(f.id, `${f.id} [${f.severity}] ${cell(f.title)} (round ${h.round})`);
  if (resolved.size) lines.push('', '## Resolved during the loop', '', ...[...resolved.values()].map((t) => `- ${t}`));

  const notFixed = history.flatMap((h) => h.fix?.notFixed ?? []);
  if (notFixed.length) lines.push('', '## The fixer could not fix', '', ...notFixed.map((n) => `- ${n.id}: ${cell(n.reason)}`));

  lines.push('', '## Video evidence', '');
  if (!ctx.video.videos.length && !ctx.video.unreadable.length) lines.push('No videos were found, so the video lens did not run.');
  for (const v of ctx.video.videos) lines.push(`- ${v.source}: ${v.duration ? `${v.duration.toFixed(1)}s, ` : ''}${v.frames.length} frames (${v.extractor})`);
  for (const u of ctx.video.unreadable) lines.push(`- Could not read ${u.source}: ${u.reason}`);

  const skipped = [];
  if (!ctx.isFrontend) skipped.push('UI design (not a frontend story)');
  if (!ctx.video.videos.length) skipped.push('video evidence (no readable video)');
  for (const h of history) for (const l of h.failedLenses) skipped.push(`${LENSES[l].title} in round ${h.round} (the critic failed; scored 0)`);
  if (skipped.length) lines.push('', '## Not checked', '', ...skipped.map((s) => `- ${s}`));
  if (last?.summary) lines.push('', '## Judge summary', '', last.summary);
  return `${lines.join('\n')}\n`;
}
