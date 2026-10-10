import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  effect,
  inject,
  viewChild,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { IconComponent } from '../icon/icon.component';
import { ConfirmationService } from './confirmation.service';

@Component({
  selector: 'app-confirmation-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslatePipe, IconComponent],
  host: { '(document:keydown.escape)': 'answer(false)' },
  template: `
    @if (confirmation.current(); as c) {
    <div class="backdrop" (click)="answer(false)" aria-hidden="true"></div>
    <div
      class="dialog"
      role="alertdialog"
      aria-modal="true"
      aria-labelledby="confirm-title"
      aria-describedby="confirm-msg"
      [class]="'dialog sev-' + c.severity"
    >
      <div class="icon"><app-icon name="alert" [size]="24" /></div>
      <h2 id="confirm-title">{{ c.titleKey | translate }}</h2>
      <p id="confirm-msg">{{ c.messageKey | translate : c.params }}</p>
      <div class="actions">
        <button type="button" class="btn btn-secondary" (click)="answer(false)">
          {{ 'General.Cancel' | translate }}
        </button>
        <button
          #confirmBtn
          type="button"
          class="btn"
          [class.btn-danger]="c.severity === 'error'"
          [class.btn-primary]="c.severity !== 'error'"
          (click)="answer(true)"
        >
          {{ c.confirmKey | translate }}
        </button>
      </div>
    </div>
    }
  `,
  styles: `
    .backdrop { position: fixed; inset: 0; background: rgba(15, 18, 38, .45); z-index: 1050; animation: fade var(--motion-base) var(--ease-out); }
    .dialog { position: fixed; z-index: 1051; inset-block-start: 50%; inset-inline-start: 50%; transform: translate(-50%, -50%); width: min(420px, calc(100vw - 32px));
      background: var(--surface); color: var(--text-primary); border-radius: var(--radius-xl); box-shadow: var(--shadow-lg); padding: 24px;
      animation: pop var(--motion-slow) var(--ease-out); }
    :host-context([dir='rtl']) .dialog { transform: translate(50%, -50%); animation-name: pop-rtl; }
    .icon { width: 44px; height: 44px; border-radius: 50%; display: grid; place-items: center; background: var(--warning-50); color: var(--warning-700); margin-block-end: 12px; }
    .sev-error .icon { background: var(--danger-50); color: var(--danger-700); }
    .sev-info .icon { background: var(--info-50); color: var(--info-700); }
    h2 { margin: 0 0 6px; font-size: var(--fs-lg); }
    p { margin: 0 0 20px; color: var(--text-secondary); }
    .actions { display: flex; justify-content: flex-end; gap: 8px; }
    @keyframes fade { from { opacity: 0; } }
    @keyframes pop { from { opacity: 0; transform: translate(-50%, -50%) scale(.96); } }
    @keyframes pop-rtl { from { opacity: 0; transform: translate(50%, -50%) scale(.96); } }
    @media (prefers-reduced-motion: reduce) { .dialog, .backdrop { animation: fade var(--motion-base); } }
  `,
})
export class ConfirmationDialogComponent {
  readonly confirmation = inject(ConfirmationService);
  private readonly confirmBtn =
    viewChild<ElementRef<HTMLButtonElement>>('confirmBtn');

  constructor() {
    effect(() => this.confirmBtn()?.nativeElement.focus());
  }

  answer(confirmed: boolean): void {
    this.confirmation.current()?.resolve(confirmed);
  }
}
