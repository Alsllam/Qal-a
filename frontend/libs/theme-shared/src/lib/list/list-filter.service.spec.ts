import { Injector, runInInjectionContext } from '@angular/core';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { BaseFilterDto, PagedResultDto } from '@qala-fe/Core';
import { Observable, of } from 'rxjs';
import { ListFilterService } from './list-filter.service';

interface Filter extends BaseFilterDto {
  status?: string;
}

describe('ListFilterService', () => {
  let calls: Filter[];
  let fetch: (f: Filter) => Observable<PagedResultDto<{ id: number }>>;

  function create(): ListFilterService<{ id: number }, Filter> {
    return runInInjectionContext(
      TestBed.inject(Injector),
      () => new ListFilterService(fetch, { pageSize: 10 })
    );
  }

  beforeEach(() => {
    calls = [];
    fetch = (f) => {
      calls.push(f);
      return of({ items: [{ id: 1 }, { id: 2 }], totalCount: 42 });
    };
  });

  it('loads the first page on creation', fakeAsync(() => {
    const list = create();
    tick();
    expect(calls).toEqual([{ skipCount: 0, maxResultCount: 10 }]);
    expect(list.items()).toHaveLength(2);
    expect(list.totalCount()).toBe(42);
    expect(list.pageCount()).toBe(5);
    expect(list.loading()).toBe(false);
  }));

  it('debounces search text by 300 ms and resets to the first page', fakeAsync(() => {
    const list = create();
    tick();
    list.setPage(2);
    tick();
    list.setFilterText('a');
    list.setFilterText('al');
    list.setFilterText('ali');
    tick(299);
    expect(calls).toHaveLength(2);
    tick(1);
    expect(calls[2]).toEqual({
      filterText: 'ali',
      skipCount: 0,
      maxResultCount: 10,
    });
  }));

  it('applies extra filters immediately and drops empty values', fakeAsync(() => {
    const list = create();
    tick();
    list.patchFilter({ status: 'Finished', activeFilter: undefined });
    tick();
    expect(calls[1]).toEqual({
      status: 'Finished',
      skipCount: 0,
      maxResultCount: 10,
    });
    expect(list.hasFilter()).toBe(true);
    expect(list.lastQuery).toEqual(calls[1]);
    list.clearFilters();
    tick();
    expect(list.hasFilter()).toBe(false);
  }));

  it('steps back a page when the last row of a page is removed', fakeAsync(() => {
    fetch = (f) => {
      calls.push(f);
      return of({ items: [{ id: 1 }], totalCount: 21 });
    };
    const list = create();
    tick();
    list.setPage(2);
    tick();
    list.refreshAfterRemoval();
    tick();
    expect(calls[calls.length - 1].skipCount).toBe(10);
  }));
});
