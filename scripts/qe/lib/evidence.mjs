import fs from 'node:fs';
import path from 'node:path';

// Visual evidence from recorded e2e runs. e2e/video-reporter.ts writes a manifest listing every test with
// its status, errors, video and labelled keyframes (screenshots taken at each step, demo checkpoint and at
// the end or failure of the test). Agents can't watch the video but can open the keyframe images.

export const manifestPath = (config) => path.join(config.root, config.evidence?.manifest ?? 'artifacts/e2e-videos/manifest.json');

// Tests from the manifest whose title contains `casePrefix`, only if the run started at or after `since`
// (so a manifest left over from an older run is never shown as evidence for this one).
export function readEvidence(config, { casePrefix, since = null } = {}) {
  const file = manifestPath(config);
  if (!fs.existsSync(file)) return [];
  let manifest;
  try {
    manifest = JSON.parse(fs.readFileSync(file, 'utf8'));
  } catch {
    return [];
  }
  if (since && !(Date.parse(manifest.startedAt) >= Date.parse(since))) return [];
  const dir = path.dirname(file);
  return (manifest.tests ?? [])
    .filter((t) => !casePrefix || t.title?.includes(casePrefix))
    .map((t) => ({
      ...t,
      videos: (t.videos ?? []).map((v) => path.join(dir, v)),
      keyframes: (t.keyframes ?? []).map((k) => ({ label: k.label, path: path.join(dir, k.path) })),
    }));
}

// Markdown for an agent prompt. Paths are absolute so image-reading tools can open them directly.
export function formatEvidence(tests, { maxFramesPerTest = 20 } = {}) {
  const lines = [];
  for (const t of tests) {
    lines.push('', `#### ${t.title} (${t.file}:${t.line}, ${t.ok ? 'passed' : `FAILED: ${t.status}`})`);
    if (!t.ok && t.errors?.length) lines.push('', 'Error:', '', '````text', t.errors.join('\n\n').trimEnd(), '````');
    if (!t.keyframes.length) {
      lines.push('', t.videos.length ? '- No keyframes captured.' : '- No browser in this test (API only): no video or keyframes.');
    } else {
      const frames = t.keyframes.length > maxFramesPerTest
        ? [...t.keyframes.slice(0, maxFramesPerTest - 3), ...t.keyframes.slice(-3)]
        : t.keyframes;
      lines.push('', 'Keyframes, in order:');
      for (const [i, k] of frames.entries()) lines.push(`${i + 1}. ${k.label}: \`${k.path}\``);
      if (frames.length < t.keyframes.length) lines.push(`(${t.keyframes.length - frames.length} middle frames omitted)`);
    }
    for (const v of t.videos) lines.push(`- Video (for humans): \`${v}\``);
  }
  return lines.join('\n').trimStart();
}
