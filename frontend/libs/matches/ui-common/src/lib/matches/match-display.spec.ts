import { resultKey, statusTone } from './match-display';

describe('match display helpers', () => {
  it('maps the result to a translation key', () => {
    expect(resultKey({ status: 'Active', outcome: null })).toBe(
      'Matches.InProgress'
    );
    expect(
      resultKey({
        status: 'Finished',
        outcome: { winner: 'south', reason: 'waterVictory' },
      })
    ).toBe('Matches.SouthWon');
    expect(
      resultKey({
        status: 'Finished',
        outcome: { winner: 'north', reason: 'amirCaptured' },
      })
    ).toBe('Matches.NorthWon');
    expect(
      resultKey({
        status: 'Finished',
        outcome: { winner: null, reason: 'plyLimitDraw' },
      })
    ).toBe('Matches.Draw');
    expect(resultKey({ status: 'Aborted', outcome: null })).toBe(
      'Matches.Aborted'
    );
  });

  it('maps the status to a status-tag tone', () => {
    expect(statusTone('Active')).toBe('info');
    expect(statusTone('Finished')).toBe('success');
    expect(statusTone('Aborted')).toBe('neutral');
  });
});
