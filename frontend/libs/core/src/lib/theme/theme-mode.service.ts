import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark';
const THEME_KEY = 'qala.theme';

/** Light/dark mode, applied as `data-theme` on <html> (brand tokens react to it). */
@Injectable({ providedIn: 'root' })
export class ThemeModeService {
  private readonly document = inject(DOCUMENT);
  private readonly current = signal<ThemeMode>(this.initialMode());
  readonly mode = computed(() => this.current());
  readonly isDark = computed(() => this.current() === 'dark');

  constructor() {
    this.apply(this.current());
  }

  setMode(mode: ThemeMode): void {
    this.current.set(mode);
    this.apply(mode);
    try {
      localStorage.setItem(THEME_KEY, mode);
    } catch {
      /* storage unavailable */
    }
  }

  toggle(): void {
    this.setMode(this.isDark() ? 'light' : 'dark');
  }

  private apply(mode: ThemeMode): void {
    this.document.documentElement.setAttribute('data-theme', mode);
    this.document.documentElement.style.colorScheme = mode;
  }

  private initialMode(): ThemeMode {
    try {
      const stored = localStorage.getItem(THEME_KEY);
      if (stored === 'light' || stored === 'dark') return stored;
    } catch {
      /* storage unavailable */
    }
    const prefersDark =
      typeof window !== 'undefined' &&
      window.matchMedia?.('(prefers-color-scheme: dark)').matches;
    return prefersDark ? 'dark' : 'light';
  }
}
