import { readPermissionClaims } from './auth.service';

function jwt(payload: object): string {
  const b64 = (o: object) =>
    btoa(JSON.stringify(o))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/, '');
  return `${b64({ alg: 'none' })}.${b64(payload)}.sig`;
}

describe('readPermissionClaims', () => {
  it('reads an array or a single permission claim', () => {
    expect(readPermissionClaims(jwt({ permission: ['A', 'B'] }))).toEqual([
      'A',
      'B',
    ]);
    expect(readPermissionClaims(jwt({ permissions: 'A' }))).toEqual(['A']);
  });

  it('is safe with missing or malformed tokens', () => {
    expect(readPermissionClaims(null)).toEqual([]);
    expect(readPermissionClaims('garbage')).toEqual([]);
    expect(readPermissionClaims('a.!!!.c')).toEqual([]);
  });
});
