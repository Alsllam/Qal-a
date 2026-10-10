import { DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BaseFilterDto, PagedResultDto } from '@qala-fe/Core';
import {
  Observable,
  Subject,
  catchError,
  debounce,
  of,
  switchMap,
  tap,
  timer,
} from 'rxjs';

export type ListFetch<TItem, TFilter extends BaseFilterDto> = (
  input: TFilter
) => Observable<PagedResultDto<TItem>>;

export interface ListFilterOptions {
  pageSize?: number;
  /** Saves filters and page per screen (session storage) so they survive navigation. */
  stateKey?: string;
  /** Search debounce in ms (default 300). */
  debounceMs?: number;
}

type ExtraFilter<TFilter> = Omit<TFilter, keyof BaseFilterDto> &
  Partial<Pick<BaseFilterDto, 'activeFilter' | 'sorting'>>;

interface SavedState<TFilter> {
  filterText: string;
  extra: Partial<ExtraFilter<TFilter>>;
  page: number;
  pageSize: number;
}

/**
 * Paged list state for one screen. Provide it in the component:
 * `{ provide: ListFilterService, useFactory: () => new ListFilterService(inject(PlayersService).getList) }`.
 */
export class ListFilterService<
  TItem,
  TFilter extends BaseFilterDto = BaseFilterDto
> {
  readonly items = signal<TItem[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly page = signal(0);
  readonly pageSize = signal(10);
  readonly filterText = signal('');
  readonly extra = signal<Partial<ExtraFilter<TFilter>>>({});

  readonly pageCount = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize()))
  );
  readonly hasFilter = computed(
    () =>
      !!this.filterText().trim() ||
      Object.values(this.extra()).some(
        (v) => v !== undefined && v !== null && v !== '' && v !== 'All'
      )
  );

  /** The last query sent, e.g. for an export endpoint. */
  lastQuery: TFilter | null = null;

  private readonly trigger = new Subject<number>();
  private readonly stateKey?: string;

  constructor(
    private readonly fetch: ListFetch<TItem, TFilter>,
    options: ListFilterOptions = {}
  ) {
    this.stateKey = options.stateKey;
    if (options.pageSize) this.pageSize.set(options.pageSize);
    this.restore();
    const debounceMs = options.debounceMs ?? 300;

    this.trigger
      .pipe(
        debounce((delay) => timer(delay)),
        tap(() => {
          this.loading.set(true);
          this.error.set(false);
        }),
        switchMap(() => {
          const query = this.buildQuery();
          this.lastQuery = query;
          this.save();
          return this.fetch(query).pipe(
            catchError(() => {
              this.error.set(true);
              return of<PagedResultDto<TItem>>({ items: [], totalCount: 0 });
            })
          );
        }),
        takeUntilDestroyed(inject(DestroyRef))
      )
      .subscribe((result) => {
        this.items.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      });

    this.searchDelay = debounceMs;
    this.trigger.next(0);
  }

  private readonly searchDelay: number;

  setFilterText(text: string): void {
    this.filterText.set(text);
    this.page.set(0);
    this.trigger.next(this.searchDelay);
  }

  patchFilter(patch: Partial<ExtraFilter<TFilter>>): void {
    this.extra.update((current) => ({ ...current, ...patch }));
    this.page.set(0);
    this.trigger.next(0);
  }

  clearFilters(): void {
    this.filterText.set('');
    this.extra.set({});
    this.page.set(0);
    this.trigger.next(0);
  }

  setPage(page: number): void {
    this.page.set(Math.max(0, Math.min(page, this.pageCount() - 1)));
    this.trigger.next(0);
  }

  setPageSize(size: number): void {
    this.pageSize.set(size);
    this.page.set(0);
    this.trigger.next(0);
  }

  refresh(): void {
    this.trigger.next(0);
  }

  /** After deleting the last row of a page, step back one page. */
  refreshAfterRemoval(): void {
    if (this.items().length <= 1 && this.page() > 0)
      this.page.update((p) => p - 1);
    this.trigger.next(0);
  }

  private buildQuery(): TFilter {
    const extra = Object.fromEntries(
      Object.entries(this.extra()).filter(
        ([, v]) => v !== undefined && v !== null && v !== ''
      )
    );
    const text = this.filterText().trim();
    return {
      ...extra,
      ...(text ? { filterText: text } : {}),
      skipCount: this.page() * this.pageSize(),
      maxResultCount: this.pageSize(),
    } as TFilter;
  }

  private restore(): void {
    if (!this.stateKey) return;
    try {
      const raw = sessionStorage.getItem(`qala.list.${this.stateKey}`);
      if (!raw) return;
      const state = JSON.parse(raw) as SavedState<TFilter>;
      this.filterText.set(state.filterText ?? '');
      this.extra.set(state.extra ?? {});
      this.page.set(state.page ?? 0);
      this.pageSize.set(state.pageSize ?? this.pageSize());
    } catch {
      /* ignore */
    }
  }

  private save(): void {
    if (!this.stateKey) return;
    const state: SavedState<TFilter> = {
      filterText: this.filterText(),
      extra: this.extra(),
      page: this.page(),
      pageSize: this.pageSize(),
    };
    try {
      sessionStorage.setItem(
        `qala.list.${this.stateKey}`,
        JSON.stringify(state)
      );
    } catch {
      /* ignore */
    }
  }
}
