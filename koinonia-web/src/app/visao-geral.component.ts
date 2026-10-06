import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { atalhosDoModulo } from './menu';

@Component({
  selector: 'app-visao-geral',
  standalone: true,
  imports: [RouterLink],
  template: `
    <h1>O ponto está em uso.</h1>
    <p>A gestão organiza a jornada. O empregado registra a sua.</p>
    <article>
      <h2>Ponto</h2>
      <p>Entrada, saída, escala e fechamento da jornada.</p>
      @for (atalho of atalhos; track atalho.rota) {
        <a [routerLink]="atalho.rota">{{ atalho.rotulo }}</a>
      }
    </article>
  `,
})
export class VisaoGeralComponent {
  get atalhos() {
    return atalhosDoModulo(sessionStorage.getItem('koinonia.papel'));
  }
}
