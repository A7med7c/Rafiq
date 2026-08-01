import { TestBed } from '@angular/core/testing';

import { Speech } from './speech';

describe('Speech', () => {
  let service: Speech;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Speech);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
