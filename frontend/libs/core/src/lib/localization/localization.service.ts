import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, firstValueFrom, tap } from 'rxjs';
import { APP_SETTINGS } from '../settings/app-settings';
import { SupportedLanguage } from '../settings/app-settings.model';

const LANG_KEY = 'qala.lang';
export const SUPPORTED_LANGUAGES: SupportedLanguage[] = ['ar', 'en'];

/**
 * Current language, direction and number/date formatting.
 * Arabic is the default and switches the whole document to RTL.
 */
@Injectable({ providedIn: 'root' })
export class LocalizationService {
  private readonly translate = inject(TranslateService);
  private readonly document = inject(DOCUMENT);
  private readonly settings = inject(APP_SETTINGS, { optional: true });

  private readonly lang = signal<SupportedLanguage>('ar');
  readonly currentLang = computed(() => this.lang());
  readonly isRtl = computed(() => this.lang() === 'ar');
  /** Arabic-Indic digits (٠١٢…) in Arabic; the brand kit allows Latin digits too. */
  readonly arabicIndicDigits = signal(true);
  /** BCP-47 locale used by Intl formatters. */
  readonly locale = computed(() =>
    this.lang() === 'ar'
      ? this.arabicIndicDigits()
        ? 'ar-u-nu-arab'
        : 'ar-u-nu-latn'
      : 'en-GB'
  );

  init(): Promise<unknown> {
    const stored = readLocal(LANG_KEY) as SupportedLanguage | null;
    const lang =
      stored && SUPPORTED_LANGUAGES.includes(stored)
        ? stored
        : this.settings?.defaultLanguage ?? 'ar';
    return firstValueFrom(this.setLanguage(lang));
  }

  /** Switches once the translations are loaded, so signals never see missing keys. */
  setLanguage(lang: SupportedLanguage): Observable<unknown> {
    return this.translate.use(lang).pipe(
      tap(() => {
        const html = this.document.documentElement;
        html.lang = lang;
        html.dir = lang === 'ar' ? 'rtl' : 'ltr';
        writeLocal(LANG_KEY, lang);
        this.lang.set(lang);
      })
    );
  }

  toggleLanguage(): void {
    this.setLanguage(this.lang() === 'ar' ? 'en' : 'ar').subscribe();
  }

  instant(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params) as string;
  }

  formatNumber(
    value: number | null | undefined,
    options?: Intl.NumberFormatOptions
  ): string {
    if (value === null || value === undefined || Number.isNaN(value))
      return '—';
    return new Intl.NumberFormat(this.locale(), options).format(value);
  }

  /** `ratio` is 0..1. */
  formatPercent(ratio: number | null | undefined, fractionDigits = 1): string {
    return this.formatNumber(ratio, {
      style: 'percent',
      minimumFractionDigits: fractionDigits,
      maximumFractionDigits: fractionDigits,
    });
  }

  formatDate(
    value: string | Date | null | undefined,
    options?: Intl.DateTimeFormatOptions
  ): string {
    if (!value) return '—';
    const date = typeof value === 'string' ? new Date(value) : value;
    return new Intl.DateTimeFormat(
      this.locale(),
      options ?? { dateStyle: 'medium' }
    ).format(date);
  }

  /** Converts Latin digits in free text according to the current language. */
  localizeDigits(text: string): string {
    if (this.lang() !== 'ar' || !this.arabicIndicDigits()) return text;
    return text.replace(/[0-9]/g, (d) =>
      String.fromCharCode(0x0660 + Number(d))
    );
  }
}

function readLocal(key: string): string | null {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function writeLocal(key: string, value: string): void {
  try {
    localStorage.setItem(key, value);
  } catch {
    /* storage unavailable */
  }
}
