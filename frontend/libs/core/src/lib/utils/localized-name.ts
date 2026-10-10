import { SupportedLanguage } from '../settings/app-settings.model';

/** Picks `{field}Ar` / `{field}En` (falls back to the other language, then to `{field}`). */
export function localizedName(
  dto: Record<string, unknown>,
  lang: SupportedLanguage,
  field = 'name'
): string {
  const ar = dto[`${field}Ar`];
  const en = dto[`${field}En`];
  const primary = lang === 'ar' ? ar : en;
  const secondary = lang === 'ar' ? en : ar;
  const value = primary ?? secondary ?? dto[field];
  return value === null || value === undefined ? '' : String(value);
}
