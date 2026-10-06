import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { routes } from './app.routes';

describe('App', () => {
  beforeEach(async () => {
    sessionStorage.clear();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter(routes)],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('mostra o nome Koinonia para quem entrou', () => {
    sessionStorage.setItem('koinonia.papel', 'Gestao');
    sessionStorage.setItem('koinonia.funcionarioId', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Koinonia');
    expect(compiled.textContent).not.toContain('Hello');
  });
});
