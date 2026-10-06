import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Planejamento } from './planejamento';

describe('Planejamento', () => {
  let component: Planejamento;
  let fixture: ComponentFixture<Planejamento>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Planejamento]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Planejamento);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
