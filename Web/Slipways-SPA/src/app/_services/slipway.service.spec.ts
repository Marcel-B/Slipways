import { TestBed } from '@angular/core/testing';

import { SlipwayService } from './slipway.service';

describe('SlipwayService', () => {
  let service: SlipwayService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SlipwayService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
