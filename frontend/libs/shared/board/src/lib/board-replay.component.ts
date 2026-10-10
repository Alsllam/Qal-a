import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { EnArPipe, LocalizationService } from '@qala-fe/Core';
import { TranslatePipe } from '@ngx-translate/core';
import { BoardComponent } from './board.component';
import {
  INITIAL_POSITION,
  Move,
  NotationError,
  Position,
  parseMove,
  parsePosition,
  replay,
} from './notation';

/**
 * Read-only replay: board + first/previous/next/last controls, a ply slider
 * and the move list. Arrow keys follow the reading direction.
 */
@Component({
  selector: 'app-board-replay',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BoardComponent, TranslatePipe, EnArPipe],
  host: { '(keydown)': 'onKey($event)' },
  template: `
    @if (error()) {
    <p class="replay-error" role="alert">
      {{ 'Board.InvalidRecord' | translate }}
    </p>
    } @else {
    <div class="replay">
      <div class="replay-board">
        <app-board
          [position]="current()"
          [lastMove]="lastMove()"
          [flipped]="flipped()"
        />
      </div>
      <div class="replay-side">
        <div class="status">
          <span class="ply"
            >{{ 'Board.Ply' | translate }} {{ ply() | enar }} /
            {{ maxPly() | enar }}</span
          >
          <span class="to-move" [class.north]="current().toMove === 'north'">
            {{
              (current().toMove === 'south'
                ? 'Board.SouthToMove'
                : 'Board.NorthToMove'
              ) | translate
            }}
          </span>
        </div>
        @if (finalWater(); as water) {
        <div class="water">
          {{ 'Board.Water' | translate }}:
          <strong
            >{{ 'Board.South' | translate }} {{ water.south | enar }}</strong
          >
          ·
          <strong
            >{{ 'Board.North' | translate }} {{ water.north | enar }}</strong
          >
        </div>
        }
        <div
          class="controls"
          role="toolbar"
          [attr.aria-label]="'Board.Controls' | translate"
        >
          <button
            type="button"
            class="btn btn-icon"
            (click)="go(0)"
            [disabled]="ply() === 0"
            [attr.aria-label]="'Board.First' | translate"
          >
            <svg viewBox="0 0 24 24" class="mirror">
              <path d="M6 5v14M18 6l-8 6 8 6z" />
            </svg>
          </button>
          <button
            type="button"
            class="btn btn-icon"
            (click)="go(ply() - 1)"
            [disabled]="ply() === 0"
            [attr.aria-label]="'Board.Previous' | translate"
          >
            <svg viewBox="0 0 24 24" class="mirror">
              <path d="M15 6l-6 6 6 6" />
            </svg>
          </button>
          <input
            type="range"
            min="0"
            [max]="maxPly()"
            [value]="ply()"
            (input)="go(+$any($event.target).value)"
            [attr.aria-label]="'Board.Ply' | translate"
          />
          <button
            type="button"
            class="btn btn-icon"
            (click)="go(ply() + 1)"
            [disabled]="ply() === maxPly()"
            [attr.aria-label]="'Board.Next' | translate"
          >
            <svg viewBox="0 0 24 24" class="mirror">
              <path d="M9 6l6 6-6 6" />
            </svg>
          </button>
          <button
            type="button"
            class="btn btn-icon"
            (click)="go(maxPly())"
            [disabled]="ply() === maxPly()"
            [attr.aria-label]="'Board.Last' | translate"
          >
            <svg viewBox="0 0 24 24" class="mirror">
              <path d="M18 5v14M6 6l8 6-8 6z" />
            </svg>
          </button>
        </div>
        <ol class="moves" dir="ltr">
          @for (m of movePairs(); track m.number) {
          <li>
            <span class="num">{{ m.number }}.</span>
            <button
              type="button"
              class="mv"
              [class.active]="ply() === m.southPly"
              (click)="go(m.southPly)"
            >
              {{ m.south }}
            </button>
            @if (m.north) {
            <button
              type="button"
              class="mv"
              [class.active]="ply() === m.northPly"
              (click)="go(m.northPly)"
            >
              {{ m.north }}
            </button>
            }
          </li>
          }
        </ol>
      </div>
    </div>
    }
  `,
  styles: `
    :host { display: block; outline: none; }
    .replay { display: grid; grid-template-columns: minmax(0, 1fr); gap: 16px; }
    @media (min-width: 900px) { .replay { grid-template-columns: minmax(0, 3fr) minmax(240px, 2fr); } }
    .replay-board { max-width: 520px; width: 100%; justify-self: center; }
    .status { display: flex; justify-content: space-between; gap: 8px; font-weight: 600; }
    .to-move { padding: 2px 10px; border-radius: 999px; background: var(--game-south-fill); color: var(--game-south-edge); font-size: var(--fs-sm); }
    .to-move.north { background: var(--game-north-fill); color: var(--game-south-fill); }
    .water { margin-top: 8px; color: var(--text-secondary); font-size: var(--fs-sm); }
    .controls { display: flex; align-items: center; gap: 4px; margin-block: 12px; }
    .controls input { flex: 1; accent-color: var(--primary); min-width: 0; }
    .controls svg { width: 18px; height: 18px; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; }
    :host-context([dir='rtl']) .mirror { transform: scaleX(-1); }
    .moves { list-style: none; margin: 0; padding: 8px; max-height: 320px; overflow: auto; display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 2px 8px;
      background: var(--surface-raised); border-radius: var(--radius-md); font-family: var(--font-latin); font-variant-numeric: tabular-nums; font-size: var(--fs-sm); }
    .num { color: var(--text-muted); display: inline-block; min-width: 2.2em; }
    .mv { border: 0; background: none; color: var(--text-primary); padding: 1px 4px; border-radius: var(--radius-sm); cursor: pointer; font: inherit; }
    .mv:hover { background: var(--border-subtle); }
    .mv.active { background: var(--accent); color: var(--on-accent); }
    .replay-error { color: var(--danger-500); }
  `,
})
export class BoardReplayComponent {
  private readonly l10n = inject(LocalizationService);

