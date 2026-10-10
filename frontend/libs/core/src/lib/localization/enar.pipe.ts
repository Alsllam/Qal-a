import { Pipe, PipeTransform, inject } from '@angular/core';
import { LocalizationService } from './localization.service';
import { localizedName } from '../utils/localized-name';

/**
 * Language-aware display:
 * - `value | enar`              numbers and digit strings follow the language (٣٤ / 34)
 * - `dto | enar:'name'`         picks `nameAr` / `nameEn` from a bilingual DTO
 */
@Pipe({ name: 'enar', pure: false })
export class EnArPipe implements PipeTransform {
  private readonly l10n = inject(LocalizationService);

  transform(value: unknown, field?: string): string {
    if (value === null || value === undefined) return '';
    if (field && typeof value === 'object') {
      return localizedName(
        value as Record<string, unknown>,
        this.l10n.currentLang(),
        field
      );
    }
    if (typeof value === 'number') return this.l10n.formatNumber(value);
    return this.l10n.localizeDigits(String(value));
  }
}
