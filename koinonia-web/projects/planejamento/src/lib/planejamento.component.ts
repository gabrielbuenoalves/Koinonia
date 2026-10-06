import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { API_URL, CampoComponent, EstadoVazioComponent, PeriodoComponent } from 'ui';

interface Funcionario {
  id: string;
  nome: string;
}

@Component({
  selector: 'planejamento-tela',
  standalone: true,
  imports: [CampoComponent, PeriodoComponent, EstadoVazioComponent],
  template: `
    <h1 class="mb-4 text-xl">Planejamento</h1>
    <section class="flex flex-col gap-8 rounded-xl bg-superficie p-6">
      @if (erro) {
        <p class="text-secundario">{{ erro }}</p>
      }

      <form class="flex flex-col gap-4" aria-label="Cadastrar funcionário" (submit)="cadastrar($event)">
        <ui-campo rotulo="Nome" [valor]="nome" (valorChange)="nome = $event" />
        <button type="submit" class="rounded-md bg-acento px-4 py-2 text-texto">Cadastrar</button>
      </form>

      @if (funcionarios.length === 0) {
        <ui-estado-vazio mensagem="Nenhum funcionário cadastrado." />
      }

      <form class="flex flex-col gap-4" aria-label="Publicar escala" (submit)="publicarEscala($event)">
        <label class="flex flex-col gap-1 text-secundario">
          Funcionário
          <select class="rounded-md bg-fundo px-3 py-2 text-texto" [value]="funcionarioEscala" (change)="definirFuncionario($event)">
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
        <button type="submit" class="rounded-md bg-acento px-4 py-2 text-texto">Publicar</button>
        <button type="button" class="rounded-md bg-fundo px-4 py-2 text-secundario" (click)="removerEscala()">Remover</button>
      </form>

      <form class="flex flex-col gap-4" aria-label="Publicar evento" (submit)="publicarEvento($event)">
        <ui-campo rotulo="Nome" [valor]="nomeEvento" (valorChange)="nomeEvento = $event" />
        <ui-periodo
          tipo="datetime-local"
          [inicio]="inicioEvento"
          [fim]="fimEvento"
          (inicioChange)="inicioEvento = $event"
          (fimChange)="fimEvento = $event"
        />
        <p class="text-secundario">Funcionários</p>
        @for (funcionario of funcionarios; track funcionario.id) {
          <label>
            <input type="checkbox" (change)="alternarFuncionario(funcionario.id, $event)" />
            {{ funcionario.nome }}
          </label>
        }
        <label class="flex flex-col gap-1 text-secundario">
          Forma
          <select class="rounded-md bg-fundo px-3 py-2 text-texto" (change)="definirForma($event)">
            <option value="hora normal">hora normal</option>
            <option value="adicional">adicional</option>
            <option value="banco de horas">banco de horas</option>
          </select>
        </label>
        <ui-campo rotulo="Percentual" [valor]="percentual" (valorChange)="percentual = $event" />
        <ui-campo rotulo="Fator" [valor]="fator" (valorChange)="fator = $event" />
        <button type="submit" class="rounded-md bg-acento px-4 py-2 text-texto">Publicar</button>
      </form>

      <form class="flex flex-col gap-4" aria-label="Percentual institucional" (submit)="definirPercentual($event)">
        <ui-campo
          rotulo="Percentual"
          [valor]="percentualInstitucional"
          (valorChange)="percentualInstitucional = $event"
        />
        <button type="submit" class="rounded-md bg-acento px-4 py-2 text-texto">Publicar</button>
      </form>
    </section>
  `,
})
export class PlanejamentoComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

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
      .post(`${this.apiUrl}/funcionarios`, { nome: this.nome }, { responseType: 'text' })
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
        `${this.apiUrl}/escalas`,
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
      .delete(`${this.apiUrl}/escalas/${this.funcionarioEscala}/${this.diaEscala}`, { responseType: 'text' })
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
        `${this.apiUrl}/eventos`,
        {
          nome: this.nomeEvento,
          inicio: comFusoDeSaoPaulo(this.inicioEvento),
          fim: comFusoDeSaoPaulo(this.fimEvento),
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
        `${this.apiUrl}/percentual-extra-comum`,
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
    this.http.get(`${this.apiUrl}/funcionarios`, { responseType: 'text' }).subscribe({
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

function comFusoDeSaoPaulo(valor: string): string {
  return /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(valor) ? `${valor}:00-03:00` : valor;
}

function decimalOuNulo(valor: string): number | null {
  const texto = valor.trim();
  return texto ? Number(texto) : null;
}
