import { Component, computed, signal } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('koinonia-web');
  protected readonly papel = signal(sessionStorage.getItem('koinonia.papel') ?? '');
  protected readonly funcionarioId = signal(sessionStorage.getItem('koinonia.funcionarioId') ?? '');
  protected readonly gestao = computed(() => this.papel().toLowerCase() === 'gestao');

  guardarPapel(evento: Event) {
    const valor = (evento.target as HTMLSelectElement).value;
    sessionStorage.setItem('koinonia.papel', valor);
    this.papel.set(valor);
  }

  guardarFuncionario(evento: Event) {
    const valor = (evento.target as HTMLInputElement).value;
    sessionStorage.setItem('koinonia.funcionarioId', valor);
    this.funcionarioId.set(valor);
  }
}
