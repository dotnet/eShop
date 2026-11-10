import fs from 'fs';
import path from 'path';
import type { FullConfig, Reporter, Suite, TestCase, TestResult } from '@playwright/test/reporter';

/**
 * Copies every test's video (`use.video` in playwright.config.ts) to a stable, readable path:
 * artifacts/e2e-videos/<project>/<spec>--<test>[--retryN][.failed].webm, and prints the list at the end.
 */
const OUT_DIR = path.join(__dirname, '..', 'artifacts', 'e2e-videos');

function slug(value: string) {
  return value.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '').slice(0, 80) || 'default';
}

export default class VideoReporter implements Reporter {
  private saved: string[] = [];

  onBegin(_config: FullConfig, _suite: Suite) {
    fs.rmSync(OUT_DIR, { recursive: true, force: true });
  }

  onTestEnd(test: TestCase, result: TestResult) {
    const videos = result.attachments.filter(a => a.name === 'video' && a.path && fs.existsSync(a.path));
    if (videos.length === 0) return;

    const project = slug(test.parent.project()?.name ?? '');
    const spec = slug(path.basename(test.location.file).replace(/\.(spec|setup)\.ts$/, ''));
    const failed = result.status !== test.expectedStatus;
    const base = `${spec}--${slug(test.title)}${result.retry ? `--retry${result.retry}` : ''}`;

    fs.mkdirSync(path.join(OUT_DIR, project), { recursive: true });
    videos.forEach((video, i) => {
      const suffix = videos.length > 1 ? `--page${i + 1}` : '';
      const target = path.join(OUT_DIR, project, `${base}${suffix}${failed ? '.failed' : ''}.webm`);
      fs.copyFileSync(video.path!, target);
      this.saved.push(target);
    });
  }

  onEnd() {
    if (this.saved.length === 0) return;
    console.log(`\n[video] ${this.saved.length} test video(s) saved:`);
    for (const file of this.saved) console.log(`  ${file}`);
  }

  printsToStdio() {
    return false;
  }
}
