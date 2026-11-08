import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Pharmacien } from './pharmacien';

describe('Pharmacien', () => {
  let component: Pharmacien;
  let fixture: ComponentFixture<Pharmacien>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Pharmacien],
    }).compileComponents();

    fixture = TestBed.createComponent(Pharmacien);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
