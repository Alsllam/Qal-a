import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { EnArPipe } from '@qala-fe/Core';

/** 403 / 404 page; route data: `{ code: 403 | 404 }`. */
@Component({
  selector: 'app-status-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslatePipe, RouterLink, EnArPipe],
  template: `
    <section class="status card">
      <p class="code">{{ code | enar }}</p>
      <h1>{{ 'Errors.' + code + 'Title' | translate }}</h1>
      <p>{{ 'Errors.' + code + 'Text' | translate }}</p>
      <a routerLink="/" class="btn btn-primary">{{
        'General.BackHome' | translate
      }}</a>
    </section>
  `,
  styles: `
    .status { max-width: 480px; margin: 48px auto; text-align: center; padding: 40px 24px; }
    .code { font-size: var(--fs-4xl); font-weight: 700; color: var(--primary); margin: 0; }
    h1 { font-size: var(--fs-xl); margin: 8px 0; }
    p { color: var(--text-secondary); }
  `,
})
export class StatusPageComponent {
  readonly code =
    (inject(ActivatedRoute).snapshot.data['code'] as number | undefined) ?? 404;
}
