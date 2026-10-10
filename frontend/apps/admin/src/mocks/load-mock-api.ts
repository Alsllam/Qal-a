import { MockApiHandler } from '@qala-fe/Core';

/** Dev/demo builds: loads the mock API as a lazy chunk. */
export async function loadMockApi(): Promise<MockApiHandler | null> {
  const { createMockApiHandler } = await import('./mock-api');
  return createMockApiHandler();
}
