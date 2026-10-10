import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
} from '@angular/core';
import { LocalizationService } from '@qala-fe/Core';
import {
  BOARD_SIZE,
  FILES,
  Move,
  NotationError,
  PieceType,
  Position,
  QALA,
  WELLS,
  parsePosition,
  squareIndex,
} from './notation';

const CELL = 10;
const LETTERS_AR: Record<PieceType, string> = {
  A: 'أ',
  J: 'ج',
  F: 'ف',
  R: 'ر',
};

interface SquareView {
  index: number;
  x: number;
  y: number;
  dark: boolean;
  well: boolean;
  qala: 'south' | 'north' | null;
  highlight: 'from' | 'to' | 'shot' | null;
}

/**
 * Read-only SVG board for a Qal'a position. The board itself never mirrors in
 * RTL: files always run a→g from South's left, like the rules diagrams.
 */
@Component({
  selector: 'app-board',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg
      class="board"
      [attr.viewBox]="'-7 0 ' + (size + 8) + ' ' + (size + 8)"
      role="img"
      [attr.aria-label]="ariaLabel()"
      dir="ltr"
    >
      <rect
        x="-1"
        y="-1"
        [attr.width]="size + 2"
        [attr.height]="size + 2"
        rx="2"
        class="edge"
        transform="translate(0 1)"
      />
      <g transform="translate(0 1)">
        @for (sq of squares(); track sq.index) {
        <rect
          [attr.x]="sq.x"
          [attr.y]="sq.y"
          [attr.width]="cell"
          [attr.height]="cell"
          [class.dark]="sq.dark"
          [class.light]="!sq.dark"
          [class.qala-south]="sq.qala === 'south'"
          [class.qala-north]="sq.qala === 'north'"
        />
        @if (sq.qala) {
        <path
          class="qala-mark"
          [attr.d]="crenel(sq.x, sq.y)"
          [class.south]="sq.qala === 'south'"
          [class.north]="sq.qala === 'north'"
        />
        } @if (sq.well) {
        <circle
          class="well"
          [attr.cx]="sq.x + 5"
          [attr.cy]="sq.y + 5"
          r="4.3"
        />
        <path class="well-drop" [attr.d]="drop(sq.x, sq.y)" />
        } @if (sq.highlight === 'from' || sq.highlight === 'to') {
        <rect
          class="hl"
          [attr.x]="sq.x + 0.4"
          [attr.y]="sq.y + 0.4"
          width="9.2"
          height="9.2"
          rx="1"
        />
        } } @for (p of pieces(); track p.key) {
        <g
          class="piece"
          [class.south]="p.side === 'south'"
          [class.north]="p.side === 'north'"
        >
          <circle [attr.cx]="p.x + 5" [attr.cy]="p.y + 5" r="3.7" />
          <text
            [attr.x]="p.x + 5"
            [attr.y]="p.y + 5.15"
            text-anchor="middle"
            dominant-baseline="central"
          >
            {{ p.label }}
          </text>
        </g>
        } @for (sq of squares(); track sq.index) { @if (sq.highlight === 'shot')
        {
        <path class="shot" [attr.d]="cross(sq.x, sq.y)" />
        } } @for (f of fileLabels(); track f.label) {
        <text
          class="coord"
          [attr.x]="f.x"
          [attr.y]="size + 4.2"
          text-anchor="middle"
        >
          {{ f.label }}
        </text>
        } @for (r of rankLabels(); track r.label) {
        <text
          class="coord"
          x="-3.6"
          [attr.y]="r.y"
          text-anchor="middle"
          dominant-baseline="central"
        >
          {{ r.label }}
        </text>
        }
      </g>
    </svg>
  `,
  styles: `
    :host { display: block; }
    .board { width: 100%; height: auto; display: block; }
    .edge { fill: var(--game-board-edge); }
    .light { fill: var(--game-board-light); }
    .dark { fill: var(--game-board-dark); }
    .qala-south { fill: var(--game-qala-south); }
    .qala-north { fill: var(--game-qala-north); }
    .qala-mark { fill: none; stroke-width: 0.6; opacity: 0.85; }
    .qala-mark.south { stroke: var(--game-south-edge); }
    .qala-mark.north { stroke: var(--game-north-edge); }
    .well { fill: var(--game-well); opacity: 0.22; stroke: var(--game-well); stroke-width: 0.5; }
    .well-drop { fill: var(--game-well); opacity: 0.55; }
    .hl { fill: none; stroke: var(--game-selection); stroke-width: 0.8; }
    .shot { stroke: var(--game-capture); stroke-width: 1; stroke-linecap: round; }
    .piece circle { stroke-width: 0.6; transition: cx var(--motion-base) var(--ease-out), cy var(--motion-base) var(--ease-out); }
    .piece.south circle { fill: var(--game-south-fill); stroke: var(--game-south-edge); }
    .piece.north circle { fill: var(--game-north-fill); stroke: var(--game-north-edge); }
    .piece text { font-size: 3.6px; font-weight: 700; font-family: var(--font-latin); pointer-events: none; }
    .piece.south text { fill: var(--game-south-edge); }
    .piece.north text { fill: var(--game-south-fill); }
    .coord { font-size: 2.8px; fill: var(--text-muted); font-family: var(--font-latin); }
  `,
})
export class BoardComponent {
  private readonly l10n = inject(LocalizationService);

  /** A parsed position or its notation. */
  readonly position = input.required<Position | string>();
  readonly lastMove = input<Move | null>(null);
  /** View from North's side (rank 7 at the bottom). */
  readonly flipped = input(false);

  readonly size = BOARD_SIZE * CELL;
  readonly cell = CELL;

  private readonly parsed = computed<Position | null>(() => {
    const value = this.position();
    if (typeof value !== 'string') return value;
    try {
      return parsePosition(value);
    } catch (e) {
      if (e instanceof NotationError) return null;
      throw e;
    }
  });

  readonly squares = computed<SquareView[]>(() => {
    const move = this.lastMove();
    const wells = WELLS.map(squareIndex);
    const qalaS = squareIndex(QALA.south);
    const qalaN = squareIndex(QALA.north);
    const result: SquareView[] = [];
    for (let index = 0; index < BOARD_SIZE * BOARD_SIZE; index++) {
      const { x, y } = this.xy(index);
      const file = index % BOARD_SIZE;
      const rank = Math.floor(index / BOARD_SIZE);
      let highlight: SquareView['highlight'] = null;
      if (move) {
        if (move.kind === 'shot' && index === move.to) highlight = 'shot';
        else if (index === move.from) highlight = 'from';
        else if (index === move.to) highlight = 'to';
      }
      result.push({
        index,
        x,
        y,
        dark: (file + rank) % 2 === 0,
        well: wells.includes(index),
        qala: index === qalaS ? 'south' : index === qalaN ? 'north' : null,
        highlight,
      });
    }
    return result;
  });

  readonly pieces = computed(() => {
    const pos = this.parsed();
    if (!pos) return [];
    const ar = this.l10n.currentLang() === 'ar';
    return pos.cells.flatMap((p, index) => {
      if (!p) return [];
      const { x, y } = this.xy(index);
      return [
        {
          key: `${index}${p.side}${p.type}`,
          x,
          y,
          side: p.side,
          label: ar ? LETTERS_AR[p.type] : p.type,
        },
      ];
    });
  });

  readonly fileLabels = computed(() =>
    FILES.split('').map((label, file) => ({
      label,
      x: (this.flipped() ? BOARD_SIZE - 1 - file : file) * CELL + CELL / 2,
    }))
  );

  readonly rankLabels = computed(() =>
    Array.from({ length: BOARD_SIZE }, (_, rank) => ({
      label: String(rank + 1),
      y: (this.flipped() ? rank : BOARD_SIZE - 1 - rank) * CELL + CELL / 2,
    }))
  );

  readonly ariaLabel = computed(() => {
    const pos = this.parsed();
    return pos
      ? `Qal'a board, ply ${pos.ply}, ${pos.toMove} to move`
      : 'Invalid position';
  });

  crenel(x: number, y: number): string {
    // small tower outline inside the Qal'a square
    const l = x + 2.5;
    const b = y + 8;
    const t = y + 3;
    return `M${l} ${b}V${t}h1v1h1v-1h1v1h1v-1h1V${b}z`;
  }

  drop(x: number, y: number): string {
    const cx = x + 5;
    const top = y + 2.6;
    return `M${cx} ${top}c0 0-2 2.3-2 3.6a2 2 0 0 0 4 0c0-1.3-2-3.6-2-3.6z`;
  }

  cross(x: number, y: number): string {
    return `M${x + 2.5} ${y + 2.5}L${x + 7.5} ${y + 7.5}M${x + 7.5} ${
      y + 2.5
    }L${x + 2.5} ${y + 7.5}`;
  }

  private xy(index: number): { x: number; y: number } {
    const file = index % BOARD_SIZE;
    const rank = Math.floor(index / BOARD_SIZE);
    const col = this.flipped() ? BOARD_SIZE - 1 - file : file;
    const row = this.flipped() ? rank : BOARD_SIZE - 1 - rank;
    return { x: col * CELL, y: row * CELL };
  }
}
