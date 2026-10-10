import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import {
  EnArPipe,
  LocalizationService,
  PermissionDirective,
  Permissions,
} from '@qala-fe/Core';
import { PlayerListDto, PlayersService } from '@qala-fe/PlayersProxy';
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
import { PlayersListFilterComponent } from './players-list-filter.component';

@Component({
  selector: 'app-players-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageHeaderComponent,
    PagedTableComponent,
    TableRowDirective,
    PlayersListFilterComponent,
    PermissionDirective,
    TranslatePipe,
    EnArPipe,
    IconComponent,
  ],
  templateUrl: './players-list.component.html',
  styles: `
    .player-name { font-weight: 600; }
    .small { font-size: var(--fs-xs); }
  `,
  providers: [
    {
      provide: ListFilterService,
      useFactory: () =>
        new ListFilterService(inject(PlayersService).getList, {
          stateKey: 'players',
        }),
    },
  ],
})
export class PlayersListComponent extends AbstractListComponent<PlayerListDto> {
  private readonly players = inject(PlayersService);
  readonly l10n = inject(LocalizationService);
  readonly permissions = Permissions;

  readonly columns: TableColumn[] = [
    { key: 'Players.DisplayName' },
    { key: 'Players.Rating', align: 'end' },
    { key: 'Players.Games', align: 'end', hideOnMobile: true },
    { key: 'Players.Record', align: 'end', hideOnMobile: true },
    { key: 'Players.Status' },
    { key: 'Players.Joined', hideOnMobile: true },
    { key: 'General.Actions', align: 'end', width: '1%' },
  ];

  row(item: unknown): PlayerListDto {
    return item as PlayerListDto;
  }

  ban(player: PlayerListDto): void {
    this.confirmAndRun(
      'Players.BanConfirm',
      () => this.players.deactivate(player.id),
      {
        severity: 'error',
        params: { name: player.displayName },
        confirmKey: 'Players.Ban',
      }
    );
  }

  unban(player: PlayerListDto): void {
    this.confirmAndRun(
      'Players.UnbanConfirm',
      () => this.players.activate(player.id),
      {
        params: { name: player.displayName },
        confirmKey: 'Players.Unban',
      }
    );
  }
}
