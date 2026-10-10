import {
  INITIAL_POSITION,
  NotationError,
  applyMove,
  parseMove,
  parsePosition,
  replay,
  squareIndex,
  squareName,
  toNotation,
} from './notation';

describe('notation', () => {
  it('parses the opening position', () => {
    const pos = parsePosition(INITIAL_POSITION);
    expect(pos.toMove).toBe('south');
    expect(pos.ply).toBe(0);
    expect(pos.water).toEqual({ south: 0, north: 0 });
    expect(pos.cells[squareIndex('d1')]).toEqual({ type: 'A', side: 'south' });
    expect(pos.cells[squareIndex('d7')]).toEqual({ type: 'A', side: 'north' });
    expect(pos.cells[squareIndex('d2')]).toEqual({ type: 'R', side: 'south' });
    expect(pos.cells[squareIndex('b7')]).toEqual({ type: 'F', side: 'north' });
    expect(pos.cells[squareIndex('c4')]).toBeNull();
    expect(pos.cells.filter(Boolean)).toHaveLength(16);
  });

  it('reads side, ply and water', () => {
    const pos = parsePosition('3j2j/3r1j1/a6/1j1J3/1AJ3F/3Jff1/RFJ4 n 41 3:10');
    expect(pos.toMove).toBe('north');
    expect(pos.ply).toBe(41);
    expect(pos.water).toEqual({ south: 3, north: 10 });
  });

  it('defaults side, ply and water when omitted', () => {
    const pos = parsePosition('1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1');
    expect(pos).toMatchObject({
      toMove: 'south',
      ply: 0,
      water: { south: 0, north: 0 },
    });
  });

  it('round-trips through toNotation', () => {
    const text = '1j1jf2/1fj4/F6/2j4/1JaJr1F/3R3/2JA1J1 n 41 0:10';
    expect(toNotation(parsePosition(text))).toBe(text);
    expect(toNotation(parsePosition(INITIAL_POSITION))).toBe(INITIAL_POSITION);
  });

  it.each([
    ['too few ranks', '7/7/7 s 0'],
    ['short rank', '1fjajf/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0'],
    ['long rank', '1fjajf11/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0'],
    ['bad piece', '1fjxjf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0'],
    ['bad side', '1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 w 0'],
    ['bad water', '1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0 3-1'],
  ])('rejects %s', (_label, text) => {
    expect(() => parsePosition(text)).toThrow(NotationError);
  });

  it('maps squares both ways', () => {
    expect(squareIndex('a1')).toBe(0);
    expect(squareIndex('g7')).toBe(48);
    expect(squareName(squareIndex('e4'))).toBe('e4');
    expect(() => squareIndex('h1')).toThrow(NotationError);
  });

  it('parses the three move kinds', () => {
    expect(parseMove('b1-b4')).toMatchObject({
      kind: 'step',
      from: squareIndex('b1'),
      to: squareIndex('b4'),
    });
    expect(parseMove('c3xc4').kind).toBe('capture');
    expect(parseMove('d2*d4').kind).toBe('shot');
    expect(() => parseMove('d2d4')).toThrow(NotationError);
  });

  it('applies a step and matches game_core', () => {
    // From packages/game_core/test_vectors/rules_v0.6.json
    const after = applyMove(
      parsePosition(INITIAL_POSITION),
      parseMove('d2-c3')
    );
    expect(toNotation(after)).toBe(
      '1fjajf1/2jrj2/7/7/2R4/2J1J2/1FJAJF1 n 1 0:0'
    );
  });

  it('a Rami shot removes the target and the archer stays', () => {
    const pos = parsePosition('3a3/7/3j3/7/3R3/7/3A3 s 10 0:0');
    const after = applyMove(pos, parseMove('d3*d5'));
    expect(after.cells[squareIndex('d5')]).toBeNull();
    expect(after.cells[squareIndex('d3')]).toEqual({
      type: 'R',
      side: 'south',
    });
  });

  it('a capture replaces the target', () => {
    const pos = parsePosition('3a3/7/7/2jJ3/7/7/3A3 n 9 0:0');
    const after = applyMove(pos, parseMove('c4xd4'));
    expect(after.cells[squareIndex('d4')]).toEqual({
      type: 'J',
      side: 'north',
    });
    expect(after.cells[squareIndex('c4')]).toBeNull();
    expect(after.toMove).toBe('south');
  });

  it('replays a record into one position per ply', () => {
    const positions = replay(['d2-c3', 'd6-e5', 'c3-b4']);
    expect(positions).toHaveLength(4);
    expect(positions[3].ply).toBe(3);
    expect(positions[3].cells[squareIndex('b4')]).toEqual({
      type: 'R',
      side: 'south',
    });
  });

  it('fails clearly when a move starts on an empty square', () => {
    expect(() => replay(['d4-d5'])).toThrow(/No piece on d4/);
  });
});
