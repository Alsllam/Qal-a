import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { replay } from '@qala-fe/Board';
import { createMatches, createPlayers, createStats } from '../mocks/mock-data';
import { SAMPLE_GAMES } from '../mocks/sample-games';

const root = join(__dirname, '..', '..');
const readJson = (path: string) =>
  JSON.parse(readFileSync(join(root, path), 'utf8')) as Record<string, unknown>;

function keys(obj: Record<string, unknown>, prefix = ''): string[] {
  return Object.entries(obj).flatMap(([k, v]) =>
    v && typeof v === 'object'
      ? keys(v as Record<string, unknown>, `${prefix}${k}.`)
      : [`${prefix}${k}`]
  );
}

describe('i18n', () => {
  const ar = readJson('public/i18n/ar.json');
  const en = readJson('public/i18n/en.json');

  it('ar.json and en.json have exactly the same keys', () => {
    expect(keys(ar).sort()).toEqual(keys(en).sort());
  });

  it('Arabic strings are translated, not English copies', () => {
    const flatEn = new Map(
      keys(en).map((k) => [
        k,
        k
          .split('.')
          .reduce<unknown>((o, p) => (o as Record<string, unknown>)[p], en),
      ])
    );
    const copies = keys(ar).filter((k) => {
      const value = k
        .split('.')
        .reduce<unknown>(
          (o, p) => (o as Record<string, unknown>)[p],
          ar
        ) as string;
      return value === flatEn.get(k) && /[A-Za-z]{3}/.test(value);
    });
    expect(copies).toEqual([]);
  });
});

describe('runtime settings', () => {
  it('production settings never enable the mock API and require HTTPS', () => {
    const prod = readJson('src/config/production/app-settings.json');
    expect(prod['useMockApi']).toBe(false);
    expect(
      (prod['oAuthConfig'] as Record<string, unknown>)['requireHttps']
    ).toBe(true);
    expect(
      (prod['oAuthConfig'] as Record<string, unknown>)['responseType']
    ).toBe('code');
  });
});

describe('mock data', () => {
  it('every sample game replays with the board notation reader', () => {
    for (const g of SAMPLE_GAMES) expect(() => replay(g.moves)).not.toThrow();
  });

  it('matches reference existing players and have readable records', () => {
    const players = createPlayers();
    const matches = createMatches(players);
    const ids = new Set(players.map((p) => p.id));
    for (const m of matches) {
      expect(ids.has(m.south.playerId)).toBe(true);
      expect(() => replay(m.moves)).not.toThrow();
    }
  });

  it('stats are internally consistent', () => {
    const s = createStats('2026-09-11', '2026-10-10', '0.6');
    expect(s.byDay).toHaveLength(30);
    expect(s.byDay.reduce((n, d) => n + d.games, 0)).toBe(s.games);
    expect(s.endReasons.reduce((n, r) => n + r.count, 0)).toBe(s.games);
    expect(s.lengthHistogram.reduce((n, b) => n + b.count, 0)).toBe(s.games);
    expect(s.southScore).toBeGreaterThan(0.4);
    expect(s.southScore).toBeLessThan(0.65);
  });
});
