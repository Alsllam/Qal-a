import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { PermissionService, Permissions } from '@qala-fe/Core';
import { PlayerListDto, PlayersService } from '@qala-fe/PlayersProxy';
import { ConfirmationService } from '@qala-fe/theme-shared';
import { of } from 'rxjs';
import { PlayersListComponent } from './players-list.component';

const players: PlayerListDto[] = [
  {
    id: 'p1',
    displayName: 'سارة القحطاني',
    rating: 1620,
    ratingDeviation: 60,
    gamesPlayed: 120,
    wins: 70,
    draws: 1,
    losses: 49,
    isActive: true,
    locale: 'ar',
    creationTime: '2026-08-01T10:00:00Z',
  },
  {
    id: 'p2',
    displayName: 'desert_fox',
    rating: 1410,
    ratingDeviation: 90,
    gamesPlayed: 40,
    wins: 15,
    draws: 0,
    losses: 25,
    isActive: false,
    locale: 'en',
    creationTime: '2026-09-01T10:00:00Z',
  },
];

describe('PlayersListComponent', () => {
  const service = {
    getList: jest.fn(() => of({ items: players, totalCount: 2 })),
    deactivate: jest.fn(() => of(undefined)),
    activate: jest.fn(() => of(undefined)),
  };

  beforeEach(() => {
    jest.clearAllMocks();
    TestBed.configureTestingModule({
      imports: [PlayersListComponent],
      providers: [
        provideRouter([]),
        provideTranslateService(),
        { provide: PlayersService, useValue: service },
      ],
    });
  });

  function render() {
    const fixture = TestBed.createComponent(PlayersListComponent);
    fixture.detectChanges();
    tick(1);
    fixture.detectChanges();
    return fixture;
  }

  it('renders one row per player', fakeAsync(() => {
    TestBed.inject(PermissionService).setGrantedPolicies([
      Permissions.Players.ViewPlayer,
    ]);
    const el: HTMLElement = render().nativeElement;
    const rows = el.querySelectorAll('tbody tr');
    expect(rows).toHaveLength(2);
    expect(rows[0].textContent).toContain('سارة القحطاني');
    expect(el.querySelector('.status-tag-danger')).not.toBeNull();
  }));

  it('hides ban/unban without Permissions.Players.ManagePlayer', fakeAsync(() => {
    TestBed.inject(PermissionService).setGrantedPolicies([
      Permissions.Players.ViewPlayer,
    ]);
    const el: HTMLElement = render().nativeElement;
    expect(el.querySelector('.ban-btn')).toBeNull();
    expect(el.querySelector('.unban-btn')).toBeNull();
  }));

  it('shows ban for active and unban for banned players with ManagePlayer, and bans after confirmation', fakeAsync(() => {
    TestBed.inject(PermissionService).setGrantedPolicies([
      Permissions.Players.ViewPlayer,
      Permissions.Players.ManagePlayer,
    ]);
    const fixture = render();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelectorAll('.ban-btn')).toHaveLength(1);
    expect(el.querySelectorAll('.unban-btn')).toHaveLength(1);

    (el.querySelector('.ban-btn') as HTMLButtonElement).click();
    const confirmation = TestBed.inject(ConfirmationService);
    expect(confirmation.current()?.messageKey).toBe('Players.BanConfirm');
    confirmation.current()?.resolve(true);
    tick(1);
    expect(service.deactivate).toHaveBeenCalledWith('p1');
    expect(service.getList).toHaveBeenCalledTimes(2);
    tick(5000);
  }));
});
