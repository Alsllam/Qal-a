import { HttpErrorResponse } from '@angular/common/http';
import {
  inject,
  makeEnvironmentProviders,
  provideAppInitializer,
} from '@angular/core';
import { Router } from '@angular/router';
import {
  AuthService,
  HttpErrorReporterService,
  RemoteErrorDto,
} from '@qala-fe/Core';
import { ToasterService } from '../toaster/toaster.service';

/** Turns reported HTTP errors into toasts; 401 → sign in again, 403 → /403. */
export function provideErrorHandling() {
  return makeEnvironmentProviders([
    provideAppInitializer(() => {
      const reporter = inject(HttpErrorReporterService);
      const toaster = inject(ToasterService);
      const router = inject(Router);
      const auth = inject(AuthService);
      reporter.reporter$.subscribe((error: HttpErrorResponse) => {
        if (error.status === 401) {
          auth.login(router.url);
          return;
        }
        if (error.status === 403) {
          void router.navigateByUrl('/403');
          return;
        }
        const messages =
          (error.error as RemoteErrorDto | null)?.error?.messages ?? [];
        if (messages.length) toaster.error(messages.join(' '), undefined, true);
        else
          toaster.error(
            error.status === 0 ? 'General.NetworkError' : 'General.ErrorMessage'
          );
      });
    }),
  ]);
}
