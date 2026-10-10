import { Injectable, inject } from '@angular/core';
import {
  EntityIdDto,
  PagedResultDto,
  RestConfig,
  RestService,
} from '@qala-fe/Core';
import {
  FilterPlayerDto,
  LeaderboardEntryDto,
  LeaderboardRequestDto,
  PlayerDto,
  PlayerListDto,
  PlayerProfileDto,
  UpdateMyProfileDto,
} from './models/player.model';

/** `/players-api/players` (docs/architecture.md §5). Reads are POST with a body. */
@Injectable({ providedIn: 'root' })
export class PlayersService {
  private readonly rest = inject(RestService);
  readonly apiName = 'players';

  getMe = (config?: RestConfig) =>
    this.rest.request<void, PlayerProfileDto>(
      { method: 'POST', url: '/players/me' },
      { apiName: this.apiName, ...config }
    );

  updateMe = (input: UpdateMyProfileDto, config?: RestConfig) =>
    this.rest.request<UpdateMyProfileDto, void>(
      { method: 'PUT', url: '/players/me', body: input },
      { apiName: this.apiName, ...config }
    );

  getList = (input: FilterPlayerDto, config?: RestConfig) =>
    this.rest.request<FilterPlayerDto, PagedResultDto<PlayerListDto>>(
      { method: 'POST', url: '/players/list', body: input },
      { apiName: this.apiName, ...config }
    );

  get = (id: string, config?: RestConfig) =>
    this.rest.request<EntityIdDto, PlayerDto>(
      { method: 'POST', url: '/players/getbyid', body: { id } },
      { apiName: this.apiName, ...config }
    );

  getLeaderboard = (input: LeaderboardRequestDto, config?: RestConfig) =>
    this.rest.request<
      LeaderboardRequestDto,
      PagedResultDto<LeaderboardEntryDto>
    >(
      { method: 'POST', url: '/players/leaderboard', body: input },
      { apiName: this.apiName, ...config }
    );

  /** Unban. */
  activate = (id: string, config?: RestConfig) =>
    this.rest.request<EntityIdDto, void>(
      { method: 'POST', url: '/players/activate', body: { id } },
      { apiName: this.apiName, ...config }
    );

  /** Ban (publishes `PlayerBannedEto`, which aborts the player's active games). */
  deactivate = (id: string, config?: RestConfig) =>
    this.rest.request<EntityIdDto, void>(
      { method: 'POST', url: '/players/deactivate', body: { id } },
      { apiName: this.apiName, ...config }
    );
}
