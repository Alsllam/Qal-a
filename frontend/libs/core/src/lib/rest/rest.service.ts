import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { APP_SETTINGS } from '../settings/app-settings';
import { HttpErrorReporterService } from './http-error-reporter.service';
import { RestConfig, RestRequest } from './rest.models';

/** The only way features talk to the backend. Proxies wrap it. */
@Injectable({ providedIn: 'root' })
export class RestService {
  private readonly http = inject(HttpClient);
  private readonly settings = inject(APP_SETTINGS);
  private readonly errorReporter = inject(HttpErrorReporterService);

  request<TBody, TResult>(
    request: RestRequest<TBody>,
    config: RestConfig = {}
  ): Observable<TResult> {
    const url = this.resolveUrl(request.url, config.apiName);
    return this.http
      .request<TResult>(request.method, url, {
        body: request.body,
        params: request.params,
      })
      .pipe(
        catchError((error: unknown) => {
          if (!config.skipHandleError && error instanceof HttpErrorResponse) {
            this.errorReporter.reportError(error);
          }
          return throwError(() => error);
        })
      );
  }

  resolveUrl(url: string, apiName?: string): string {
    if (/^https?:\/\//.test(url)) return url;
    const api = apiName ? this.settings.apis[apiName] : undefined;
    if (apiName && !api) {
      throw new Error(
        `API "${apiName}" is missing from app-settings.json → apis`
      );
    }
    const base = (api?.url ?? '').replace(/\/+$/, '');
    return `${base}/${url.replace(/^\/+/, '')}`;
  }
}
