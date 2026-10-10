import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EnArPipe, LocalizationService } from '@qala-fe/Core';
import { MatchListDto, MatchesService } from '@qala-fe/MatchesProxy';
import {
  AbstractListComponent,
  IconComponent,
  ListFilterService,
  PageHeaderComponent,
  PagedTableComponent,
  TableColumn,
  TableRowDirective,
} from '@qala-fe/theme-shared';
import { TranslatePipe } from '@ngx-translate/core';
import { resultKey, statusTone } from './match-display';
import { MatchesListFilterComponent } from './matches-list-filter.component';

@Component({
  selector: 'app-matches-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageHeaderComponent,
    PagedTableComponent,
    TableRowDirective,
    MatchesListFilterComponent,
    RouterLink,
    TranslatePipe,
    EnArPipe,
    IconComponent,
  ],
  templateUrl: './matches-list.component.html',
  styles: `
    .players { display: flex; flex-direction: column; gap: 2px; }
    .side { display: inline-flex; align-items: center; gap: 6px; }
    .dot { width: 10px; height: 10px; border-radius: 50%; flex: none; border: 1.5px solid; }
    .dot.south { background: var(--game-south-fill); border-color: var(--game-south-edge); }
    .dot.north { background: var(--game-north-fill); border-color: var(--game-north-edge); }
    .reason { display: block; font-size: var(--fs-xs); color: var(--text-muted); }
  `,
  providers: [
    {
      provide: ListFilterService,
      useFactory: () =>
        new ListFilterService(inject(MatchesService).getList, {
          stateKey: 'matches',
        }),
    },
  ],
})
export class MatchesListComponent extends AbstractListComponent<MatchListDto> {
  readonly l10n = inject(LocalizationService);
  readonly statusTone = statusTone;
  readonly resultKey = resultKey;

  readonly columns: TableColumn[] = [
    { key: 'Matches.Players' },
    { key: 'Matches.Result' },
    { key: 'Matches.Plies', align: 'end', hideOnMobile: true },
    { key: 'General.RulesVersion', hideOnMobile: true },
    { key: 'Matches.Status' },
    { key: 'Matches.Started', hideOnMobile: true },
    { key: 'General.Actions', align: 'end', width: '1%' },
  ];

  row(item: unknown): MatchListDto {
    return item as MatchListDto;
  }
}
