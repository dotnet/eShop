import fs from 'fs';
import path from 'path';
import type { FullConfig, Reporter, Suite, TestCase, TestResult } from '@playwright/test/reporter';

/**
 * Copies every test's video (`use.video` in playwright.config.ts) to a stable, readable path:
 * artifacts/e2e-videos/<project>/<spec>--<test>[--retryN][.failed].webm, and prints the list at the end.
 *
 * Agents can't watch video, so the keyframes each test captures (e2e/fixtures.ts) are copied to
 * <spec>--<test>[--retryN][.failed].keyframes/NN-<label>.png, and artifacts/e2e-videos/manifest.json lists
 * every test with its status, error, videos and labelled keyframes. scripts/qe reads the manifest to show
 * failing runs to the automation agent and the recorded flow to the automation verifier.
 *
 * The previous run is moved to artifacts/e2e-videos-previous/ so a fix can be compared with the failure.
 */
const OUT_DIR = path.join(__dirname, '..', 'artifacts', 'e2e-videos');
const PREVIOUS_DIR = `${OUT_DIR}-previous`;
const MANIFEST = path.join(OUT_DIR, 'manifest.json');
const KEEP_VIDEOS = /^(1|true|yes)$/i.test(process.env.E2E_KEEP_VIDEOS ?? '');

function slug(value: string) {
  return value.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '').slice(0, 80) || 'default';
}

const rel = (file: string) => path.relative(OUT_DIR, file).split(path.sep).join('/');
const stripAnsi = (text: string) => text.replace(/\u001b\[[0-9;]*m/g, '');

interface ManifestTest {
  title: string;
  titlePath: string[];
  file: string;
  line: number;
  project: string;
  status: TestResult['status'];
  expectedStatus: TestCase['expectedStatus'];
  ok: boolean;
  retry: number;
  durationMs: number;
  errors: string[];
  videos: string[];
  keyframes: { label: string; path: string }[];
}

export default class VideoReporter implements Reporter {
  private saved: string[] = [];
  private tests: ManifestTest[] = [];
  private rootDir = process.cwd();
  private startedAt = new Date().toISOString();

  onBegin(config: FullConfig, _suite: Suite) {
    this.rootDir = config.rootDir;
    this.startedAt = new Date().toISOString();
    fs.rmSync(PREVIOUS_DIR, { recursive: true, force: true });
    if (fs.existsSync(OUT_DIR)) {
      try {
        fs.renameSync(OUT_DIR, PREVIOUS_DIR);
      } catch {
        fs.rmSync(OUT_DIR, { recursive: true, force: true });
      }
    }
  }

  onTestEnd(test: TestCase, result: TestResult) {
    const failed = result.status !== test.expectedStatus;
    // Passing tests keep their video and keyframes only when asked to (E2E_KEEP_VIDEOS=1).
    const keep = failed || KEEP_VIDEOS;
    const videos = keep ? result.attachments.filter(a => a.name === 'video' && a.path && fs.existsSync(a.path)) : [];
    const keyframes = keep ? result.attachments.filter(a => a.name.startsWith('keyframe: ') && a.path && fs.existsSync(a.path)) : [];
    const entry: ManifestTest = {
      title: test.title,
      titlePath: test.titlePath().filter(Boolean),
      file: path.relative(this.rootDir, test.location.file).split(path.sep).join('/'),
      line: test.location.line,
      project: test.parent.project()?.name ?? '',
      status: result.status,
      expectedStatus: test.expectedStatus,
      ok: !failed,
      retry: result.retry,
      durationMs: result.duration,
      errors: result.errors.map(e => stripAnsi(e.message ?? e.value ?? '').slice(0, 2000)).filter(Boolean),
      videos: [],
      keyframes: [],
    };
    this.tests.push(entry);
    if (videos.length === 0 && keyframes.length === 0) return;

    const project = slug(entry.project);
    const spec = slug(path.basename(test.location.file).replace(/\.(spec|setup)\.ts$/, ''));
    const base = `${spec}--${slug(test.title)}${result.retry ? `--retry${result.retry}` : ''}${failed ? '.failed' : ''}`;

    fs.mkdirSync(path.join(OUT_DIR, project), { recursive: true });
    videos.forEach((video, i) => {
      const suffix = videos.length > 1 ? `--page${i + 1}` : '';
      const target = path.join(OUT_DIR, project, `${base.replace(/\.failed$/, '')}${suffix}${failed ? '.failed' : ''}.webm`);
      fs.copyFileSync(video.path!, target);
      this.saved.push(target);
      entry.videos.push(rel(target));
    });

    if (keyframes.length) {
      const dir = path.join(OUT_DIR, project, `${base}.keyframes`);
      fs.mkdirSync(dir, { recursive: true });
      keyframes.forEach((frame, i) => {
        const label = frame.name.slice('keyframe: '.length);
        const target = path.join(dir, `${String(i + 1).padStart(2, '0')}-${slug(label).slice(0, 60)}.png`);
        fs.copyFileSync(frame.path!, target);
        entry.keyframes.push({ label, path: rel(target) });
      });
    }
  }

  onEnd() {
    if (this.tests.some(t => t.videos.length || t.keyframes.length)) {
      fs.mkdirSync(OUT_DIR, { recursive: true });
      fs.writeFileSync(MANIFEST, JSON.stringify({ startedAt: this.startedAt, finishedAt: new Date().toISOString(), tests: this.tests }, null, 2));
    }
    if (this.saved.length === 0) return;
    console.log(`\n[video] ${this.saved.length} test video(s) saved:`);
    for (const file of this.saved) console.log(`  ${file}`);
    console.log(`[video] keyframes and errors per test: ${MANIFEST}`);
  }

  printsToStdio() {
    return false;
  }
}
