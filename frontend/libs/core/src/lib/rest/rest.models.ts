import { HttpParams } from '@angular/common/http';

export type HttpVerb = 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';

export interface RestRequest<TBody> {
  method: HttpVerb;
  url: string;
  body?: TBody;
  params?: HttpParams | Record<string, string | number | boolean>;
}

export interface RestConfig {
  /** Key in `app-settings.json` → `apis`. */
  apiName?: string;
  /** The caller shows the error itself: no toast, no redirect. */
  skipHandleError?: boolean;
}
