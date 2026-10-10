import {
  HttpEvent,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { InjectionToken, inject } from '@angular/core';
import { Observable } from 'rxjs';

/**
 * A dev-only handler that answers API requests without a backend. It returns
 * `null` for requests it does not handle. Only the app provides it, and only
 * when `useMockApi` is on and the build allows mocks.
 */
export type MockApiHandler = (
  req: HttpRequest<unknown>
) => Observable<HttpEvent<unknown>> | null;

export const MOCK_API_HANDLER = new InjectionToken<MockApiHandler>(
  'MOCK_API_HANDLER'
);

export const mockApiInterceptor: HttpInterceptorFn = (req, next) => {
  const handler = inject(MOCK_API_HANDLER, { optional: true });
  return handler?.(req) ?? next(req);
};
