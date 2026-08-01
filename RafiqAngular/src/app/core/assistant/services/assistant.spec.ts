import { TestBed } from '@angular/core/testing';

import { Assistant } from './assistant';

describe('Assistant', () => {
  let service: Assistant;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Assistant);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
