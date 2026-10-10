import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { IconComponent, IconName } from '../icon/icon.component';
import { ToastType, ToasterService } from './toaster.service';

const ICON: Record<ToastType, IconName> = {
  success: 'check',
  error: 'alert',
  warning: 'alert',
  info: 'info',
};

@Component({
  selector: 'app-toast-container',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslatePipe, IconComponent],
  template: `
    <div class="toasts" aria-live="polite">
      @for (t of toaster.toasts(); track t.id) {
      <div class="toast" [class]="'toast toast-' + t.type" role="status">
        <app-icon [name]="icon[t.type]" />
        <span class="msg">{{
          t.raw ? t.message : (t.message | translate : t.params)
        }}</span>
        <button
          type="button"
          class="close"
          (click)="toaster.remove(t.id)"
          [attr.aria-label]="'General.Close' | translate"
        >
          <app-icon name="close" [size]="16" />
        </button>
        <span class="progress" [style.animation-duration.ms]="t.life"></span>
      </div>
      }
    </div>
  `,
  styles: `
    .toasts { position: fixed; inset-block-start: 16px; inset-inline-end: 16px; z-index: 1100; display: flex; flex-direction: column; gap: 8px; width: min(380px, calc(100vw - 32px)); }
    .toast { position: relative; overflow: hidden; display: flex; align-items: center; gap: 10px; padding: 12px 14px; border-radius: var(--radius-md);
      background: var(--surface); color: var(--text-primary); box-shadow: var(--shadow-lg); border: 1px solid var(--border-subtle);
      border-inline-start: 4px solid var(--tone); animation: toast-in var(--motion-base) var(--ease-out); }
    .toast-success { --tone: var(--success-500); }
    .toast-error { --tone: var(--danger-500); }
    .toast-warning { --tone: var(--warning-500); }
    .toast-info { --tone: var(--info-500); }
    app-icon { color: var(--tone); }
    .msg { flex: 1; font-size: var(--fs-sm); }
    .close { border: 0; background: none; color: var(--text-muted); cursor: pointer; padding: 2px; border-radius: var(--radius-sm); }
    .progress { position: absolute; inset-inline-start: 0; inset-block-end: 0; height: 3px; width: 100%; background: var(--tone); opacity: .5; transform-origin: left center;
      animation-name: toast-life; animation-timing-function: linear; animation-fill-mode: forwards; }
    :host-context([dir='rtl']) .progress { transform-origin: right center; }
    @keyframes toast-in { from { opacity: 0; transform: translateY(-8px); } }
    @keyframes toast-life { from { transform: scaleX(1); } to { transform: scaleX(0); } }
    @media (prefers-reduced-motion: reduce) { .toast { animation: none; } }
  `,
})
export class ToastContainerComponent {
  readonly toaster = inject(ToasterService);
  readonly icon = ICON;
}
