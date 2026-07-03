import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FloatingCard } from './floating-card';

describe('FloatingCard', () => {
  let component: FloatingCard;
  let fixture: ComponentFixture<FloatingCard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FloatingCard],
    }).compileComponents();

    fixture = TestBed.createComponent(FloatingCard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
