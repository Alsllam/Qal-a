import { BaseFilterDto, PagedRequestDto } from '@qala-fe/Core';

export type MatchStatus = 'Active' | 'Finished' | 'Aborted';
export type MatchSide = 'south' | 'north';

/** Rules end reasons (game_core `EndReason`) plus online-only reasons. */
export type EndReason =
  | 'amirCaptured'
  | 'qalaTaken'
  | 'noLegalMoves'
  | 'waterVictory'
  | 'plyLimitWater'
  | 'plyLimitWells'
  | 'plyLimitMaterial'
  | 'plyLimitDraw'
  | 'timeout'
  | 'resign'
  | 'abandon';

export const END_REASONS: EndReason[] = [
  'waterVictory',
  'plyLimitWater',
  'amirCaptured',
  'qalaTaken',
  'plyLimitWells',
  'plyLimitMaterial',
  'plyLimitDraw',
  'noLegalMoves',
  'timeout',
  'resign',
  'abandon',
];

export interface MatchPlayerDto {
  playerId: string;
  displayName: string;
  rating: number;
}

export interface MatchClocksDto {
  southMs: number;
  northMs: number;
  incrementMs: number;
}

export interface MatchOutcomeDto {
  winner: MatchSide | null;
  reason: EndReason;
}

export interface MatchDto {
  id: string;
  rulesVersion: string;
  status: MatchStatus;
  south: MatchPlayerDto;
  north: MatchPlayerDto;
  /** Current position, notation incl. water: `<ranks> <s|n> <ply> <south>:<north>`. */
  position: string;
  moves: string[];
  clocks: MatchClocksDto;
  outcome: MatchOutcomeDto | null;
  startedAt?: string;
  finishedAt?: string;
}

export interface MatchListDto {
  id: string;
  rulesVersion: string;
  status: MatchStatus;
  south: MatchPlayerDto;
  north: MatchPlayerDto;
  plies: number;
  outcome: MatchOutcomeDto | null;
  timeControl: string;
  startedAt: string;
  finishedAt?: string;
}

export interface FilterMatchDto extends BaseFilterDto {
  status?: MatchStatus;
  rulesVersion?: string;
  playerId?: string;
  from?: string;
  to?: string;
}

export type MyMatchesFilterDto = PagedRequestDto & { status?: MatchStatus };

export interface QueueRequestDto {
  /** e.g. `4+2` (minutes + seconds increment). */
  timeControl: string;
}
export interface QueueTicketDto {
  ticketId: string;
}
export interface ChallengeCodeDto {
  code: string;
}

export interface BalanceStatsRequestDto {
  from: string;
  to: string;
  rulesVersion?: string;
}

/**
 * Aggregates of finished matches (same metrics as the balance lab).
 * Shape proposed by the frontend; docs/architecture.md §5 lists the content.
 */
export interface BalanceStatsDto {
  rulesVersion?: string;
  from: string;
  to: string;
  games: number;
  southWins: number;
  northWins: number;
  draws: number;
  /** (South wins + draws / 2) / games, 0..1. */
  southScore: number;
  meanPlies: number;
  medianPlies: number;
  endReasons: { reason: EndReason; count: number }[];
  /** Game length histogram, buckets of `[fromPly, toPly]` inclusive. */
  lengthHistogram: { fromPly: number; toPly: number; count: number }[];
  byDay: { date: string; games: number; southScore: number; draws: number }[];
}
