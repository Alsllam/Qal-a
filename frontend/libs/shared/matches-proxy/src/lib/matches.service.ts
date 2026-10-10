import { Injectable, inject } from '@angular/core';
import {
  EntityIdDto,
  PagedResultDto,
  RestConfig,
  RestService,
} from '@qala-fe/Core';
import {
  BalanceStatsDto,
  BalanceStatsRequestDto,
  ChallengeCodeDto,
  FilterMatchDto,
  MatchDto,
  MatchListDto,
  MyMatchesFilterDto,
  QueueRequestDto,
  QueueTicketDto,
} from './models/match.model';

/** `/matches-api/matches` (docs/architecture.md §5). Reads are POST with a body. */
@Injectable({ providedIn: 'root' })
export class MatchesService {
  private readonly rest = inject(RestService);
  readonly apiName = 'matches';

  joinQueue = (input: QueueRequestDto, config?: RestConfig) =>
    this.rest.request<QueueRequestDto, QueueTicketDto>(
      { method: 'POST', url: '/matches/queue', body: input },
      { apiName: this.apiName, ...config }
    );

  leaveQueue = (ticketId: string, config?: RestConfig) =>
    this.rest.request<QueueTicketDto, void>(
      { method: 'DELETE', url: '/matches/queue', body: { ticketId } },
      { apiName: this.apiName, ...config }
    );

  createChallenge = (input: QueueRequestDto, config?: RestConfig) =>
    this.rest.request<QueueRequestDto, ChallengeCodeDto>(
      { method: 'POST', url: '/matches/challenge', body: input },
      { apiName: this.apiName, ...config }
    );

  acceptChallenge = (code: string, config?: RestConfig) =>
    this.rest.request<ChallengeCodeDto, MatchDto>(
      { method: 'POST', url: '/matches/challenge/accept', body: { code } },
      { apiName: this.apiName, ...config }
    );

  get = (id: string, config?: RestConfig) =>
    this.rest.request<EntityIdDto, MatchDto>(
      { method: 'POST', url: '/matches/getbyid', body: { id } },
      { apiName: this.apiName, ...config }
    );

  getMine = (input: MyMatchesFilterDto, config?: RestConfig) =>
    this.rest.request<MyMatchesFilterDto, PagedResultDto<MatchListDto>>(
      { method: 'POST', url: '/matches/mine', body: input },
      { apiName: this.apiName, ...config }
    );

  getList = (input: FilterMatchDto, config?: RestConfig) =>
    this.rest.request<FilterMatchDto, PagedResultDto<MatchListDto>>(
      { method: 'POST', url: '/matches/list', body: input },
      { apiName: this.apiName, ...config }
    );

  getStats = (input: BalanceStatsRequestDto, config?: RestConfig) =>
    this.rest.request<BalanceStatsRequestDto, BalanceStatsDto>(
      { method: 'POST', url: '/matches/stats', body: input },
      { apiName: this.apiName, ...config }
    );
}
