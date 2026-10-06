import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { sairDaGestao } from './gestao.guard';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly router = inject(Router);
  protected readonly title = signal('koinonia-web');
  protected readonly papel = signal(sessionStorage.getItem('koinonia.papel') ?? '');
  protected readonly funcionarioId = signal(sessionStorage.getItem('koinonia.funcionarioId') ?? '');
  protected readonly gestao = computed(() => this.papel().toLowerCase() === 'gestao');

  guardarPapel(evento: Event) {
    const valor = (evento.target as HTMLSelectElement).value;
    sessionStorage.setItem('koinonia.papel', valor);
    this.papel.set(valor);
    sairDaGestao(this.router, valor);
  }

  guardarFuncionario(evento: Event) {
    const valor = (evento.target as HTMLInputElement).value;
    sessionStorage.setItem('koinonia.funcionarioId', valor);
    this.funcionarioId.set(valor);
  }
}
