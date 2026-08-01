import { TestBed } from '@angular/core/testing';

import { Avatar } from './avatar';

describe('Avatar', () => {
  let service: Avatar;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Avatar);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
