import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  Directive,
  TemplateRef,
  computed,
  contentChild,
  inject,
  input,
} from '@angular/core';
import { BaseFilterDto, EnArPipe, LocalizationService } from '@qala-fe/Core';
import { TranslatePipe } from '@ngx-translate/core';
import { EmptyStateComponent } from '../empty-state/empty-state.component';
import { IconComponent } from '../icon/icon.component';
import { ListFilterService } from './list-filter.service';

export interface TableColumn {
  /** Translation key of the header. */
  key: string;
  align?: 'start' | 'end' | 'center';
  width?: string;
  /** Hide below 768 px. */
  hideOnMobile?: boolean;
}

/** Marks the row template: `<ng-template appTableRow let-row>…<td>…</td></ng-template>`. */
@Directive({ selector: 'ng-template[appTableRow]' })
export class TableRowDirective {
  readonly template =
    inject<TemplateRef<{ $implicit: unknown; index: number }>>(TemplateRef);
}

/**
 * Paged table bound to a ListFilterService: skeleton rows while loading, empty
 * and no-results states, and pagination.
 */
@Component({
  selector: 'app-paged-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NgTemplateOutlet,
    TranslatePipe,
    EnArPipe,
    EmptyStateComponent,
    IconComponent,
  ],
  template: `
    <div class="table-scroll">
      <table class="table" [attr.aria-busy]="list().loading()">
        <thead>
          <tr>
            @for (c of columns(); track c.key) {
            <th
              scope="col"
              [style.width]="c.width"
              [class]="'align-' + (c.align ?? 'start')"
              [class.hide-mobile]="c.hideOnMobile"
            >
              {{ c.key | translate }}
            </th>
            }
          </tr>
        </thead>
        <tbody>
          @if (list().loading()) { @for (r of skeletonRows; track r) {
          <tr class="skeleton-row">
            @for (c of columns(); track c.key) {
            <td [class.hide-mobile]="c.hideOnMobile">
              <span class="skeleton-line"></span>
            </td>
            }
          </tr>
          } } @else { @for (row of list().items(); track trackKey(row, $index);
          let i = $index) {
          <tr
            class="row-enter"
            [style.animation-delay.ms]="i < 10 ? i * 30 : 0"
          >
            <ng-container
              *ngTemplateOutlet="
                rowTpl()?.template ?? null;
                context: { $implicit: row, index: i }
              "
            />
          </tr>
          } }
        </tbody>
      </table>
    </div>
    @if (!list().loading() && list().items().length === 0) { @if
    (list().error()) {
    <app-empty-state icon="alert" [title]="'General.LoadError' | translate" />
    } @else if (list().hasFilter()) {
    <app-empty-state
      icon="search"
      [title]="'General.NoResults' | translate"
      [description]="'General.NoResultsHint' | translate"
    >
      <button
        type="button"
        class="btn btn-secondary"
        (click)="list().clearFilters()"
      >
        {{ 'General.ClearFilters' | translate }}
      </button>
    </app-empty-state>
    } @else {
    <app-empty-state
      [title]="emptyTitle() | translate"
      [description]="emptyDescription() | translate"
    />
    } } @if (list().totalCount() > 0) {
    <nav class="pager" [attr.aria-label]="'General.Pagination' | translate">
      <span class="range">{{
        'General.ShowingRange' | translate : rangeParams()
      }}</span>
      <div class="pager-actions">
        <label class="page-size">
          <span>{{ 'General.PageSize' | translate }}</span>
          <select
            class="form-select form-select-sm"
            (change)="list().setPageSize(+$any($event.target).value)"
          >
            @for (s of pageSizes; track s) {
            <option [value]="s" [selected]="s === list().pageSize()">
              {{ s | enar }}
            </option>
            }
          </select>
        </label>
        <button
          type="button"
          class="btn btn-icon"
          [disabled]="list().page() === 0"
          (click)="list().setPage(list().page() - 1)"
          [attr.aria-label]="'General.PreviousPage' | translate"
        >
          <app-icon name="chevronStart" [mirror]="true" />
        </button>
        <span class="page-num"
          >{{ list().page() + 1 | enar }} /
          {{ list().pageCount() | enar }}</span
        >
        <button
          type="button"
          class="btn btn-icon"
          [disabled]="list().page() + 1 >= list().pageCount()"
          (click)="list().setPage(list().page() + 1)"
          [attr.aria-label]="'General.NextPage' | translate"
        >
          <app-icon name="chevronEnd" [mirror]="true" />
        </button>
      </div>
    </nav>
    }
  `,
  styles: `
    :host { display: block; }
    .table-scroll { overflow-x: auto; -webkit-overflow-scrolling: touch; }
    .skeleton-line { display: block; height: 12px; border-radius: 6px; width: 70%; background: var(--surface-sunken); animation: pulse 1.2s ease-in-out infinite; }
    .row-enter { animation: row-in var(--motion-base) var(--ease-out) both; }
    .pager { display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: 8px; padding-block-start: 12px; color: var(--text-secondary); font-size: var(--fs-sm); }
    .pager-actions { display: flex; align-items: center; gap: 6px; }
    .page-size { display: inline-flex; align-items: center; gap: 6px; margin-inline-end: 8px; }
    .page-num { min-width: 4em; text-align: center; font-variant-numeric: tabular-nums; }
    @keyframes pulse { 50% { opacity: .45; } }
    @keyframes row-in { from { opacity: 0; transform: translateY(4px); } }
    @media (prefers-reduced-motion: reduce) { .row-enter { animation: none; } .skeleton-line { animation: none; } }
  `,
})
export class PagedTableComponent<T> {
  private readonly l10n = inject(LocalizationService);

  readonly list = input.required<ListFilterService<T, BaseFilterDto>>();
  readonly columns = input.required<TableColumn[]>();
  readonly emptyTitle = input('General.NoItems');
  readonly emptyDescription = input('');
  readonly rowTpl = contentChild(TableRowDirective);

  readonly skeletonRows = [0, 1, 2, 3, 4];
  readonly pageSizes = [10, 25, 50];

  readonly rangeParams = computed(() => {
    const list = this.list();
    const from = list.page() * list.pageSize() + 1;
    const to = Math.min(list.totalCount(), from + list.items().length - 1);
    return {
      from: this.l10n.formatNumber(from),
      to: this.l10n.formatNumber(to),
      total: this.l10n.formatNumber(list.totalCount()),
    };
  });

  trackKey(row: unknown, index: number): unknown {
    return (row as { id?: unknown })?.id ?? index;
  }
}
