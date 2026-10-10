import {
  inject,
  makeEnvironmentProviders,
  provideAppInitializer,
} from '@angular/core';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { LocalizationService } from './localization.service';

/** ngx-translate with `i18n/{ar,en}.json`, Arabic default, waits for the first load. */
export function provideQalaLocalization() {
  return makeEnvironmentProviders([
    provideTranslateService({
      loader: provideTranslateHttpLoader({
        prefix: 'i18n/',
        suffix: '.json',
        useHttpBackend: true,
      }),
      fallbackLang: 'en',
    }),
    provideAppInitializer(() => inject(LocalizationService).init()),
  ]);
}
