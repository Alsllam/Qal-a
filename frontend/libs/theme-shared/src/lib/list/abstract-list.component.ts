import { Directive, inject } from '@angular/core';
import { BaseFilterDto } from '@qala-fe/Core';
import { Observable, filter, switchMap } from 'rxjs';
import {
  ConfirmationService,
  ConfirmationSeverity,
} from '../confirmation/confirmation.service';
import { ToasterService } from '../toaster/toaster.service';
import { ListFilterService } from './list-filter.service';

/** Base for list screens: the list engine plus confirm → run → toast → refresh. */
@Directive()
export abstract class AbstractListComponent<TItem> {
  readonly list = inject(ListFilterService) as ListFilterService<
    TItem,
    BaseFilterDto
  >;
  protected readonly confirmation = inject(ConfirmationService);
  protected readonly toaster = inject(ToasterService);

  /**
   * Opens the confirmation; when confirmed, runs the action, shows the success
   * toast and refreshes the list (stepping back a page if a row was removed).
   */
  protected confirmAndRun(
    messageKey: string,
    action: () => Observable<unknown>,
    options: {
      severity?: ConfirmationSeverity;
      params?: Record<string, unknown>;
      confirmKey?: string;
      removesRow?: boolean;
    } = {}
  ): void {
    this.confirmation
      .confirm(messageKey, {
        severity: options.severity,
        params: options.params,
        confirmKey: options.confirmKey,
      })
      .pipe(
        filter(Boolean),
        switchMap(() => action())
      )
      .subscribe(() => {
        this.toaster.success('General.SuccessMessage');
        if (options.removesRow) this.list.refreshAfterRemoval();
        else this.list.refresh();
      });
  }
}
