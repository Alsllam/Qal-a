import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { BoardComponent } from './board.component';
import { BoardReplayComponent } from './board-replay.component';
import { INITIAL_POSITION } from './notation';

describe('BoardComponent', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({ providers: [provideTranslateService()] })
  );

  it('draws 49 squares, 2 wells and 16 pieces for the opening', () => {
    const fixture = TestBed.createComponent(BoardComponent);
    fixture.componentRef.setInput('position', INITIAL_POSITION);
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelectorAll('rect.light, rect.dark')).toHaveLength(49);
    expect(el.querySelectorAll('circle.well')).toHaveLength(2);
    expect(el.querySelectorAll('g.piece.south')).toHaveLength(8);
    expect(el.querySelectorAll('g.piece.north')).toHaveLength(8);
  });

  it('renders nothing for an invalid position instead of throwing', () => {
    const fixture = TestBed.createComponent(BoardComponent);
    fixture.componentRef.setInput('position', 'not a position');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('g.piece')).toHaveLength(0);
  });
});

describe('BoardReplayComponent', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({ providers: [provideTranslateService()] })
  );

  it('starts at the final ply and steps back and forth', () => {
    const fixture = TestBed.createComponent(BoardReplayComponent);
    fixture.componentRef.setInput('moves', ['d2-c3', 'd6-e5', 'c3-b4']);
    fixture.detectChanges();
    const replay = fixture.componentInstance;
    expect(replay.ply()).toBe(3);
    replay.go(0);
    expect(replay.current().ply).toBe(0);
    replay.go(99);
    expect(replay.ply()).toBe(3);
    replay.go(-5);
    expect(replay.ply()).toBe(0);
  });

  it('flags an unreadable record', () => {
    const fixture = TestBed.createComponent(BoardReplayComponent);
    fixture.componentRef.setInput('moves', ['d4-d5']);
    fixture.detectChanges();
    expect(fixture.componentInstance.error()).toBe(true);
    expect(fixture.nativeElement.querySelector('[role=alert]')).not.toBeNull();
  });
});
