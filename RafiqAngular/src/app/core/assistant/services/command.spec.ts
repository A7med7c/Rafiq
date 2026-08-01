import { TestBed } from '@angular/core/testing';

import { Command } from './command';

describe('Command', () => {
  let service: Command;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Command);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
