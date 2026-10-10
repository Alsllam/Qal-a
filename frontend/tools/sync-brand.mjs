#!/usr/bin/env node
/**
 * Copies the Qal'a brand kit (repo-root `brand/`) into the frontend workspace.
 *
 * The brand kit is the single source of truth: never edit the copies, edit
 * `brand/tokens.json` and run `python3 brand/tools/build_tokens.py`, then this
 * script (it also runs on `npm install` and before every lint/test/build/serve
 * through the Nx `brand:sync` target).
 *
 * Outputs (all git-ignored):
 *   apps/admin/src/assets/brand/*                       logos, favicons, web manifest
 *   libs/theme-shared/src/styles/generated/_brand-tokens.scss   CSS variables (light + dark)
 *   libs/shared/charts/src/lib/generated/brand-tokens.ts        typed tokens for ECharts
 */
import {
  copyFileSync,
  existsSync,
  mkdirSync,
  readFileSync,
  writeFileSync,
} from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const workspace = resolve(here, '..');
const brand = resolve(workspace, '..', 'brand');

if (!existsSync(join(brand, 'dist', 'tokens.ts'))) {
  console.error(`[sync-brand] brand kit not found at ${brand}`);
  process.exit(1);
}

const banner = (src) =>
  `Copied from brand/${src} by tools/sync-brand.mjs. Do not edit.`;

function copy(src, dest) {
  mkdirSync(dirname(dest), { recursive: true });
  copyFileSync(join(brand, src), dest);
}

function writeIfChanged(dest, content) {
  mkdirSync(dirname(dest), { recursive: true });
  if (existsSync(dest) && readFileSync(dest, 'utf8') === content) return;
  writeFileSync(dest, content);
}

// 1. Logos and favicon set -> app assets
const assets = join(workspace, 'apps/admin/src/assets/brand');
for (const f of [
  'logo-ar.svg',
  'logo-en.svg',
  'logo-mark.svg',
  'logo-mono.svg',
  'favicon.svg',
]) {
  copy(join('logo', f), join(assets, f));
}
for (const f of [
  'favicon.ico',
  'favicon-32.png',
  'apple-touch-icon.png',
  'web-app-manifest-192x192.png',
  'web-app-manifest-512x512.png',
]) {
  copy(join('png', f), join(assets, f));
}

const tokens = readFileSync(join(brand, 'dist/tokens.ts'), 'utf8');
const theme =
  /"light":\s*{[^}]*"primary":\s*"(#[0-9A-Fa-f]{6})"[^}]*"surface":\s*"(#[0-9A-Fa-f]{6})"/.exec(
    tokens
  );
writeIfChanged(
  join(assets, 'site.webmanifest'),
  JSON.stringify(
    {
      name: "Qal'a Admin",
      short_name: "Qal'a",
      icons: [
        {
          src: 'web-app-manifest-192x192.png',
          sizes: '192x192',
          type: 'image/png',
          purpose: 'maskable',
        },
        {
          src: 'web-app-manifest-512x512.png',
          sizes: '512x512',
          type: 'image/png',
          purpose: 'maskable',
        },
      ],
      theme_color: theme ? theme[1] : '#3D4BA6',
      background_color: theme ? theme[2] : '#FFFFFF',
      display: 'standalone',
      start_url: '/',
    },
    null,
    2
  ) + '\n'
);

// 2. CSS variables -> theme-shared styles (the generated SCSS holds Sass vars + CSS vars)
writeIfChanged(
  join(workspace, 'libs/theme-shared/src/styles/generated/_brand-tokens.scss'),
  `// ${banner('dist/_tokens.scss')}\n` +
    readFileSync(join(brand, 'dist/_tokens.scss'), 'utf8')
);

// 3. Typed tokens -> charts library (ECharts brand themes)
writeIfChanged(
  join(workspace, 'libs/shared/charts/src/lib/generated/brand-tokens.ts'),
  `// ${banner('dist/tokens.ts')}\n/* eslint-disable */\n` + tokens
);

console.log('[sync-brand] brand kit synced from', brand);
