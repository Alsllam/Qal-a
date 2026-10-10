import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import {
  FilterMatchDto,
  MatchListDto,
  MatchStatus,
} from '@qala-fe/MatchesProxy';
import { IconComponent, ListFilterService } from '@qala-fe/theme-shared';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-matches-list-filter',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslatePipe, IconComponent],
  template: `
    <div class="filter-bar" role="search">
      <label class="grow">
        <span class="form-label">{{ 'General.Search' | translate }}</span>
        <span class="search-field">
          <app-icon name="search" [size]="18" />
          <input
            type="search"
            class="form-control"
            [value]="list.filterText()"
            (input)="list.setFilterText($any($event.target).value)"
            [placeholder]="'Matches.SearchPlaceholder' | translate"
          />
        </span>
      </label>
      <label>
        <span class="form-label">{{ 'Matches.Status' | translate }}</span>
        <select
          class="form-select"
          (change)="setStatus($any($event.target).value)"
        >
          <option value="" [selected]="!list.extra().status">
            {{ 'General.All' | translate }}
          </option>
          @for (s of statuses; track s) {
          <option [value]="s" [selected]="list.extra().status === s">
            {{ 'Matches.Status' + s | translate }}
          </option>
          }
        </select>
      </label>
      <label>
        <span class="form-label">{{ 'General.RulesVersion' | translate }}</span>
        <select
          class="form-select"
          (change)="setVersion($any($event.target).value)"
        >
          <option value="" [selected]="!list.extra().rulesVersion">
            {{ 'General.All' | translate }}
          </option>
          @for (v of versions; track v) {
          <option [value]="v" [selected]="list.extra().rulesVersion === v">
            v{{ v }}
          </option>
          }
        </select>
      </label>
    </div>
  `,
})
export class MatchesListFilterComponent {
  readonly list = inject(ListFilterService) as ListFilterService<
    MatchListDto,
    FilterMatchDto
  >;
  readonly statuses: MatchStatus[] = ['Active', 'Finished', 'Aborted'];
  readonly versions = ['0.6', '0.7'];

  setStatus(value: string): void {
    this.list.patchFilter({
      status: (value || undefined) as MatchStatus | undefined,
    });
  }

  setVersion(value: string): void {
    this.list.patchFilter({ rulesVersion: value || undefined });
  }
}
