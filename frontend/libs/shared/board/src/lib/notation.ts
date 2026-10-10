/**
 * Tiny reader for Qal'a position notation (no rules engine).
 *
 * `<ranks> <s|n> <ply> <south>:<north>`, e.g. the opening
 * `1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0 0:0`. Ranks go from 7 down to 1,
 * files a–g, digits are runs of empty squares, upper case = South,
 * lower case = North. Mirrors `GameState.fromNotation` in packages/game_core.
 */

export const BOARD_SIZE = 7;
export const FILES = 'abcdefg';
export const WELLS = ['c4', 'e4'] as const;
export const QALA = { south: 'd1', north: 'd7' } as const;
export const INITIAL_POSITION = '1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0 0:0';

export type PieceType = 'A' | 'J' | 'F' | 'R';
export type Side = 'south' | 'north';

export interface Piece {
  type: PieceType;
  side: Side;
}

export interface Position {
  /** Index = rank * 7 + file (a1 = 0, g7 = 48). */
  cells: (Piece | null)[];
  toMove: Side;
  ply: number;
  water: { south: number; north: number };
}

export type MoveKind = 'step' | 'capture' | 'shot';

export interface Move {
  from: number;
  to: number;
  kind: MoveKind;
  text: string;
}

export class NotationError extends Error {}

export function squareIndex(name: string): number {
  const m = /^([a-g])([1-7])$/.exec(name);
  if (!m) throw new NotationError(`Bad square "${name}"`);
  return (Number(m[2]) - 1) * BOARD_SIZE + FILES.indexOf(m[1]);
}

export function squareName(index: number): string {
  return `${FILES[index % BOARD_SIZE]}${Math.floor(index / BOARD_SIZE) + 1}`;
}

export function parsePosition(text: string): Position {
  const parts = text.trim().split(/\s+/);
  if (!parts[0] || parts.length > 4)
    throw new NotationError(`Bad position "${text}"`);
  const ranks = parts[0].split('/');
  if (ranks.length !== BOARD_SIZE)
    throw new NotationError(`Expected 7 ranks in "${text}"`);

  const cells: (Piece | null)[] = new Array(BOARD_SIZE * BOARD_SIZE).fill(null);
  ranks.forEach((row, r) => {
    const rank = BOARD_SIZE - 1 - r;
    let file = 0;
    for (const ch of row) {
      if (/[1-7]/.test(ch)) {
        file += Number(ch);
        continue;
      }
      const type = ch.toUpperCase();
      if (!'AJFR'.includes(type) || file >= BOARD_SIZE) {
        throw new NotationError(`Bad piece "${ch}" on rank ${rank + 1}`);
      }
      cells[rank * BOARD_SIZE + file] = {
        type: type as PieceType,
        side: ch === type ? 'south' : 'north',
      };
      file++;
    }
    if (file !== BOARD_SIZE)
      throw new NotationError(`Rank ${rank + 1} does not have 7 squares`);
  });

  const side = parts[1] ?? 's';
  if (side !== 's' && side !== 'n')
    throw new NotationError(`Bad side "${side}"`);
  const ply = parts[2] === undefined ? 0 : Number(parts[2]);
  if (!Number.isInteger(ply) || ply < 0)
    throw new NotationError(`Bad ply "${parts[2]}"`);
  let water = { south: 0, north: 0 };
  if (parts[3] !== undefined) {
    const m = /^(\d+):(\d+)$/.exec(parts[3]);
    if (!m) throw new NotationError(`Bad water "${parts[3]}"`);
    water = { south: Number(m[1]), north: Number(m[2]) };
  }
  return { cells, toMove: side === 's' ? 'south' : 'north', ply, water };
}

export function toNotation(pos: Position): string {
  const ranks: string[] = [];
  for (let rank = BOARD_SIZE - 1; rank >= 0; rank--) {
    let row = '';
    let empty = 0;
    for (let file = 0; file < BOARD_SIZE; file++) {
      const p = pos.cells[rank * BOARD_SIZE + file];
      if (!p) {
        empty++;
        continue;
      }
      if (empty) row += empty;
      empty = 0;
      row += p.side === 'south' ? p.type : p.type.toLowerCase();
    }
    if (empty) row += empty;
    ranks.push(row);
  }
  return `${ranks.join('/')} ${pos.toMove === 'south' ? 's' : 'n'} ${pos.ply} ${
    pos.water.south
  }:${pos.water.north}`;
}

/** `b1-b4` step, `c3xc4` capture, `d2*d4` Rami shot. */
export function parseMove(text: string): Move {
  const m = /^([a-g][1-7])([-x*])([a-g][1-7])$/.exec(text.trim());
  if (!m) throw new NotationError(`Bad move "${text}"`);
  const kind: MoveKind =
    m[2] === '-' ? 'step' : m[2] === 'x' ? 'capture' : 'shot';
  return {
    from: squareIndex(m[1]),
    to: squareIndex(m[3]),
    kind,
    text: text.trim(),
  };
}

/**
 * Applies a move mechanically, for replay only: it does not check legality
 * and cannot know the water points gained (they stay unchanged).
 */
export function applyMove(pos: Position, move: Move): Position {
  const cells = [...pos.cells];
  const mover = cells[move.from];
  if (!mover)
    throw new NotationError(
      `No piece on ${squareName(move.from)} for ${move.text}`
    );
  if (move.kind === 'shot') {
    cells[move.to] = null;
  } else {
    cells[move.to] = mover;
    cells[move.from] = null;
  }
  return {
    cells,
    toMove: pos.toMove === 'south' ? 'north' : 'south',
    ply: pos.ply + 1,
    water: { ...pos.water },
  };
}

/** Positions after each ply: `[start, after move 1, …]`. */
export function replay(moves: string[], start = INITIAL_POSITION): Position[] {
  const positions = [parsePosition(start)];
  for (const text of moves) {
    positions.push(applyMove(positions[positions.length - 1], parseMove(text)));
  }
  return positions;
}
