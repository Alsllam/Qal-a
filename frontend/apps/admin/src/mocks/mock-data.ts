/**
 * Deterministic, realistic demo data for the dev mock API.
 * Never shipped in production builds (see main.ts / environment.allowMockApi).
 */
import { INITIAL_POSITION, replay, toNotation } from '@qala-fe/Board';
import {
  BalanceStatsDto,
  EndReason,
  MatchDto,
  MatchListDto,
  MatchStatus,
} from '@qala-fe/MatchesProxy';
import { PlayerDto } from '@qala-fe/PlayersProxy';
import { SAMPLE_GAMES } from './sample-games';

export function rng(seed: number): () => number {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6d2b79f5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

export function hash(text: string): number {
  let h = 2166136261;
  for (let i = 0; i < text.length; i++)
    h = Math.imul(h ^ text.charCodeAt(i), 16777619);
  return h >>> 0;
}

function gaussian(r: () => number): number {
  return Math.sqrt(-2 * Math.log(1 - r())) * Math.cos(2 * Math.PI * r());
}

const AR_FIRST = [
  'سارة',
  'يوسف',
  'ليلى',
  'عمر',
  'فهد',
  'نورة',
  'خالد',
  'مريم',
  'أحمد',
  'هند',
  'سلمان',
  'ريم',
  'طارق',
  'جود',
  'ماجد',
  'لمى',
  'بدر',
  'دانة',
  'زياد',
  'شهد',
];
const AR_LAST = [
  'القحطاني',
  'الحربي',
  'العتيبي',
  'الشمري',
  'الزهراني',
  'المطيري',
  'الدوسري',
  'السبيعي',
  'الغامدي',
  'العنزي',
];
const EN_HANDLES = [
  'desert_fox',
  'WellKeeper',
  'qala.master',
  'Rami_Shot',
  'FarisRider',
  'sandstorm',
  'NightIndigo',
  'saffron.k',
  'oasis_hunter',
  'the_amir',
  'walls&wells',
  'MinaretMove',
  'dune.runner',
  'Jundi_77',
  'tower_up',
];
const EN_FIRST = [
  'Omar',
  'Layla',
  'Sami',
  'Nora',
  'Adam',
  'Hana',
  'Karim',
  'Dina',
  'Yara',
  'Faisal',
];

const DAY = 86_400_000;
const NOW = Date.now();

export function createPlayers(count = 137): PlayerDto[] {
  const r = rng(20261010);
  const players: PlayerDto[] = [];
  for (let i = 0; i < count; i++) {
    const kind = r();
    let displayName: string;
    let locale: 'ar' | 'en';
    if (kind < 0.5) {
      displayName = `${AR_FIRST[Math.floor(r() * AR_FIRST.length)]} ${
        AR_LAST[Math.floor(r() * AR_LAST.length)]
      }`;
      locale = 'ar';
    } else if (kind < 0.8) {
      displayName = `${EN_HANDLES[Math.floor(r() * EN_HANDLES.length)]}${
        r() < 0.5 ? Math.floor(r() * 99) : ''
      }`;
      locale = r() < 0.6 ? 'ar' : 'en';
    } else {
      displayName = `${
        EN_FIRST[Math.floor(r() * EN_FIRST.length)]
      } ${String.fromCharCode(65 + Math.floor(r() * 26))}.`;
      locale = 'en';
    }
    const games = Math.floor(Math.pow(r(), 1.6) * 420);
    const rating = Math.round(
      1500 + gaussian(r) * 170 + Math.min(games, 200) * 0.4
    );
    const rd = Math.round(Math.max(45, 350 - games * 2.4 + r() * 20));
    const draws = Math.round(games * 0.004 * r() * 2);
    const winShare = Math.min(
      0.85,
      Math.max(0.15, 0.5 + (rating - 1500) / 900)
    );
    const wins = Math.round((games - draws) * winShare);
    const created = NOW - Math.floor(r() * 120 + 1) * DAY;
    players.push({
      id: `p-${(i + 1).toString().padStart(4, '0')}`,
      displayName,
      email: r() < 0.7 ? `player${i + 1}@example.com` : undefined,
      rating,
      ratingDeviation: rd,
      gamesPlayed: games,
      wins,
      draws,
      losses: games - draws - wins,
      isActive: r() > 0.06,
      locale,
      creationTime: new Date(created).toISOString(),
      lastSeenTime: new Date(
        created + Math.floor(r() * (NOW - created))
      ).toISOString(),
      volatility: 0.06,
    });
  }
  return players.sort((a, b) => b.rating - a.rating);
}

export function createMatches(players: PlayerDto[], count = 240): MatchDto[] {
  const r = rng(4242);
  const matches: MatchDto[] = [];
  for (let i = 0; i < count; i++) {
    const south = players[Math.floor(r() * players.length)];
    let north = players[Math.floor(r() * players.length)];
    if (north.id === south.id)
      north = players[(players.indexOf(south) + 1) % players.length];
    const game = SAMPLE_GAMES[i % SAMPLE_GAMES.length];
    const started = NOW - Math.floor(r() * 30 * DAY) - 5 * 60_000;
    const roll = r();
    let status: MatchStatus = 'Finished';
    let moves = game.moves;
    let position = game.final;
    let outcome: MatchDto['outcome'] = {
      winner: game.winner,
      reason: game.reason as EndReason,
    };
    if (i < 6) {
      status = 'Active';
      moves = game.moves.slice(0, 8 + Math.floor(r() * 24));
      outcome = null;
    } else if (roll < 0.035) {
      status = 'Aborted';
      moves = game.moves.slice(0, 1 + Math.floor(r() * 6));
      outcome = { winner: null, reason: 'abandon' };
    } else if (roll < 0.14) {
      moves = game.moves.slice(0, 12 + Math.floor(r() * 20));
      const loser = moves.length % 2 === 0 ? 'south' : 'north';
      outcome = {
        winner: loser === 'south' ? 'north' : 'south',
        reason: roll < 0.1 ? 'resign' : 'timeout',
      };
    }
    if (moves !== game.moves)
      position = toNotation(replay(moves, INITIAL_POSITION)[moves.length]);
    const durationMs = moves.length * (6_000 + Math.floor(r() * 7_000));
    matches.push({
      id: `m-${(9000 - i).toString(36)}${Math.floor(r() * 1e6).toString(36)}`,
      rulesVersion: r() < 0.9 ? '0.6' : '0.7',
      status,
      south: {
        playerId: south.id,
        displayName: south.displayName,
        rating: south.rating,
      },
      north: {
        playerId: north.id,
        displayName: north.displayName,
        rating: north.rating,
      },
      position,
      moves,
      clocks: {
        southMs: Math.max(0, 240_000 - Math.floor(r() * 200_000)),
        northMs: Math.max(0, 240_000 - Math.floor(r() * 200_000)),
        incrementMs: 2000,
      },
      outcome,
      startedAt: new Date(started).toISOString(),
      finishedAt:
        status === 'Active'
          ? undefined
          : new Date(started + durationMs).toISOString(),
    });
  }
  return matches.sort((a, b) =>
    (b.startedAt ?? '').localeCompare(a.startedAt ?? '')
  );
}

export function toListDto(m: MatchDto): MatchListDto {
  return {
    id: m.id,
    rulesVersion: m.rulesVersion,
    status: m.status,
    south: m.south,
    north: m.north,
    plies: m.moves.length,
    outcome: m.outcome,
    timeControl: `${m.clocks.incrementMs ? '4+2' : '4+0'}`,
    startedAt: m.startedAt ?? '',
    finishedAt: m.finishedAt,
  };
}

/** Human end-reason mix (online play adds resign / timeout / abandon). */
const REASON_MIX: [EndReason, number][] = [
  ['waterVictory', 0.418],
  ['plyLimitWater', 0.168],
  ['amirCaptured', 0.162],
  ['qalaTaken', 0.091],
  ['resign', 0.083],
  ['timeout', 0.046],
  ['plyLimitMaterial', 0.012],
  ['plyLimitWells', 0.01],
  ['abandon', 0.006],
  ['plyLimitDraw', 0.004],
];

export function createStats(
  from: string,
  to: string,
  rulesVersion = '0.6'
): BalanceStatsDto {
  const start = Date.parse(from);
  const end = Date.parse(to);
  const byDay: BalanceStatsDto['byDay'] = [];
  let games = 0;
  let southPoints = 0;
  let draws = 0;
  const base = rulesVersion === '0.7' ? 9 : 64;
  for (let t = start, d = 0; t <= end; t += DAY, d++) {
    const date = new Date(t).toISOString().slice(0, 10);
    const r = rng(hash(`${rulesVersion}:${date}`));
    const weekend = [4, 5].includes(new Date(t).getUTCDay()) ? 1.35 : 1; // Thu/Fri evenings
    const trend = 1 + ((t - (NOW - 90 * DAY)) / (90 * DAY)) * 0.35;
    const n = Math.max(
      1,
      Math.round(base * weekend * trend * (0.8 + r() * 0.4))
    );
    const score = Math.min(
      0.75,
      Math.max(
        0.3,
        (rulesVersion === '0.7' ? 0.515 : 0.532) +
          gaussian(r) * (0.35 / Math.sqrt(n))
      )
    );
    const dayDraws = r() < n * 0.004 ? 1 : 0;
    byDay.push({
      date,
      games: n,
      southScore: Number(score.toFixed(3)),
      draws: dayDraws,
    });
    games += n;
    southPoints += score * n;
    draws += dayDraws;
  }
  const southScore = games ? southPoints / games : 0;
  const southWins = Math.round(southScore * games - draws / 2);
  const r = rng(hash(`${rulesVersion}:${from}:${to}`));
  let remaining = games - draws;
  const endReasons = REASON_MIX.filter(
    ([reason]) => reason !== 'plyLimitDraw'
  ).map(([reason, share], i, arr) => {
    const count =
      i === arr.length - 1
        ? remaining
        : Math.round((games - draws) * share * (0.94 + r() * 0.12));
    remaining -= count;
    return { reason, count: Math.max(0, count) };
  });
  endReasons.push({ reason: 'plyLimitDraw', count: draws });

  const plyLimitShare =
    (endReasons.find((e) => e.reason.startsWith('plyLimit'))?.count ?? 0) /
    Math.max(1, games);
  const buckets: [number, number, number][] = [
    [1, 9, 0.012],
    [10, 19, 0.048],
    [20, 29, 0.112],
    [30, 39, 0.214],
    [40, 49, 0.248],
    [50, 59, 0.156],
  ];
  const lengthHistogram = buckets.map(([fromPly, toPly, share]) => ({
    fromPly,
    toPly,
    count: Math.round(games * share * (0.95 + r() * 0.1)),
  }));
  const sixty = Math.max(
    0,
    games - lengthHistogram.reduce((s, b) => s + b.count, 0)
  );
  lengthHistogram.push({ fromPly: 60, toPly: 60, count: sixty });

  return {
    rulesVersion,
    from,
    to,
    games,
    southWins,
    northWins: games - draws - southWins,
    draws,
    southScore: Number(southScore.toFixed(4)),
    meanPlies: Number(
      (rulesVersion === '0.7' ? 42.8 : 43.6 + plyLimitShare).toFixed(1)
    ),
    medianPlies: rulesVersion === '0.7' ? 43 : 44,
    endReasons,
    lengthHistogram,
    byDay,
  };
}
