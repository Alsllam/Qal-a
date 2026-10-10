import { EndReason } from '@qala-fe/MatchesProxy';

/**
 * AI self-play baseline per rules version, from docs/balance-log.md
 * (2,000 games at depth 3, random first 2 plies). Update when a new
 * version is adopted.
 */
export interface AiBaseline {
  rulesVersion: string;
  southScore: number;
  drawRate: number;
  meanPlies: number;
  /** Share of games per end-reason group, 0..1. */
  endReasons: Record<EndReasonGroup, number>;
}

export type EndReasonGroup =
  | 'water'
  | 'plyLimitWater'
  | 'amir'
  | 'qala'
  | 'other';
export const END_REASON_GROUPS: EndReasonGroup[] = [
  'water',
  'plyLimitWater',
  'amir',
  'qala',
  'other',
];

export const AI_BASELINES: Record<string, AiBaseline> = {
  '0.6': {
    rulesVersion: '0.6',
    southScore: 0.542,
    drawRate: 0.003,
    meanPlies: 45.4,
    endReasons: {
      water: 0.506,
      plyLimitWater: 0.206,
      amir: 0.15,
      qala: 0.105,
      other: 0.033,
    },
  },
  '0.7': {
    rulesVersion: '0.7',
    southScore: 0.521,
    drawRate: 0.003,
    meanPlies: 44.6,
    // The v0.7 report gives totals only; end reasons reuse v0.6 until a full split is published.
    endReasons: {
      water: 0.506,
      plyLimitWater: 0.206,
      amir: 0.15,
      qala: 0.105,
      other: 0.033,
    },
  },
};

/** Balance-lab targets (docs/balance-log.md → Targets). */
export const BALANCE_TARGETS = {
  southScore: { center: 0.5, tolerance: 0.05 },
  maxDrawRate: 0.1,
  meanPlies: { min: 20, max: 50 },
} as const;

export function endReasonGroup(reason: EndReason): EndReasonGroup {
  switch (reason) {
    case 'waterVictory':
      return 'water';
    case 'plyLimitWater':
      return 'plyLimitWater';
    case 'amirCaptured':
      return 'amir';
    case 'qalaTaken':
      return 'qala';
    default:
      return 'other';
  }
}
