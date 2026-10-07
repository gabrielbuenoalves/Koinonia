import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { sessaoValida } from './sessao';

@Component({
  selector: 'app-entrada',
  standalone: true,
  template: `
    <form class="flex flex-col gap-4 rounded-2xl border border-trilho bg-superficie p-6" aria-label="Entrar" (submit)="confirmar($event)">
      <p class="text-xs font-semibold uppercase tracking-widest text-acento">Casa</p>
      <h1 class="font-serif text-4xl">Koinonia</h1>
      <label class="flex flex-col gap-1 text-secundario">
        Papel
        <select class="rounded-xl border border-trilho bg-campo px-3 py-2 text-texto" [value]="papel" (change)="definirPapel($event)">
          <option value=""></option>
          <option value="Gestao">Gestão</option>
          <option value="Empregado">Empregado</option>
        </select>
      </label>
      <label class="flex flex-col gap-1 text-secundario">
        Identificador
        <input class="rounded-xl border border-trilho bg-campo px-3 py-2 text-texto" [value]="identificador" (input)="definirIdentificador($event)" />
      </label>
      @if (erro) {
        <p class="text-secundario">{{ erro }}</p>
      }
      <button type="submit" class="rounded-full bg-acento px-4 py-2 text-sobre">Entrar</button>
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
    const papel = this.papel.trim();
    const papelExato = papel === 'Gestao' || papel === 'Empregado';
    if (!sessaoValida(papel, identificador) || !papelExato) {
      this.erro = 'Informe o papel e um identificador válido.';
      return;
    }
    sessionStorage.setItem('koinonia.papel', papel);
    sessionStorage.setItem('koinonia.funcionarioId', identificador);
    void this.router.navigateByUrl('/');
  }
}
