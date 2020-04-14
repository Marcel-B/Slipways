import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SlipwayListComponent } from './slipway-list.component';

describe('SlipwayListComponent', () => {
  let component: SlipwayListComponent;
  let fixture: ComponentFixture<SlipwayListComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SlipwayListComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SlipwayListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
