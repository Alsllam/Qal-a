#!/usr/bin/env python3
"""Generate platform token files from brand/tokens.json and check contrast.

Outputs (all in brand/dist/, committed so apps build without Python):
  tokens.css          CSS custom properties, light on :root, dark on [data-theme="dark"]
  _tokens.scss        Sass variables + the CSS file content (for the Angular theme)
  tokens.ts           Typed constants (ECharts theme, X6 graph colours)
  app_tokens.g.dart   Dart constants for the Flutter app (Color, durations, sizes)

Fails (exit 1) when a pair listed in `contrastPairs` is below its WCAG ratio.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
TOKENS = json.loads((ROOT / "tokens.json").read_text(encoding="utf-8"))
DIST = ROOT / "dist"


def resolve(value: str) -> str:
    """Turn a reference such as `brand.500` into its hex value."""
    if value.startswith("#"):
        return value.upper()
    node = TOKENS["color"]
    for part in value.split("."):
        node = node[part]
    return str(node).upper()


def luminance(hex_color: str) -> float:
    rgb = [int(hex_color[i : i + 2], 16) / 255 for i in (1, 3, 5)]
    lin = [c / 12.92 if c <= 0.03928 else ((c + 0.055) / 1.055) ** 2.4 for c in rgb]
    return 0.2126 * lin[0] + 0.7152 * lin[1] + 0.0722 * lin[2]


def contrast(a: str, b: str) -> float:
    la, lb = sorted((luminance(a), luminance(b)), reverse=True)
    return (la + 0.05) / (lb + 0.05)


def flat_colors() -> list[tuple[str, str]]:
    """(css-name, hex) for every palette colour."""
    out: list[tuple[str, str]] = []
    for group, values in TOKENS["color"].items():
        if group == "status":
            for status, shades in values.items():
                for shade, hex_value in shades.items():
                    out.append((f"{status}-{shade}", hex_value.upper()))
        elif group == "game":
            for name, hex_value in values.items():
                kebab = "".join(f"-{c.lower()}" if c.isupper() else c for c in name)
                out.append((f"game-{kebab}", hex_value.upper()))
        else:
            for shade, hex_value in values.items():
                out.append((f"{group}-{shade}", hex_value.upper()))
    return out


def kebab(name: str) -> str:
    return "".join(f"-{c.lower()}" if c.isupper() else c for c in name)


def camel(name: str) -> str:
    parts = name.replace("-", " ").split()
    return parts[0] + "".join(p[:1].upper() + p[1:] for p in parts[1:])


def build_css() -> str:
    t = TOKENS
    lines = ["/* Generated from brand/tokens.json. Do not edit by hand. */", ":root {"]
    lines += [f"  --{name}: {hex_value};" for name, hex_value in flat_colors()]
    for key, value in t["themes"]["light"].items():
        lines.append(f"  --{kebab(key)}: {resolve(value)};")
    for key, px in t["radius"].items():
        lines.append(f"  --radius-{key}: {px}px;")
    for key, value in t["shadow"].items():
        lines.append(f"  --shadow-{key}: {value};")
    for key, px in t["typography"]["scale"].items():
        lines.append(f"  --fs-{key}: {px}px;")
    m = t["motion"]
    lines += [
        f"  --motion-fast: {m['fast']}ms;",
        f"  --motion-base: {m['base']}ms;",
        f"  --motion-slow: {m['slow']}ms;",
        f"  --ease-out: cubic-bezier({', '.join(map(str, m['easeOut']))});",
        f"  --ease-in: cubic-bezier({', '.join(map(str, m['easeIn']))});",
        f"  --font-arabic: '{t['typography']['arabic']}', system-ui, sans-serif;",
        f"  --font-latin: '{t['typography']['latin']}', system-ui, sans-serif;",
        "}",
        '[data-theme="dark"] {',
    ]
    for key, value in t["themes"]["dark"].items():
        lines.append(f"  --{kebab(key)}: {resolve(value)};")
    lines.append("}")
    lines += [
        "@media (prefers-reduced-motion: reduce) {",
        "  :root { --motion-fast: 0ms; --motion-base: 0ms; --motion-slow: 0ms; }",
        "}",
    ]
    return "\n".join(lines) + "\n"


def build_scss(css: str) -> str:
    lines = ["// Generated from brand/tokens.json. Do not edit by hand."]
    lines += [f"${name}: {hex_value};" for name, hex_value in flat_colors()]
    for theme, values in TOKENS["themes"].items():
        lines.append(f"${theme}-theme: (")
        lines += [f"  {kebab(k)}: {resolve(v)}," for k, v in values.items()]
        lines.append(");")
    return "\n".join(lines) + "\n\n" + css


def build_ts() -> str:
    t = TOKENS
    palette = {camel(name): hex_value for name, hex_value in flat_colors()}
    themes = {
        theme: {k: resolve(v) for k, v in values.items()}
        for theme, values in t["themes"].items()
    }
    body = {
        "palette": palette,
        "themes": themes,
        "chartSeries": [
            resolve("brand.500"), resolve("accent.500"), resolve("water.500"),
            t["color"]["status"]["success"]["500"], t["color"]["status"]["danger"]["500"],
            resolve("brand.300"), resolve("neutral.500"),
        ],
        "fonts": {"arabic": t["typography"]["arabic"], "latin": t["typography"]["latin"]},
        "motion": t["motion"],
        "radius": t["radius"],
    }
    return (
        "// Generated from brand/tokens.json. Do not edit by hand.\n"
        f"export const brandTokens = {json.dumps(body, ensure_ascii=False, indent=2)} as const;\n"
        "export type BrandTokens = typeof brandTokens;\n"
    )


def dart_color(hex_value: str) -> str:
    return f"Color(0xFF{hex_value[1:]})"


def build_dart() -> str:
    t = TOKENS
    out = [
        "// Generated from brand/tokens.json. Do not edit by hand.",
        "// ignore_for_file: public_member_api_docs",
        "import 'dart:ui';",
        "",
        "abstract final class BrandPalette {",
    ]
    for name, hex_value in flat_colors():
        out.append(f"  static const {camel(name).replace('-', '')} = {dart_color(hex_value)};")
    out.append("}")
    for theme, values in t["themes"].items():
        out.append("")
        out.append(f"abstract final class Brand{theme.capitalize()}Scheme {{")
        for key, value in values.items():
            out.append(f"  static const {key} = {dart_color(resolve(value))};")
        out.append("}")
    m = t["motion"]
    out += [
        "",
        "abstract final class BrandMotion {",
        f"  static const fast = Duration(milliseconds: {m['fast']});",
        f"  static const base = Duration(milliseconds: {m['base']});",
        f"  static const slow = Duration(milliseconds: {m['slow']});",
        f"  static const logo = Duration(milliseconds: {m['logo']});",
        "}",
        "",
        "abstract final class BrandRadius {",
    ]
    out += [f"  static const double {k} = {v};" for k, v in t["radius"].items()]
    out += ["}", "", "abstract final class BrandFontSize {"]
    for key, px in t["typography"]["scale"].items():
        name = {"2xl": "xxl", "3xl": "xxxl", "4xl": "display"}.get(key, key)
        out.append(f"  static const double {name} = {px};")
    out += [
        "}",
        "",
        "abstract final class BrandFonts {",
        f"  static const arabic = '{t['typography']['arabic']}';",
        f"  static const latin = '{t['typography']['latin']}';",
        "}",
        "",
    ]
    return "\n".join(out)


def check_contrast() -> list[str]:
    failures = []
    for theme, fg, bg, minimum in TOKENS["contrastPairs"]:
        values = TOKENS["themes"][theme]
        ratio = contrast(resolve(values[fg]), resolve(values[bg]))
        status = "ok " if ratio >= minimum else "LOW"
        print(f"  {status} {theme:5} {fg:>13} on {bg:<14} {ratio:5.2f} (min {minimum})")
        if ratio < minimum:
            failures.append(f"{theme} {fg}/{bg}")
    return failures


def main() -> int:
    DIST.mkdir(exist_ok=True)
    css = build_css()
    (DIST / "tokens.css").write_text(css, encoding="utf-8")
    (DIST / "_tokens.scss").write_text(build_scss(css), encoding="utf-8")
    (DIST / "tokens.ts").write_text(build_ts(), encoding="utf-8")
    (DIST / "app_tokens.g.dart").write_text(build_dart(), encoding="utf-8")
    print("Contrast (WCAG):")
    failures = check_contrast()
    if failures:
        print("Contrast check failed: " + ", ".join(failures))
        return 1
    print(f"Wrote {', '.join(p.name for p in sorted(DIST.iterdir()))}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
