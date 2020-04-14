import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { WaterListComponent } from './water-list.component';

describe('WaterListComponent', () => {
  let component: WaterListComponent;
  let fixture: ComponentFixture<WaterListComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ WaterListComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(WaterListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
