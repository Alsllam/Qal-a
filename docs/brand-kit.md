# Qal'a brand kit (قلعة)

**Version 1.0.** The single source of truth is [`brand/tokens.json`](../brand/tokens.json). After editing it, run:

```sh
python3 brand/tools/build_tokens.py     # regenerates brand/dist/* and checks WCAG contrast
```

The generated files are consumed as follows:

| File | Used by |
|---|---|
| `brand/dist/tokens.css`, `_tokens.scss` | Angular frontend theme (`frontend/theme-layout-generator`) |
| `brand/dist/tokens.ts` | ECharts theme and X6 graph colours (`frontend/libs/shared/charts`) |
| `brand/dist/app_tokens.g.dart` | Flutter app theme (`mobile/lib/app/theme`) |

## 1. Idea

> **A fortress that guards water.** The game is about fortresses (*qal'a*) and wells: you win by keeping your army linked to water. The brand says this in one image.

The tone is calm, clever and warm, like desert evenings and an old game board, not a war game. It should feel heritage without being folkloric, and modern without being cold.

## 2. Logo

![Logo preview](../brand/png/preview.png)

| File | Use |
|---|---|
| `brand/logo/logo-mark.svg` | The mark: a crenellated tower whose gate is a water drop. Works from 16 px |
| `brand/logo/logo-en.svg`, `logo-ar.svg` | Full lockups. In Arabic the mark sits on the right (reading start) |
| `brand/logo/logo-mono.svg` | One colour (`currentColor`), with the drop cut out. For dark backgrounds, print and embossing |
| `brand/logo/favicon.svg` + `brand/png/*` | Favicon set, web manifest icons, apple-touch icon |
| `brand/logo/app-icon*.svg` + `brand/png/app-icon-*` | App icon: full-bleed, Android adaptive foreground, and monochrome (themed icon) |

**Rules**
- Leave clear space around the mark equal to one merlon width (1/5 of the tower width).
- Never recolour the drop red, stretch the mark, add shadows or outlines, or put the colour mark on a busy photo (use the mono version instead).
- Minimum size: 16 px for the mark, 96 px wide for a lockup.
- The mark and lockups are original geometric drawings (rectangles plus one drop). Before launch, run a **trademark search** for the name and mark in the target markets.

**Motion:** on the splash screen and the web login, the tower rises from the baseline (scale-y 0.6 → 1), then the drop falls into the gate and lands with a small bounce. The whole thing takes 800 ms and plays once. It never loops on working screens.

## 3. Colour

| Role | Token | Hex | Meaning |
|---|---|---|---|
| Primary | `brand-500` | `#3D4BA6` | Night-sky indigo: the fortress, the brand |
| Accent | `accent-500` | `#E0A21B` | Saffron: selection, highlights, rewards |
| Water | `water-500` | `#2F8FD0` | Wells, supply, water points. **Reserved for water meaning** |
| Neutrals | `neutral-0…900` | `#FFFFFF … #231C14` | Warm sand: surfaces and text |
| Success / Warning / Danger / Info | `success-500` … | `#2E9D6B`, `#E0A21B`, `#D64541`, `#2F8FD0` | Each comes with a `-50` tint for backgrounds |

Full 10-step brand scale, plus light and dark themes (dark is a raised indigo night, not inverted colours), are in `tokens.json`.

**Game colours** (`color.game`): sand pieces for South, indigo pieces for North, a warm board, blue Wells and a cyan supply glow. These are the same colours as the playtest prototype, so playtesters will recognise the production game.

**Contrast:** every text/background pair used is checked by the build script (WCAG AA). Current results: body text 16.8:1 in light and 15.0:1 in dark, primary button text 7.6:1 in light and 5.5:1 in dark.

**Chart series order:** brand, accent, water, success, danger, brand-300, neutral-500.

## 4. Typography

- **Arabic:** IBM Plex Sans Arabic. **Latin:** IBM Plex Sans. Both are under the SIL Open Font License and self-hosted in each app; they are designed as a pair, so mixed Arabic/English text looks balanced.
- Scale (px): 12 / 14 / 16 / 18 / 20 / 24 / 30 / 36. Weights: 400 / 500 / 600 / 700. Line height: 1.5 for body, 1.25 for headings.
- Numbers in scores and tables use tabular figures. The digits follow the locale (Arabic-Indic in Arabic, if the user prefers it).

## 5. Shape, space and depth

- Radius: 6 / 8 / 12 / 16 px. Cards use 12 px, and sheets/modals 16 px.
- Spacing on an 8 px scale: 4, 8, 16, 24, 32, 48.
- Shadows are soft and tinted indigo (`shadow-sm/md/lg`), never grey-black.
- **Pattern:** a subtle repeating motif made from merlon shapes can be used as a page or card background at 4–6% opacity. Never use it behind text that must be read.

## 6. Motion

| Token | Value | Use |
|---|---|---|
| `motion.fast` | 120 ms | press, hover, focus |
| `motion.base` | 200 ms | tabs, chips, toasts, piece select |
| `motion.slow` | 320 ms | sheets, modals, page transitions |
| `motion.logo` | 800 ms | splash/login logo animation |
| `easeOut` / `easeIn` | `cubic-bezier(.2,.8,.2,1)` / `(.4,0,1,1)` | entering / leaving |

**Game-specific motion:**
- A piece slides in 180 ms with ease-out.
- A Rami shot draws a line and fades out in 450 ms.
- Supply flows outward along a chain when it is reconnected: a 200 ms ripple, one piece every 40 ms.
- When water is gained, a drop falls into the water bar.

All of these switch to opacity-only changes when the system asks for reduced motion.

## 7. Voice

| Do | Don't |
|---|---|
| "Your Faris is cut off from water. Link it back to capture." | "Illegal move!" |
| "Well held: +1 water." | "You scored." |
| Short sentences in Modern Standard Arabic, with a light touch of heritage imagery | Heavy archaic vocabulary, or slang |

The coach speaks like a patient older player: it explains *why*, and never mocks.
