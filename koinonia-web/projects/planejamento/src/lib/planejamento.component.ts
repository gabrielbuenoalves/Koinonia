import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { CampoComponent, EstadoVazioComponent, PeriodoComponent } from 'ui';
import { apiUrl } from '../../../../src/app/api-url';

interface Funcionario {
  id: string;
  nome: string;
}

@Component({
  selector: 'planejamento-tela',
  standalone: true,
  imports: [CampoComponent, PeriodoComponent, EstadoVazioComponent],
  template: `
    @if (erro) {
      <p>{{ erro }}</p>
    }

    <form aria-label="Cadastrar funcionário" (submit)="cadastrar($event)">
      <ui-campo rotulo="Nome" [valor]="nome" (valorChange)="nome = $event" />
      <button type="submit">Cadastrar</button>
    </form>

    @if (funcionarios.length === 0) {
      <ui-estado-vazio mensagem="Nenhum funcionário cadastrado." />
    }

    <form aria-label="Publicar escala" (submit)="publicarEscala($event)">
      <label>
        Funcionário
        <select [value]="funcionarioEscala" (change)="definirFuncionario($event)">
          @for (funcionario of funcionarios; track funcionario.id) {
            <option [value]="funcionario.id">{{ funcionario.nome }}</option>
          }
        </select>
      </label>
      <ui-campo rotulo="Dia" tipo="date" [valor]="diaEscala" (valorChange)="diaEscala = $event" />
      <ui-periodo
        tipo="number"
        [inicio]="inicioEscala"
        [fim]="fimEscala"
        (inicioChange)="inicioEscala = $event"
        (fimChange)="fimEscala = $event"
      />
      <button type="submit">Publicar</button>
      <button type="button" (click)="removerEscala()">Remover</button>
    </form>

    <form aria-label="Publicar evento" (submit)="publicarEvento($event)">
      <ui-campo rotulo="Nome" [valor]="nomeEvento" (valorChange)="nomeEvento = $event" />
      <ui-periodo
        [inicio]="inicioEvento"
        [fim]="fimEvento"
        (inicioChange)="inicioEvento = $event"
        (fimChange)="fimEvento = $event"
      />
      <p>Funcionários</p>
      @for (funcionario of funcionarios; track funcionario.id) {
        <label>
          <input type="checkbox" (change)="alternarFuncionario(funcionario.id, $event)" />
          {{ funcionario.nome }}
        </label>
      }
      <label>
        Forma
        <select (change)="definirForma($event)">
          <option value="hora normal">hora normal</option>
          <option value="adicional">adicional</option>
          <option value="banco de horas">banco de horas</option>
        </select>
      </label>
      <ui-campo rotulo="Percentual" [valor]="percentual" (valorChange)="percentual = $event" />
      <ui-campo rotulo="Fator" [valor]="fator" (valorChange)="fator = $event" />
      <button type="submit">Publicar</button>
    </form>

    <form aria-label="Percentual institucional" (submit)="definirPercentual($event)">
      <ui-campo
        rotulo="Percentual"
        [valor]="percentualInstitucional"
        (valorChange)="percentualInstitucional = $event"
      />
      <button type="submit">Publicar</button>
    </form>
  `,
})
export class PlanejamentoComponent implements OnInit {
  private readonly http = inject(HttpClient);

  nome = '';
  funcionarios: Funcionario[] = [];
  funcionarioEscala = '';
  diaEscala = '';
  inicioEscala = '';
  fimEscala = '';
  nomeEvento = '';
  inicioEvento = '';
  fimEvento = '';
  funcionariosDoEvento: string[] = [];
  forma = 'hora normal';
  percentual = '';
  fator = '';
  percentualInstitucional = '';
  erro = '';

  ngOnInit() {
    this.carregarFuncionarios();
  }

  cadastrar(evento: Event) {
    evento.preventDefault();
    this.erro = '';
    this.http
      .post(`${apiUrl}/funcionarios`, { nome: this.nome }, { responseType: 'text' })
      .subscribe({
        next: () => this.carregarFuncionarios(),
        error: (erro: HttpErrorResponse) => this.mostrar(erro),
      });
  }

  publicarEscala(evento: Event) {
    evento.preventDefault();
    this.erro = '';
    this.http
      .put(
        `${apiUrl}/escalas`,
        {
          funcionarioId: this.funcionarioEscala,
          dia: this.diaEscala,
          intervalos: [{ inicio: Number(this.inicioEscala), fim: Number(this.fimEscala) }],
        },
        { responseType: 'text' },
      )
      .subscribe({
        next: () => undefined,
        error: (erro: HttpErrorResponse) => this.mostrar(erro),
      });
  }

  removerEscala() {
    this.erro = '';
    this.http
      .delete(`${apiUrl}/escalas/${this.funcionarioEscala}/${this.diaEscala}`, { responseType: 'text' })
      .subscribe({
        next: () => undefined,
        error: (erro: HttpErrorResponse) => this.mostrar(erro),
      });
  }

  publicarEvento(evento: Event) {
    evento.preventDefault();
    this.erro = '';
    this.http
      .post(
        `${apiUrl}/eventos`,
        {
          nome: this.nomeEvento,
          inicio: this.inicioEvento,
          fim: this.fimEvento,
          funcionarioIds: this.funcionariosDoEvento,
          forma: this.forma,
          percentual: decimalOuNulo(this.percentual),
          fator: decimalOuNulo(this.fator),
        },
        { responseType: 'text' },
      )
      .subscribe({
        next: () => undefined,
        error: (erro: HttpErrorResponse) => this.mostrar(erro),
      });
  }

  definirPercentual(evento: Event) {
    evento.preventDefault();
    this.erro = '';
    this.http
      .put(
        `${apiUrl}/percentual-extra-comum`,
        { valor: Number(this.percentualInstitucional) },
        { responseType: 'text' },
      )
      .subscribe({
        next: () => undefined,
        error: (erro: HttpErrorResponse) => this.mostrar(erro),
      });
  }

  definirFuncionario(evento: Event) {
    this.funcionarioEscala = (evento.target as HTMLSelectElement).value;
  }

  definirForma(evento: Event) {
    this.forma = (evento.target as HTMLSelectElement).value;
  }

  alternarFuncionario(id: string, evento: Event) {
    const marcado = (evento.target as HTMLInputElement).checked;
    if (marcado && !this.funcionariosDoEvento.includes(id)) {
      this.funcionariosDoEvento = [...this.funcionariosDoEvento, id];
      return;
    }
    if (!marcado) {
      this.funcionariosDoEvento = this.funcionariosDoEvento.filter((item) => item !== id);
    }
  }

  private carregarFuncionarios() {
    this.http.get(`${apiUrl}/funcionarios`, { responseType: 'text' }).subscribe({
      next: (texto) => {
        const lista = JSON.parse(texto) as Funcionario[];
        this.funcionarios = lista;
        if (!lista.some((item) => item.id === this.funcionarioEscala)) {
          this.funcionarioEscala = lista[0]?.id ?? '';
        }
      },
      error: (erro: HttpErrorResponse) => this.mostrar(erro),
    });
  }

  private mostrar(erro: HttpErrorResponse) {
    this.erro = typeof erro.error === 'string' && erro.error.trim() ? erro.error : 'A operação foi recusada.';
  }
}

function decimalOuNulo(valor: string): number | null {
  const texto = valor.trim();
  return texto ? Number(texto) : null;
}
