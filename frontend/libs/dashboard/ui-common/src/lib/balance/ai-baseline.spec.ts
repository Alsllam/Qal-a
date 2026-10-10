import { END_REASONS } from '@qala-fe/MatchesProxy';
import { AI_BASELINES, END_REASON_GROUPS, endReasonGroup } from './ai-baseline';

describe('AI baseline', () => {
  it('matches docs/balance-log.md v0.6', () => {
    const b = AI_BASELINES['0.6'];
    expect(b.southScore).toBe(0.542);
    expect(b.drawRate).toBe(0.003);
    expect(b.meanPlies).toBe(45.4);
    expect(b.endReasons).toMatchObject({
      water: 0.506,
      plyLimitWater: 0.206,
      amir: 0.15,
      qala: 0.105,
    });
  });

  it('end-reason shares add up to the decided games', () => {
    for (const b of Object.values(AI_BASELINES)) {
      const sum = END_REASON_GROUPS.reduce((s, g) => s + b.endReasons[g], 0);
      expect(sum).toBeCloseTo(1, 2);
    }
  });

  it('maps every end reason to a group', () => {
    expect(endReasonGroup('waterVictory')).toBe('water');
    expect(endReasonGroup('plyLimitWater')).toBe('plyLimitWater');
    expect(endReasonGroup('amirCaptured')).toBe('amir');
    expect(endReasonGroup('qalaTaken')).toBe('qala');
    expect(endReasonGroup('resign')).toBe('other');
    for (const r of END_REASONS)
      expect(END_REASON_GROUPS).toContain(endReasonGroup(r));
  });
});
