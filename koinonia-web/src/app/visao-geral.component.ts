import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { atalhosDoModulo } from './menu';

@Component({
  selector: 'app-visao-geral',
  standalone: true,
  imports: [RouterLink],
  template: `
    <h1 class="font-serif text-4xl">O ponto está em uso.</h1>
    <p class="mt-2 text-secundario">A gestão organiza a jornada. O empregado registra a sua.</p>
    <article class="mt-8 rounded-2xl border border-trilho bg-superficie p-6">
      <p class="text-xs font-semibold uppercase tracking-widest text-acento">Módulo</p>
      <h2 class="mt-2 font-serif text-2xl">Ponto</h2>
      <p class="mt-2 text-secundario">Entrada, saída, escala e fechamento da jornada.</p>
      <div class="mt-6 flex flex-wrap gap-3">
        @for (atalho of atalhos; track atalho.rota) {
          <a
            [class]="atalho.rota === '/ponto'
              ? 'rounded-full bg-acento px-4 py-2 text-sobre'
              : 'rounded-full border border-acento px-4 py-2 text-acento'"
            [routerLink]="atalho.rota"
          >{{ atalho.rotulo }}</a>
        }
      </div>
    </article>
  `,
})
export class VisaoGeralComponent {
  get atalhos() {
    return atalhosDoModulo(sessionStorage.getItem('koinonia.papel'));
  }
}
