import { Locator, Page } from '@playwright/test';
import { captureKeyframe, test as base, expect, withStepKeyframes } from './fixtures';

/**
 * Optional narration for e2e tests. Every e2e test is already recorded on video (see `video` in
 * playwright.config.ts); use this fixture to make a feature's video read like a demo.
 *
 *   import { test, expect } from '../demo';
 *   test('...', async ({ page, demo }) => { await demo.chapter('Checkout'); ... });
 */
export interface Demo {
  /** Full-screen title card that narrates the next step. Blocks for its duration. */
  chapter(title: string, description?: string, durationMs?: number): Promise<void>;
  /** Sticky caption at the bottom of the video. Replaces the previous caption; pass '' to clear. */
  caption(text: string): Promise<void>;
  /** Outline an element with an optional label, so viewers can see what is being verified. */
  highlight(locator: Locator, label?: string, durationMs?: number): Promise<void>;
  /** Type like a human, so the input is readable in the video. */
  type(locator: Locator, text: string): Promise<void>;
  /** Let the viewer catch up. */
  pause(ms?: number): Promise<void>;
}

function escapeHtml(value: string) {
  return value.replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]!);
}

export function createDemo(page: Page): Demo & { dispose(): Promise<void> } {
  let currentCaption: { dispose(): Promise<void> | void } | undefined;
  let currentChapter = '';
  const clearCaption = async () => {
    await currentCaption?.dispose();
    currentCaption = undefined;
  };

  return {
    chapter: async (title, description, durationMs = 2500) => {
      // Keyframe for agents: how the previous chapter ended, before the title card covers it.
      if (currentChapter) await captureKeyframe(`end of: ${currentChapter}`, [page]);
      currentChapter = title;
      await page.screencast.showChapter(title, { description, duration: durationMs });
    },
    caption: async text => {
      await clearCaption();
      if (!text) return;
      currentCaption = await page.screencast.showOverlay(`
        <div style="position:absolute; left:50%; bottom:28px; transform:translateX(-50%);
          max-width:80%; padding:10px 18px; border-radius:10px; background:rgba(15,23,42,.85);
          color:#fff; font:500 18px/1.4 system-ui, sans-serif; text-align:center;
          box-shadow:0 4px 18px rgba(0,0,0,.35);">${escapeHtml(text)}</div>`);
    },
    highlight: async (locator, label, durationMs = 2000) => {
      await locator.scrollIntoViewIfNeeded();
      const box = await locator.boundingBox();
      if (!box) throw new Error(`demo.highlight: ${locator} is not visible`);
      // Keyframe for agents: the element this step proves, before the overlay is drawn.
      await captureKeyframe(`${currentChapter ? `${currentChapter} - ` : ''}highlight: ${label ?? String(locator)}`, [page]);
      const labelHtml = label
        ? `<div style="position:absolute; top:${box.y + box.height + 8}px; left:${box.x + box.width / 2}px;
            transform:translateX(-50%); padding:6px 12px; border-radius:8px; background:#16a34a; color:#fff;
            font:600 15px/1.3 system-ui, sans-serif; white-space:nowrap;">${escapeHtml(label)}</div>`
        : '';
      await page.screencast.showOverlay(`
        <div style="position:absolute; top:${box.y - 4}px; left:${box.x - 4}px; width:${box.width + 8}px;
          height:${box.height + 8}px; border:3px solid #16a34a; border-radius:8px;
          box-shadow:0 0 0 9999px rgba(0,0,0,.15);"></div>${labelHtml}`, { duration: durationMs });
    },
    type: async (locator, text) => {
      await locator.click();
      await locator.pressSequentially(text, { delay: 60 });
    },
    pause: ms => page.waitForTimeout(ms ?? 1000),
    dispose: clearCaption,
  };
}

/** `test` with a `demo` fixture that narrates the test's video. */
export const test = withStepKeyframes(base.extend<{ demo: Demo }>({
  demo: async ({ page }, use) => {
    const demo = createDemo(page);
    await use(demo);
    // Hold the last frame so the end state is visible in the video.
    await page.waitForTimeout(500).catch(() => {});
    await demo.dispose().catch(() => {});
  },
}));

export { expect };
