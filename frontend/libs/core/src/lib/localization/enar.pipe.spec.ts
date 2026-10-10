import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { EnArPipe } from './enar.pipe';
import { LocalizationService } from './localization.service';

describe('EnArPipe', () => {
  let l10n: LocalizationService;
  let pipe: EnArPipe;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideTranslateService(), EnArPipe],
    });
    l10n = TestBed.inject(LocalizationService);
    pipe = TestBed.inject(EnArPipe);
  });

  it('uses Arabic-Indic digits in Arabic and Latin digits in English', async () => {
    await firstValueFrom(l10n.setLanguage('ar'));
    expect(pipe.transform(1500)).toBe('١٬٥٠٠');
    expect(pipe.transform('v0.6')).toBe('v٠.٦');
    await firstValueFrom(l10n.setLanguage('en'));
    expect(pipe.transform(1500)).toBe('1,500');
    expect(pipe.transform('v0.6')).toBe('v0.6');
  });

  it('picks the bilingual field for the current language', async () => {
    const dto = { nameAr: 'قلعة', nameEn: 'Fortress' };
    await firstValueFrom(l10n.setLanguage('ar'));
    expect(pipe.transform(dto, 'name')).toBe('قلعة');
    await firstValueFrom(l10n.setLanguage('en'));
    expect(pipe.transform(dto, 'name')).toBe('Fortress');
    expect(pipe.transform({ nameAr: 'فقط' }, 'name')).toBe('فقط');
  });

  it('switches document direction with the language', async () => {
    await firstValueFrom(l10n.setLanguage('ar'));
    expect(document.documentElement.dir).toBe('rtl');
    await firstValueFrom(l10n.setLanguage('en'));
    expect(document.documentElement.dir).toBe('ltr');
  });
});
