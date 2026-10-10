import {
  HttpErrorResponse,
  HttpEvent,
  HttpRequest,
  HttpResponse,
} from '@angular/common/http';
import { MockApiHandler, PagedResultDto } from '@qala-fe/Core';
import {
  BalanceStatsRequestDto,
  FilterMatchDto,
  MatchListDto,
} from '@qala-fe/MatchesProxy';
import { FilterPlayerDto, PlayerListDto } from '@qala-fe/PlayersProxy';
import { Observable, delay, of, throwError, timer, mergeMap } from 'rxjs';
import {
  createMatches,
  createPlayers,
  createStats,
  rng,
  toListDto,
} from './mock-data';

/**
 * Dev-only in-memory backend for `nx serve admin` without the .NET services.
 * Mirrors docs/architecture.md §5 (reads are POST with a body).
 */
export function createMockApiHandler(): MockApiHandler {
  const players = createPlayers();
  const matches = createMatches(players);
  const latency = rng(7);

  const ok = <T>(body: T): Observable<HttpEvent<unknown>> =>
    of(new HttpResponse<unknown>({ status: 200, body })).pipe(
      delay(220 + Math.floor(latency() * 260))
    );

  const fail = (
    status: number,
    message: string,
    url: string
  ): Observable<HttpEvent<unknown>> =>
    timer(200).pipe(
      mergeMap(() =>
        throwError(
          () =>
            new HttpErrorResponse({
              status,
              url,
              error: { error: { code: String(status), messages: [message] } },
            })
        )
      )
    );

  function page<T>(items: T[], skip: number, take: number): PagedResultDto<T> {
    return { items: items.slice(skip, skip + take), totalCount: items.length };
  }

  return (req: HttpRequest<unknown>) => {
    const path = new URL(req.url, 'http://mock').pathname;
    const body = (req.body ?? {}) as Record<string, unknown>;

    // ---- players-api ----------------------------------------------------
    if (path.endsWith('/players/list') && req.method === 'POST') {
      const f = body as unknown as FilterPlayerDto;
      const text = (f.filterText ?? '').trim().toLowerCase();
      const items: PlayerListDto[] = players.filter(
        (p) =>
          (!text ||
            p.displayName.toLowerCase().includes(text) ||
            (p.email ?? '').includes(text)) &&
          (!f.activeFilter ||
            f.activeFilter === 'All' ||
            (f.activeFilter === 'Active') === p.isActive) &&
          (f.minRating === undefined || p.rating >= f.minRating) &&
          (f.maxRating === undefined || p.rating <= f.maxRating)
      );
      return ok(page(items, f.skipCount ?? 0, f.maxResultCount ?? 10));
    }
    if (path.endsWith('/players/getbyid')) {
      const p = players.find((x) => x.id === body['id']);
      return p ? ok(p) : fail(404, 'Player not found', req.url);
    }
    if (
      path.endsWith('/players/activate') ||
      path.endsWith('/players/deactivate')
    ) {
      const p = players.find((x) => x.id === body['id']);
      if (!p) return fail(404, 'Player not found', req.url);
      p.isActive = path.endsWith('/activate');
      return ok(null);
    }
    if (path.endsWith('/players/me')) {
      return ok({
        id: 'p-dev',
        displayName: 'Dev Admin',
        locale: 'ar',
        rating: 1500,
        ratingDeviation: 350,
      });
    }
    if (path.endsWith('/players/leaderboard')) {
      const ranked = players
        .filter((p) => p.isActive)
        .map((p, i) => ({
          rank: i + 1,
          displayName: p.displayName,
          rating: p.rating,
        }));
      return ok(
        page(
          ranked,
          Number(body['skipCount'] ?? 0),
          Number(body['maxResultCount'] ?? 10)
        )
      );
    }

    // ---- matches-api ----------------------------------------------------
    if (
      (path.endsWith('/matches/list') || path.endsWith('/matches/mine')) &&
      req.method === 'POST'
    ) {
      const f = body as unknown as FilterMatchDto;
      const text = (f.filterText ?? '').trim().toLowerCase();
      const items: MatchListDto[] = matches
        .filter(
          (m) =>
            (!text ||
              m.id.includes(text) ||
              m.south.displayName.toLowerCase().includes(text) ||
              m.north.displayName.toLowerCase().includes(text)) &&
            (!f.status || m.status === f.status) &&
            (!f.rulesVersion || m.rulesVersion === f.rulesVersion) &&
            (!f.playerId ||
              m.south.playerId === f.playerId ||
              m.north.playerId === f.playerId)
        )
        .map(toListDto);
      return ok(page(items, f.skipCount ?? 0, f.maxResultCount ?? 10));
    }
    if (path.endsWith('/matches/getbyid')) {
      const m = matches.find((x) => x.id === body['id']);
      return m ? ok(m) : fail(404, 'Match not found', req.url);
    }
    if (path.endsWith('/matches/stats')) {
      const f = body as unknown as BalanceStatsRequestDto;
      return ok(createStats(f.from, f.to, f.rulesVersion ?? '0.6'));
    }

    // i18n files and anything else go to the network.
    if (path.includes('-api/'))
      return fail(
        501,
        `Mock API: ${req.method} ${path} is not implemented`,
        req.url
      );
    return null;
  };
}
