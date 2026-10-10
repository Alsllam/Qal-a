import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AppSettings } from '../settings/app-settings.model';
import { provideAppSettings } from '../settings/app-settings';
import { HttpErrorReporterService } from './http-error-reporter.service';
import { RestService } from './rest.service';

const settings: AppSettings = {
  application: { name: 't', baseUrl: '' },
  oAuthConfig: {
    issuer: '',
    clientId: '',
    responseType: 'code',
    scope: '',
    requireHttps: false,
  },
  apis: { players: { url: 'https://bff/players-api/' } },
};

describe('RestService', () => {
  let rest: RestService;
  let http: HttpTestingController;
  let reported: HttpErrorResponse[];

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAppSettings(settings),
      ],
    });
    rest = TestBed.inject(RestService);
    http = TestBed.inject(HttpTestingController);
    reported = [];
    TestBed.inject(HttpErrorReporterService).reporter$.subscribe((e) =>
      reported.push(e)
    );
  });

  afterEach(() => http.verify());

  it('joins the api base url and the path without double slashes', () => {
    expect(rest.resolveUrl('/players/list', 'players')).toBe(
      'https://bff/players-api/players/list'
    );
    expect(rest.resolveUrl('https://other/x', 'players')).toBe(
      'https://other/x'
    );
  });

  it('throws for an api name missing from app-settings', () => {
    expect(() => rest.resolveUrl('/x', 'nope')).toThrow(/nope/);
  });

  it('reports errors unless skipHandleError', () => {
    rest
      .request({ method: 'POST', url: '/a' }, { apiName: 'players' })
      .subscribe({ error: () => undefined });
    http
      .expectOne('https://bff/players-api/a')
      .flush({}, { status: 500, statusText: 'x' });
    rest
      .request(
        { method: 'POST', url: '/b' },
        { apiName: 'players', skipHandleError: true }
      )
      .subscribe({ error: () => undefined });
    http
      .expectOne('https://bff/players-api/b')
      .flush({}, { status: 500, statusText: 'x' });
    expect(reported.map((e) => e.url)).toEqual(['https://bff/players-api/a']);
  });
});
