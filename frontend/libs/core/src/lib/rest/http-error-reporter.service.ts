import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';

/** Collects HTTP errors; the theme-shared error handler turns them into toasts. */
@Injectable({ providedIn: 'root' })
export class HttpErrorReporterService {
  private readonly errors = new Subject<HttpErrorResponse>();
  readonly reporter$: Observable<HttpErrorResponse> =
    this.errors.asObservable();

  reportError(error: HttpErrorResponse): void {
    this.errors.next(error);
  }
}