  readonly moves = input<string[]>([]);
  readonly start = input(INITIAL_POSITION);
  /** The match's current position; its water points are shown on the last ply. */
  readonly finalPosition = input<string | null>(null);
  readonly flipped = input(false);

  readonly ply = signal(0);

  private readonly record = computed<{
    positions: Position[];
    moves: Move[];
  } | null>(() => {
    try {
      return {
        positions: replay(this.moves(), this.start()),
        moves: this.moves().map(parseMove),
      };
    } catch (e) {
      if (e instanceof NotationError) return null;
      throw e;
    }
  });

  readonly error = computed(() => this.record() === null);
  readonly maxPly = computed(() => this.record()?.moves.length ?? 0);
  readonly current = computed(
    () =>
      this.record()?.positions[this.ply()] ?? parsePosition(INITIAL_POSITION)
  );
  readonly lastMove = computed(() =>
    this.ply() > 0 ? this.record()?.moves[this.ply() - 1] ?? null : null
  );

  readonly finalWater = computed(() => {
    const final = this.finalPosition();
    if (!final || this.ply() !== this.maxPly()) return null;
    try {
      return parsePosition(final).water;
    } catch {
      return null;
    }
  });

  readonly movePairs = computed(() => {
    const moves = this.moves();
    const startPly = this.record()?.positions[0].ply ?? 0;
    const pairs: {
      number: number;
      south: string;
      north?: string;
      southPly: number;
      northPly: number;
    }[] = [];
    for (let i = 0; i < moves.length; i += 2) {
      pairs.push({
        number: Math.floor((startPly + i) / 2) + 1,
        south: moves[i],
        north: moves[i + 1],
        southPly: i + 1,
        northPly: i + 2,
      });
    }
    return pairs;
  });

  constructor() {
    // Start at the final position whenever a new record arrives.
    effect(() => this.ply.set(this.maxPly()));
  }

  go(ply: number): void {
    this.ply.set(Math.max(0, Math.min(this.maxPly(), ply)));
  }

  onKey(event: KeyboardEvent): void {
    const forward = this.l10n.isRtl() ? 'ArrowLeft' : 'ArrowRight';
    const back = this.l10n.isRtl() ? 'ArrowRight' : 'ArrowLeft';
    if (event.key === forward) this.go(this.ply() + 1);
    else if (event.key === back) this.go(this.ply() - 1);
    else if (event.key === 'Home') this.go(0);
    else if (event.key === 'End') this.go(this.maxPly());
    else return;
    event.preventDefault();
  }
}
