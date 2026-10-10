import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { BoardReplayComponent } from '@qala-fe/Board';
import { EnArPipe, LocalizationService } from '@qala-fe/Core';
import { MatchDto, MatchesService } from '@qala-fe/MatchesProxy';
import {
  EmptyStateComponent,
  PageHeaderComponent,
  SkeletonComponent,
} from '@qala-fe/theme-shared';
import { TranslatePipe } from '@ngx-translate/core';
import { resultKey, statusTone } from './match-display';

/** Read-only match view with a board replay (route `matches/view/:id`). */
@Component({
  selector: 'app-match-view',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageHeaderComponent,
    BoardReplayComponent,
    SkeletonComponent,
    EmptyStateComponent,
    TranslatePipe,
    EnArPipe,
  ],
  templateUrl: './match-view.component.html',
  styles: `
    .layout { display: grid; gap: 16px; grid-template-columns: minmax(0, 1fr); }
    @media (min-width: 1200px) { .layout { grid-template-columns: minmax(0, 3fr) minmax(260px, 1fr); } }
    dl { display: grid; grid-template-columns: auto 1fr; gap: 8px 16px; margin: 0; font-size: var(--fs-sm); }
    dt { color: var(--text-muted); }
    dd { margin: 0; font-weight: 500; }
    .versus { display: flex; flex-direction: column; gap: 10px; margin-block-end: 16px; }
    .side { display: flex; align-items: center; gap: 10px; }
    .dot { width: 14px; height: 14px; border-radius: 50%; border: 2px solid; flex: none; }
    .dot.south { background: var(--game-south-fill); border-color: var(--game-south-edge); }
    .dot.north { background: var(--game-north-fill); border-color: var(--game-north-edge); }
    .name { font-weight: 600; }
    .winner { color: var(--success-500); font-size: var(--fs-xs); font-weight: 700; }
  `,
})
export class MatchViewComponent {
  private readonly matches = inject(MatchesService);
  readonly l10n = inject(LocalizationService);

  /** Route param (withComponentInputBinding). */
  readonly id = input.required<string>();

  readonly match = rxResource<MatchDto, string>({
    params: () => this.id(),
    stream: ({ params }) => this.matches.get(params),
  });

  readonly statusTone = statusTone;
  readonly resultKey = resultKey;

  readonly title = computed(() => {
    const m = this.match.value();
    return m
      ? `\u2068${m.south.displayName}\u2069 — \u2068${m.north.displayName}\u2069`
      : '';
  });

  clock(ms: number): string {
    const total = Math.max(0, Math.round(ms / 1000));
    const text = `${Math.floor(total / 60)}:${String(total % 60).padStart(
      2,
      '0'
    )}`;
    return this.l10n.localizeDigits(text);
  }
}
