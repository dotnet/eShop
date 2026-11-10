import { test as base, expect, Browser, BrowserContext, BrowserContextOptions, Page, TestInfo, TestType, Video } from '@playwright/test';

/**
 * `test` for every e2e spec. Playwright only records video for the built-in `page`/`context` fixtures;
 * this also records contexts a test opens itself with `browser.newContext()` (multi-user flows), using the
 * project's `video` settings, and attaches each page's video as `video` so the video reporter collects it.
 *
 *   import { test, expect } from '../fixtures';
 *
 * Contexts opened in `beforeAll`/`afterAll` are not recorded: they are setup, not part of a test's flow.
 *
 * Agents can't watch a video, so every recorded test also gets keyframes: labelled screenshots taken at
 * the end of each `test.step`, at each `demo` checkpoint (e2e/demo.ts), and of every open page when the
 * test ends (`final`, or `failure` when it failed). They are attached as `keyframe: <label>`, and
 * e2e/video-reporter.ts lists them next to the videos in artifacts/e2e-videos/manifest.json.
 */

type VideoOption = { mode?: string; size?: { width: number; height: number }; show?: { actions?: object } };

let current:
  | { testInfo: TestInfo; contexts: BrowserContext[]; videos: (Video | null)[]; pages: Page[]; manualPages: Set<Page>; frames: number }
  | undefined;

function keyframeSlug(value: string) {
  return value.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '').slice(0, 60) || 'frame';
}

/** Screenshot `pages` (default: every open page of the current test) and attach them as keyframes. Never throws. */
export async function captureKeyframe(label: string, pages?: Page[]) {
  const recording = current;
  if (!recording || !videoSettings(recording.testInfo)) return;
  const open = (pages ?? recording.pages).filter(page => !page.isClosed());
  for (const [i, page] of open.entries()) {
    const name = open.length > 1 ? `${label} [page ${i + 1}]` : label;
    const file = recording.testInfo.outputPath('keyframes', `${String(++recording.frames).padStart(2, '0')}-${keyframeSlug(name)}.png`);
    try {
      await page.screenshot({ path: file, timeout: 5000, animations: 'disabled', caret: 'initial' });
      await recording.testInfo.attach(`keyframe: ${name}`, { path: file, contentType: 'image/png' });
    } catch {
      // A page that is navigating or crashed is not worth failing the test over.
    }
  }
}

/** Makes `test.step` capture a keyframe when each step ends (labelled `failed` if it threw). */
export function withStepKeyframes<T extends TestType<any, any>>(testType: T): T {
  const original = testType.step;
  const step = original.bind(testType);
  const wrapped = ((title: string, body: (...args: any[]) => any, options?: any) =>
    step(title, async (...args: any[]) => {
      try {
        const result = await body(...args);
        await captureKeyframe(`step: ${title}`);
        return result;
      } catch (error) {
        await captureKeyframe(`step failed: ${title}`);
        throw error;
      }
    }, options)) as T['step'];
  Object.assign(wrapped, original);
  testType.step = wrapped;
  return testType;
}

function videoSettings(testInfo: TestInfo): VideoOption | undefined {
  const video = (testInfo.project.use as { video?: string | VideoOption }).video;
  const settings = typeof video === 'string' ? { mode: video } : video;
  if (!settings?.mode || settings.mode === 'off') return undefined;
  if (settings.mode === 'on-first-retry' && testInfo.retry !== 1) return undefined;
  return settings;
}

export const test = withStepKeyframes(base.extend<{ _recordManualContexts: void }, { browser: Browser }>({
  page: async ({ page }, use, testInfo) => {
    current?.pages.push(page);
    await use(page);
    await captureKeyframe(testInfo.status === testInfo.expectedStatus ? 'final' : 'failure', [page]);
  },

  browser: [
    async ({ browser }, use) => {
      const newContext = browser.newContext.bind(browser);
      browser.newContext = async (options: BrowserContextOptions = {}) => {
        const settings = current && !options.recordVideo ? videoSettings(current.testInfo) : undefined;
        if (!current || !settings) return newContext(options);
        const context = await newContext({
          ...options,
          recordVideo: {
            dir: current.testInfo.outputPath('videos'),
            size: settings.size ?? { width: 1280, height: 720 },
            ...(settings.show?.actions ? { showActions: settings.show.actions } : {}),
          },
        });
        const recording = current;
        recording.contexts.push(context);
        context.on('page', page => {
          recording.videos.push(page.video());
          recording.pages.push(page);
          recording.manualPages.add(page);
        });
        return context;
      };
      await use(browser);
      browser.newContext = newContext;
    },
    { scope: 'worker' },
  ],

  _recordManualContexts: [
    async ({}, use, testInfo) => {
      const contexts: BrowserContext[] = [];
      const videos: (Video | null)[] = [];
      current = { testInfo, contexts, videos, pages: [], manualPages: new Set(), frames: 0 };
      await use();
      await captureKeyframe(testInfo.status === testInfo.expectedStatus ? 'final' : 'failure', [...current.manualPages]);
      current = undefined;

      // Videos are only written once their context closes.
      await Promise.all(contexts.map(context => context.close().catch(() => {})));
      const passed = testInfo.status === testInfo.expectedStatus;
      const mode = videoSettings(testInfo)?.mode;
      if (passed && (mode === 'retain-on-failure' || mode === 'retain-on-first-failure')) return;
      for (const video of videos) {
        const path = await video?.path().catch(() => undefined);
        if (path) await testInfo.attach('video', { path, contentType: 'video/webm' });
      }
    },
    { auto: true },
  ],
}));

export { expect };
