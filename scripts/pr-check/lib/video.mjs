// Video evidence for the PR check: find the videos, download linked ones, and turn each into a set of
// frame images the video critic can look at. Frames come from ffmpeg when it is installed, otherwise
// from Playwright's Chromium, which plays the file and screenshots it at each timestamp.
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { download, videoLinks } from './github.mjs';
import { log, run } from '../../pipeline/lib/util.mjs';

const isUrl = (s) => /^https?:\/\//i.test(s);

function filesUnder(dir, extensions) {
  return fs
    .readdirSync(dir, { recursive: true, withFileTypes: true })
    .filter((e) => e.isFile() && extensions.includes(path.extname(e.name).toLowerCase()))
    .map((e) => path.join(e.parentPath ?? e.path, e.name))
    .sort();
}

// Explicit --videos values win; otherwise links in the PR body, then the default folders
// (Playwright's test-results/ when tests ran with video recording on).
export function videoSources(config, { values = [], prBody = '' } = {}) {
  const { extensions, defaultDirs } = config.prCheck.video;
  const sources = [];
  const missing = [];
  const addPath = (p) => {
    const abs = path.resolve(config.root, p);
    if (!fs.existsSync(abs)) return missing.push({ source: p, reason: 'file or folder not found' });
    if (fs.statSync(abs).isDirectory()) for (const f of filesUnder(abs, extensions)) sources.push({ source: path.relative(config.root, f).replace(/\\/g, '/'), file: f });
    else sources.push({ source: p, file: abs });
  };
  for (const value of values) (isUrl(value) ? sources.push({ source: value, url: value }) : addPath(value));
  for (const url of videoLinks(prBody, extensions)) sources.push({ source: url, url });
  if (values.length === 0 && sources.length === 0) {
    for (const dir of defaultDirs) if (fs.existsSync(path.join(config.root, dir))) addPath(dir);
  }
  return { sources, missing };
}

const hasCmd = (cmd) => run(cmd, ['-version']).status === 0;

function timestamps(duration, { maxFrames, minIntervalSec }) {
  const count = Math.max(1, Math.min(maxFrames, Math.floor(duration / minIntervalSec) + 1));
  if (count === 1) return [duration / 2];
  const end = Math.max(0, duration - 0.05);
  return Array.from({ length: count }, (_, i) => Math.round(((i * end) / (count - 1)) * 100) / 100);
}

function ffmpegFrames(file, outDir, options) {
  const probe = run('ffprobe', ['-v', 'error', '-show_entries', 'format=duration', '-of', 'csv=p=0', file]);
  const duration = Number(probe.stdout.trim());
  if (!(duration > 0)) throw new Error(`ffprobe could not read the duration: ${probe.stderr.trim() || probe.stdout.trim()}`);
  const frames = timestamps(duration, options).map((t, i) => {
    const out = path.join(outDir, `frame-${String(i + 1).padStart(3, '0')}.png`);
    const result = run('ffmpeg', ['-hide_banner', '-loglevel', 'error', '-y', '-ss', String(t), '-i', file, '-frames:v', '1', '-vf', `scale=${options.width}:-2`, out]);
    if (result.status !== 0 || !fs.existsSync(out)) throw new Error(`ffmpeg failed at ${t}s: ${result.stderr.trim()}`);
    return { file: out, t };
  });
  return { duration, frames };
}

async function loadChromium() {
  for (const name of ['@playwright/test', 'playwright', 'playwright-core']) {
    try {
      const mod = await import(name);
      const chromium = mod.chromium ?? mod.default?.chromium;
      if (chromium) return chromium;
    } catch {
      // try the next package
    }
  }
  throw new Error('neither ffmpeg nor Playwright is available to read videos (run `npm ci` and `npx playwright install chromium`, or install ffmpeg)');
}

async function browserFrames(file, outDir, options) {
  const chromium = await loadChromium();
  const player = path.join(outDir, 'player.html');
  fs.writeFileSync(player, `<!doctype html><body style="margin:0;background:#000"><video id="v" muted preload="auto" src="${pathToFileURL(file).href}" style="width:100%;display:block"></video></body>`);
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage({ viewport: { width: options.width, height: Math.round((options.width * 9) / 16) } });
    await page.goto(pathToFileURL(player).href);
    const duration = await page.evaluate(async () => {
      const v = document.getElementById('v');
      await new Promise((resolve, reject) => {
        if (v.readyState >= 1) return resolve();
        v.onloadedmetadata = resolve;
        v.onerror = () => reject(new Error(v.error?.message || 'the browser cannot decode this video'));
      });
      // Recorded WebM files (Playwright, MediaRecorder) report no duration until the end has been seeked.
      if (!Number.isFinite(v.duration)) {
        await new Promise((resolve) => {
          v.ontimeupdate = () => {
            v.ontimeupdate = null;
            resolve();
          };
          v.currentTime = 1e101;
        });
      }
      return v.duration;
    });
    if (!(duration > 0)) throw new Error('the video has no duration');
    const frames = [];
    for (const [i, t] of timestamps(duration, options).entries()) {
      await page.evaluate(
        (time) =>
          new Promise((resolve) => {
            const v = document.getElementById('v');
            v.onseeked = () => resolve();
            v.currentTime = time;
          }),
        t,
      );
      const out = path.join(outDir, `frame-${String(i + 1).padStart(3, '0')}.png`);
      await page.locator('#v').screenshot({ path: out });
      frames.push({ file: out, t });
    }
    return { duration, frames };
  } finally {
    await browser.close();
    fs.rmSync(player, { force: true });
  }
}

// Returns frames.json content: every readable video with its frames (paths relative to `relativeTo`,
// the agents' working directory) and every source that could not be read, with the reason.
export async function extractVideoFrames(config, runDir, { sources, missing }, relativeTo = config.root) {
  const options = config.prCheck.video;
  const useFfmpeg = hasCmd('ffmpeg') && hasCmd('ffprobe');
  const videos = [];
  const unreadable = [...missing];
  for (const [i, source] of sources.entries()) {
    const dir = path.join(runDir, 'videos', String(i + 1).padStart(2, '0'));
    fs.mkdirSync(dir, { recursive: true });
    try {
      let file = source.file;
      if (source.url) {
        const ext = path.extname(new URL(source.url).pathname).toLowerCase();
        file = path.join(dir, `video${options.extensions.includes(ext) ? ext : '.mp4'}`);
        await download(source.url, file);
      }
      const { duration, frames } = useFfmpeg ? ffmpegFrames(file, dir, options) : await browserFrames(file, dir, options);
      videos.push({
        source: source.source,
        duration: Math.round(duration * 100) / 100,
        extractor: useFfmpeg ? 'ffmpeg' : 'chromium',
        frames: frames.map((f) => ({ file: path.relative(relativeTo, f.file).replace(/\\/g, '/'), t: f.t })),
      });
      log(`video ${source.source}: ${frames.length} frames over ${Math.round(duration)}s`);
    } catch (error) {
      unreadable.push({ source: source.source, reason: error.message });
      log(`video ${source.source} could not be read: ${error.message}`);
    }
  }
  return { videos, unreadable };
}
