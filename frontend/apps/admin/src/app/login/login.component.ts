import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import {
  AuthService,
  LocalizationService,
  ThemeModeService,
} from '@qala-fe/Core';
import { IconComponent } from '@qala-fe/theme-shared';
import { TranslatePipe } from '@ngx-translate/core';

/** Sign-in page: animated mark (tower rises, drop falls, 800 ms once) + OIDC sign-in. */
@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslatePipe, IconComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  readonly auth = inject(AuthService);
  readonly l10n = inject(LocalizationService);
  readonly theme = inject(ThemeModeService);
  private readonly router = inject(Router);
  private readonly returnUrl = (
    inject(ActivatedRoute).snapshot.queryParamMap.get('returnUrl') ?? '/'
  ).replace(/^(?!\/)/, '/');

  readonly wordmark = computed(() =>
    this.l10n.currentLang() === 'ar' ? 'قلعة' : 'Qal’a'
  );

  constructor() {
    effect(() => {
      if (this.auth.isAuthenticated())
        void this.router.navigateByUrl(this.returnUrl);
    });
  }

  signIn(): void {
    this.auth.login(this.returnUrl);
  }

  devSignIn(): void {
    this.auth.devLogin();
  }
}
