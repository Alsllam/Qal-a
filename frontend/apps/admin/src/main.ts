import { bootstrapApplication } from '@angular/platform-browser';
import { MockApiHandler, loadAppSettings } from '@qala-fe/Core';
import { App } from './app/app';
import { createAppConfig } from './app/app.config';
import { environment } from './environments/environment';
import { loadMockApi } from './mocks/load-mock-api';

async function main(): Promise<void> {
  const settings = await loadAppSettings(environment.remoteEnv.url);
  let mockHandler: MockApiHandler | undefined;
  if (settings.useMockApi) {
    // Production swaps load-mock-api.ts for load-mock-api.prod.ts, which returns null.
    const handler = environment.allowMockApi ? await loadMockApi() : null;
    if (handler) {
      mockHandler = handler;
      console.warn('[qala] Mock API is ON (dev only). Data is fake.');
    } else {
      console.error('[qala] useMockApi is ignored in production builds.');
    }
  }
  await bootstrapApplication(App, createAppConfig(settings, mockHandler));
}

main().catch((err) => console.error(err));
