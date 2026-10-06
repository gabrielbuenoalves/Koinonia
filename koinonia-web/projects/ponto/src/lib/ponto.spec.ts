import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Ponto } from './ponto';

describe('Ponto', () => {
  let component: Ponto;
  let fixture: ComponentFixture<Ponto>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Ponto]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Ponto);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
