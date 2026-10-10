import { MatchListDto, MatchStatus } from '@qala-fe/MatchesProxy';

export function statusTone(
  status: MatchStatus
): 'success' | 'info' | 'neutral' {
  return status === 'Active'
    ? 'info'
    : status === 'Finished'
    ? 'success'
    : 'neutral';
}

/** Translation key for the result column. */
export function resultKey(m: Pick<MatchListDto, 'status' | 'outcome'>): string {
  if (m.status === 'Active') return 'Matches.InProgress';
  if (!m.outcome) return 'Matches.Aborted';
  if (m.outcome.winner === 'south') return 'Matches.SouthWon';
  if (m.outcome.winner === 'north') return 'Matches.NorthWon';
  return 'Matches.Draw';
}
