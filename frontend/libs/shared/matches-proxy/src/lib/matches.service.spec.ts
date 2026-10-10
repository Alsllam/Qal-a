import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AppSettings, provideAppSettings } from '@qala-fe/Core';
import { MatchesService } from './matches.service';

const settings = {
  application: { name: 'test', baseUrl: '' },
  oAuthConfig: {
    issuer: '',
    clientId: '',
    responseType: 'code',
    scope: '',
    requireHttps: false,
  },
  apis: { matches: { url: 'https://bff.test/matches-api' } },
} satisfies AppSettings;

describe('MatchesService', () => {
  let service: MatchesService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAppSettings(settings),
      ],
    });
    service = TestBed.inject(MatchesService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  const cases: [string, () => unknown, string, string, unknown][] = [
    [
      'joinQueue',
      () => service.joinQueue({ timeControl: '4+2' }).subscribe(),
      'POST',
      '/matches/queue',
      { timeControl: '4+2' },
    ],
    [
      'leaveQueue',
      () => service.leaveQueue('t1').subscribe(),
      'DELETE',
      '/matches/queue',
      { ticketId: 't1' },
    ],
    [
      'createChallenge',
      () => service.createChallenge({ timeControl: '4+2' }).subscribe(),
      'POST',
      '/matches/challenge',
      { timeControl: '4+2' },
    ],
    [
      'acceptChallenge',
      () => service.acceptChallenge('AB12CD').subscribe(),
      'POST',
      '/matches/challenge/accept',
      { code: 'AB12CD' },
    ],
    [
      'get',
      () => service.get('m1').subscribe(),
      'POST',
      '/matches/getbyid',
      { id: 'm1' },
    ],
    [
      'getMine',
      () => service.getMine({ skipCount: 0, maxResultCount: 5 }).subscribe(),
      'POST',
      '/matches/mine',
      { skipCount: 0, maxResultCount: 5 },
    ],
    [
      'getList',
      () =>
        service
          .getList({
            skipCount: 0,
            maxResultCount: 10,
            status: 'Finished',
            rulesVersion: '0.6',
          })
          .subscribe(),
      'POST',
      '/matches/list',
      {
        skipCount: 0,
        maxResultCount: 10,
        status: 'Finished',
        rulesVersion: '0.6',
      },
    ],
    [
      'getStats',
      () =>
        service
          .getStats({
            from: '2026-09-10',
            to: '2026-10-10',
            rulesVersion: '0.6',
          })
          .subscribe(),
      'POST',
      '/matches/stats',
      { from: '2026-09-10', to: '2026-10-10', rulesVersion: '0.6' },
    ],
  ];

  it.each(cases)('%s → %s %s', (_name, call, method, path, body) => {
    call();
    const req = http.expectOne(`https://bff.test/matches-api${path}`);
    expect(req.request.method).toBe(method);
    expect(req.request.body).toEqual(body);
    req.flush(null);
  });
});
