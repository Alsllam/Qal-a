import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {
  ConfirmationDialogComponent,
  ToastContainerComponent,
} from '@qala-fe/theme-shared';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, ToastContainerComponent, ConfirmationDialogComponent],
  template: `
    <router-outlet />
    <app-toast-container />
    <app-confirmation-dialog />
  `,
})
export class App {}
