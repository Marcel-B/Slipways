import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminSlipwaysComponent } from './admin-slipways.component';

describe('AdminSlipwaysComponent', () => {
  let component: AdminSlipwaysComponent;
  let fixture: ComponentFixture<AdminSlipwaysComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ AdminSlipwaysComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(AdminSlipwaysComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
