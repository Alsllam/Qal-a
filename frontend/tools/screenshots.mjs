#!/usr/bin/env node
/**
 * Headless screenshots of the demo build (mock API on).
 *
 *   npx nx build admin --configuration=demo && node tools/screenshots.mjs
 *
 * Serves dist/apps/admin-demo/browser with an SPA fallback on a free port and
 * drives the pre-installed Chromium (CHROMIUM_PATH, default /opt/pw-browsers/chromium).
 * Output: docs/screenshots/*.png
 */
import { createServer } from 'node:http';
import { existsSync, mkdirSync, readFileSync, statSync } from 'node:fs';
import { extname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from '@playwright/test';

const workspace = resolve(fileURLToPath(new URL('.', import.meta.url)), '..');
const root = join(workspace, 'dist/apps/admin-demo/browser');
const out = join(workspace, 'docs/screenshots');
const only = process.argv[2];

if (!existsSync(join(root, 'index.html'))) {
  console.error(
    'Build the demo first: npx nx build admin --configuration=demo'
  );
  process.exit(1);
}
mkdirSync(out, { recursive: true });

const types = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript',
  '.css': 'text/css',
  '.json': 'application/json',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.ico': 'image/x-icon',
  '.woff2': 'font/woff2',
  '.woff': 'font/woff',
  '.webmanifest': 'application/manifest+json',
};
const server = createServer((req, res) => {
  const path = decodeURIComponent(new URL(req.url, 'http://x').pathname);
  let file = join(root, path);
  if (
    !file.startsWith(root) ||
    !existsSync(file) ||
    statSync(file).isDirectory()
  )
    file = join(root, 'index.html');
  res.writeHead(200, {
    'content-type': types[extname(file)] ?? 'application/octet-stream',
  });
  res.end(readFileSync(file));
});
await new Promise((r) => server.listen(0, '127.0.0.1', r));
const base = `http://127.0.0.1:${server.address().port}`;

// The app needs no outbound network (fonts are self-hosted, the API is mocked),
// so Chromium runs without any proxy: loopback traffic must never hit an outbound relay.
const browser = await chromium.launch({
  executablePath: process.env.CHROMIUM_PATH ?? '/opt/pw-browsers/chromium',
  headless: true,
  args: ['--no-proxy-server'],
});

const shots = [
  {
    name: 'dashboard-ar-light-1440',
    path: '/dashboard',
    lang: 'ar',
    theme: 'light',
    width: 1440,
  },
  {
    name: 'dashboard-en-dark-1440',
    path: '/dashboard',
    lang: 'en',
    theme: 'dark',
    width: 1440,
  },
  {
    name: 'dashboard-ar-light-390',
    path: '/dashboard',
    lang: 'ar',
    theme: 'light',
    width: 390,
  },
  {
    name: 'dashboard-en-dark-390',
    path: '/dashboard',
    lang: 'en',
    theme: 'dark',
    width: 390,
  },
  {
    name: 'players-ar-light-1440',
    path: '/players',
    lang: 'ar',
    theme: 'light',
    width: 1440,
  },
  {
    name: 'players-en-dark-390',
    path: '/players',
    lang: 'en',
    theme: 'dark',
    width: 390,
  },
  {
    name: 'matches-ar-dark-1440',
    path: '/matches',
    lang: 'ar',
    theme: 'dark',
    width: 1440,
  },
  {
    name: 'match-replay-en-light-1440',
    path: '/matches',
    lang: 'en',
    theme: 'light',
    width: 1440,
    openFirstMatch: true,
  },
  {
    name: 'match-replay-ar-light-390',
    path: '/matches',
    lang: 'ar',
    theme: 'light',
    width: 390,
    openFirstMatch: true,
  },
  {
    name: 'login-ar-light-1440',
    path: '/login',
    lang: 'ar',
    theme: 'light',
    width: 1440,
    loggedOut: true,
  },
].filter((s) => !only || s.name.includes(only));

let failures = 0;
for (const shot of shots) {
  const context = await browser.newContext({
    viewport: { width: shot.width, height: shot.width < 600 ? 844 : 900 },
    deviceScaleFactor: shot.width < 600 ? 2 : 1,
    reducedMotion: 'reduce',
  });
  await context.addInitScript(({ lang, theme, loggedOut }) => {
    localStorage.setItem('qala.lang', lang);
    localStorage.setItem('qala.theme', theme);
    if (!loggedOut) sessionStorage.setItem('qala.devLogin', '1');
  }, shot);
  const page = await context.newPage();
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  page.on('console', (m) => m.type() === 'error' && errors.push(m.text()));
  await page.goto(base + shot.path, { waitUntil: 'networkidle' });
  if (shot.openFirstMatch) {
    await page.locator('tbody tr a.btn').first().click();
    await page.waitForSelector('app-board-replay svg');
  }
  // Trigger @defer (on viewport) blocks, then let charts render.
  for (
    let y = 0;
    y < (await page.evaluate(() => document.body.scrollHeight));
    y += 400
  ) {
    await page.evaluate((top) => window.scrollTo(0, top), y);
    await page.waitForTimeout(150);
  }
  await page.waitForTimeout(400);
  await page.evaluate(() => window.scrollTo(0, 0));
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(1200);
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth - window.innerWidth
  );
  await page.screenshot({
    path: join(out, `${shot.name}.png`),
    fullPage: true,
  });
  const note = [
    overflow > 0 ? `HORIZONTAL OVERFLOW ${overflow}px` : '',
    errors.length ? `errors: ${errors.join(' | ')}` : '',
  ]
    .filter(Boolean)
    .join('; ');
  if (note) failures++;
  console.log(`${note ? '✗' : '✓'} ${shot.name}.png ${note}`);
  await context.close();
}

await browser.close();
server.close();
process.exit(failures ? 1 : 0);
