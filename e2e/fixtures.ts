import { test as base, expect, Browser, BrowserContext, BrowserContextOptions, TestInfo, Video } from '@playwright/test';

/**
 * `test` for every e2e spec. Playwright only records video for the built-in `page`/`context` fixtures;
 * this also records contexts a test opens itself with `browser.newContext()` (multi-user flows), using the
 * project's `video` settings, and attaches each page's video as `video` so the video reporter collects it.
 *
 *   import { test, expect } from '../fixtures';
 *
 * Contexts opened in `beforeAll`/`afterAll` are not recorded: they are setup, not part of a test's flow.
 */

type VideoOption = { mode?: string; size?: { width: number; height: number }; show?: { actions?: object } };

let current: { testInfo: TestInfo; contexts: BrowserContext[]; videos: (Video | null)[] } | undefined;

function videoSettings(testInfo: TestInfo): VideoOption | undefined {
  const video = (testInfo.project.use as { video?: string | VideoOption }).video;
  const settings = typeof video === 'string' ? { mode: video } : video;
  if (!settings?.mode || settings.mode === 'off') return undefined;
  if (settings.mode === 'on-first-retry' && testInfo.retry !== 1) return undefined;
  return settings;
}

export const test = base.extend<{ _recordManualContexts: void }, { browser: Browser }>({
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
        context.on('page', page => recording.videos.push(page.video()));
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
      current = { testInfo, contexts, videos };
      await use();
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
});

export { expect };
