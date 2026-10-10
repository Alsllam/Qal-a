import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import {
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AuthService,
  LocalizationService,
  MOCK_API_ENABLED,
  RoutesService,
  ThemeModeService,
} from '@qala-fe/Core';
import { TranslatePipe } from '@ngx-translate/core';
import { filter } from 'rxjs';
import { IconComponent } from '../icon/icon.component';

/** Shell: sidebar (menu filtered by permission) + top bar (language, theme, user). */
@Component({
  selector: 'app-application-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    TranslatePipe,
    IconComponent,
  ],
  templateUrl: './application-layout.component.html',
  styleUrl: './application-layout.component.scss',
})
export class ApplicationLayoutComponent {
  readonly routes = inject(RoutesService);
  readonly l10n = inject(LocalizationService);
  readonly theme = inject(ThemeModeService);
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Shows a "mock data" badge so nobody mistakes demo data for production. */
  readonly mockMode = inject(MOCK_API_ENABLED);

  readonly sidebarOpen = signal(false);
  readonly logoSrc = computed(
    () => `assets/brand/logo-${this.l10n.currentLang()}.svg`
  );
  readonly initials = computed(() =>
    (this.auth.userName() ?? '?')
      .split(/\s+/)
      .map((w) => w[0])
      .join('')
      .slice(0, 2)
      .toUpperCase()
  );

  constructor() {
    this.router.events
      .pipe(
        filter((e) => e instanceof NavigationEnd),
        takeUntilDestroyed()
      )
      .subscribe(() => this.sidebarOpen.set(false));
  }

  toggleSidebar(): void {
    this.sidebarOpen.update((v) => !v);
  }

  logout(): void {
    this.auth.logout();
    // Real mode redirects to the issuer's end-session endpoint.
    if (this.auth.mockMode) void this.router.navigateByUrl('/login');
  }
}
