import { Injectable, signal } from '@angular/core';
import { Observable } from 'rxjs';

export type ConfirmationSeverity = 'warning' | 'error' | 'info';

export interface ConfirmationRequest {
  messageKey: string;
  titleKey: string;
  confirmKey: string;
  severity: ConfirmationSeverity;
  params?: Record<string, unknown>;
  resolve: (confirmed: boolean) => void;
}

export interface ConfirmationOptions {
  titleKey?: string;
  confirmKey?: string;
  severity?: ConfirmationSeverity;
  params?: Record<string, unknown>;
}

/** One confirmation dialog at a time; rendered by <app-confirmation-dialog>. */
@Injectable({ providedIn: 'root' })
export class ConfirmationService {
  readonly current = signal<ConfirmationRequest | null>(null);

  confirm(
    messageKey: string,
    options: ConfirmationOptions = {}
  ): Observable<boolean> {
    return new Observable<boolean>((subscriber) => {
      this.current()?.resolve(false);
      this.current.set({
        messageKey,
        titleKey: options.titleKey ?? 'General.AreYouSure',
        confirmKey: options.confirmKey ?? 'General.Yes',
        severity: options.severity ?? 'warning',
        params: options.params,
        resolve: (confirmed) => {
          this.current.set(null);
          subscriber.next(confirmed);
          subscriber.complete();
        },
      });
      return () => {
        if (this.current()?.messageKey === messageKey) this.current.set(null);
      };
    });
  }
}
