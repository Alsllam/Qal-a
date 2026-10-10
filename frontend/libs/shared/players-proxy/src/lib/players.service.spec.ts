import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AppSettings, provideAppSettings } from '@qala-fe/Core';
import { PlayersService } from './players.service';

const settings = {
  application: { name: 'test', baseUrl: '' },
  oAuthConfig: {
    issuer: '',
    clientId: '',
    responseType: 'code',
    scope: '',
    requireHttps: false,
  },
  apis: { players: { url: 'https://bff.test/players-api/' } },
} satisfies AppSettings;

describe('PlayersService', () => {
  let service: PlayersService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAppSettings(settings),
      ],
    });
    service = TestBed.inject(PlayersService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  const cases: [string, () => unknown, string, string, unknown][] = [
    ['getMe', () => service.getMe().subscribe(), 'POST', '/players/me', null],
    [
      'updateMe',
      () => service.updateMe({ displayName: 'Sara', locale: 'ar' }).subscribe(),
      'PUT',
      '/players/me',
      { displayName: 'Sara', locale: 'ar' },
    ],
    [
      'getList',
      () =>
        service
          .getList({
            skipCount: 20,
            maxResultCount: 10,
            filterText: 'ali',
            activeFilter: 'InActive',
          })
          .subscribe(),
      'POST',
      '/players/list',
      {
        skipCount: 20,
        maxResultCount: 10,
        filterText: 'ali',
        activeFilter: 'InActive',
      },
    ],
    [
      'get',
      () => service.get('p1').subscribe(),
      'POST',
      '/players/getbyid',
      { id: 'p1' },
    ],
    [
      'getLeaderboard',
      () =>
        service
          .getLeaderboard({ skipCount: 0, maxResultCount: 50 })
          .subscribe(),
      'POST',
      '/players/leaderboard',
      { skipCount: 0, maxResultCount: 50 },
    ],
    [
      'activate',
      () => service.activate('p1').subscribe(),
      'POST',
      '/players/activate',
      { id: 'p1' },
    ],
    [
      'deactivate',
      () => service.deactivate('p1').subscribe(),
      'POST',
      '/players/deactivate',
      { id: 'p1' },
    ],
  ];

  it.each(cases)('%s → %s %s', (_name, call, method, path, body) => {
    call();
    const req = http.expectOne(`https://bff.test/players-api${path}`);
    expect(req.request.method).toBe(method);
    expect(req.request.body).toEqual(body);
    req.flush(null);
  });

  it('getList is an arrow property so it can be passed to a list engine unbound', () => {
    const fn = service.getList;
    fn({ skipCount: 0, maxResultCount: 10 }).subscribe((r) =>
      expect(r.totalCount).toBe(1)
    );
    http
      .expectOne('https://bff.test/players-api/players/list')
      .flush({ items: [], totalCount: 1 });
  });
});
