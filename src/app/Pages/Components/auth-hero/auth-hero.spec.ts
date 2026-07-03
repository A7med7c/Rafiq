import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AuthHero } from './auth-hero';

describe('AuthHero', () => {
  let component: AuthHero;
  let fixture: ComponentFixture<AuthHero>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AuthHero],
    }).compileComponents();

    fixture = TestBed.createComponent(AuthHero);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
