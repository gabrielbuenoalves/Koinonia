import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { identificadorValido } from './sessao';

@Component({
  selector: 'app-entrada',
  standalone: true,
  template: `
    <form aria-label="Entrar" (submit)="confirmar($event)">
      <label>
        Papel
        <select [value]="papel" (change)="definirPapel($event)">
          <option value=""></option>
          <option value="Gestao">Gestão</option>
          <option value="Empregado">Empregado</option>
        </select>
      </label>
      <label>
        Identificador
        <input [value]="identificador" (input)="definirIdentificador($event)" />
      </label>
      @if (erro) {
        <p>{{ erro }}</p>
      }
      <button type="submit">Entrar</button>
    </form>
  `,
})
export class EntradaComponent {
  private readonly router = inject(Router);
  papel = '';
  identificador = '';
  erro = '';

  definirPapel(evento: Event) {
    this.papel = (evento.target as HTMLSelectElement).value;
  }

  definirIdentificador(evento: Event) {
    this.identificador = (evento.target as HTMLInputElement).value;
  }

  confirmar(evento: Event) {
    evento.preventDefault();
    const identificador = this.identificador.trim();
    if (!this.papel || !identificadorValido(identificador)) {
      this.erro = 'Informe o papel e um identificador válido.';
      return;
    }
    sessionStorage.setItem('koinonia.papel', this.papel);
    sessionStorage.setItem('koinonia.funcionarioId', identificador);
    void this.router.navigateByUrl('/');
  }
}
