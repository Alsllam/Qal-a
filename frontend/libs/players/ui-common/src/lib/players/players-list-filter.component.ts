import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActiveFilter, EnArPipe } from '@qala-fe/Core';
import { FilterPlayerDto, PlayerListDto } from '@qala-fe/PlayersProxy';
import { IconComponent, ListFilterService } from '@qala-fe/theme-shared';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-players-list-filter',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslatePipe, IconComponent, EnArPipe],
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
            [placeholder]="'Players.SearchPlaceholder' | translate"
          />
        </span>
      </label>
      <label>
        <span class="form-label">{{ 'Players.Status' | translate }}</span>
        <select
          class="form-select"
          (change)="setStatus($any($event.target).value)"
        >
          <option value="All" [selected]="!list.extra().activeFilter">
            {{ 'General.All' | translate }}
          </option>
          <option
            value="Active"
            [selected]="list.extra().activeFilter === 'Active'"
          >
            {{ 'Players.Active' | translate }}
          </option>
          <option
            value="InActive"
            [selected]="list.extra().activeFilter === 'InActive'"
          >
            {{ 'Players.Banned' | translate }}
          </option>
        </select>
      </label>
      <label>
        <span class="form-label">{{ 'Players.MinRating' | translate }}</span>
        <select
          class="form-select"
          (change)="setMinRating($any($event.target).value)"
        >
          <option value="" [selected]="!list.extra().minRating">
            {{ 'General.Any' | translate }}
          </option>
          @for (r of ratings; track r) {
          <option [value]="r" [selected]="list.extra().minRating === r">
            {{ r | enar }}+
          </option>
          }
        </select>
      </label>
    </div>
  `,
})
export class PlayersListFilterComponent {
  readonly list = inject(ListFilterService) as ListFilterService<
    PlayerListDto,
    FilterPlayerDto
  >;
  readonly ratings = [1300, 1500, 1700, 1900];

  setStatus(value: ActiveFilter): void {
    this.list.patchFilter({
      activeFilter: value === 'All' ? undefined : value,
    });
  }

  setMinRating(value: string): void {
    this.list.patchFilter({ minRating: value ? Number(value) : undefined });
  }
}
