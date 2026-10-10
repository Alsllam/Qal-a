import { MockApiHandler } from '@qala-fe/Core';

/** Production builds (fileReplacements): the mock API is not even bundled. */
export async function loadMockApi(): Promise<MockApiHandler | null> {
  return null;
}
