import { HttpClient, HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { API_URL, CampoComponent, EstadoVazioComponent, PeriodoComponent } from 'ui';
import { EspelhoComponent } from './espelho.component';
import { EstadoEspelho, Papel, papelDoEspelho } from './espelho-acoes';

interface TrechoEspelho {
  inicio: string;
  fim: string;
  fatia: string;
  evento: string | null;
  percentual: number | null;
  fator: number | null;
  minutosTrabalhados: number;
  minutosCreditados: number | null;
}

interface EspelhoResposta {
  aprovado: boolean;
  trechos: TrechoEspelho[];
}

@Component({
  selector: 'ponto-tela',
  standalone: true,
  imports: [CampoComponent, PeriodoComponent, EstadoVazioComponent, EspelhoComponent],
  template: `
    <h1 class="mb-4 text-xl">Ponto</h1>
    <section class="flex flex-col gap-4 rounded-xl bg-superficie p-6">
      @if (erro) {
        <p class="text-secundario">{{ erro }}</p>
      }

      <ui-campo rotulo="Funcionário" [valor]="funcionarioId" (valorChange)="funcionarioId = $event" />
      <ui-campo rotulo="Dia" tipo="date" [valor]="dia" (valorChange)="dia = $event" />
      <button type="button" class="rounded-md bg-acento px-4 py-2 text-texto" (click)="carregar()">Carregar</button>

      <form class="flex flex-col gap-4" aria-label="Batida" (submit)="$event.preventDefault()">
        <ui-periodo
          tipo="datetime-local"
          [inicio]="entrada"
          [fim]="saida"
          (inicioChange)="entrada = $event"
          (fimChange)="saida = $event"
        />
      </form>

      @if (carregado) {
        <ponto-espelho
          [estado]="estado"
          [papel]="papel"
          [trechos]="trechos"
          (acaoEscolhida)="executar($event)"
        />
        @if (trechos.length === 0) {
          <ui-estado-vazio mensagem="Nenhum trecho neste dia." />
        }
        @for (trecho of trechos; track $index) {
          <p class="text-texto">
            {{ trecho.inicio }}
            {{ trecho.fim }}
            {{ trecho.fatia }}
            {{ trecho.evento }}
            {{ trecho.percentual }}
            {{ trecho.fator }}
            {{ trecho.minutosTrabalhados }}
            {{ trecho.minutosCreditados }}
          </p>
        }
      }
    </section>
  `,
})
export class PontoComponent {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiUrl = inject(API_URL);

  funcionarioId = sessionStorage.getItem('koinonia.funcionarioId') ?? '';
  dia = '';
  entrada = '';
  saida = '';
  estado: EstadoEspelho = 'previa';
  trechos: TrechoEspelho[] = [];
  carregado = false;
  erro = '';

  get papel(): Papel {
    return papelDoEspelho(sessionStorage.getItem('koinonia.papel'));
  }

  carregar() {
    if (!this.funcionarioId || !this.dia) {
      return;
    }
    this.erro = '';
    this.http
      .get(`${this.apiUrl}/espelho/${this.funcionarioId}/${this.dia}`, { responseType: 'text' })
      .subscribe({
        next: (texto) => {
          const corpo = JSON.parse(texto) as EspelhoResposta;
          this.estado = corpo.aprovado ? 'aprovado' : 'previa';
          this.trechos = corpo.trechos ?? [];
          this.carregado = true;
        },
        error: (erro: HttpErrorResponse) => this.mostrar(erro),
      });
  }

  executar(acao: string) {
    if (acao === 'registrar') {
      this.registrar();
    } else if (acao === 'aprovar') {
      this.aprovar();
    } else if (acao === 'reabrir') {
      this.reabrir();
    } else if (acao === 'exportar') {
      void this.router.navigate(['/fechamento']);
    }
  }

  private registrar() {
    this.erro = '';
    this.http
      .post(`${this.apiUrl}/batidas/entrada`, { entrada: comFusoDeSaoPaulo(this.entrada) }, { responseType: 'text' })
      .subscribe({
        next: (texto) => {
          const corpo = JSON.parse(texto) as { id: string };
          this.http
            .post(`${this.apiUrl}/batidas/${corpo.id}/saida`, { saida: comFusoDeSaoPaulo(this.saida) }, { responseType: 'text' })
            .subscribe({
              next: () => this.carregar(),
              error: (erro: HttpErrorResponse) => this.descartarEntrada(corpo.id, erro),
            });
        },
        error: (erro: HttpErrorResponse) => this.mostrar(erro),
      });
  }

  private descartarEntrada(id: string, erro: HttpErrorResponse) {
    this.http.delete(`${this.apiUrl}/batidas/${id}`, { responseType: 'text' }).subscribe({
      next: () => this.mostrar(erro),
      error: () => this.mostrar(erro),
    });
  }

  private aprovar() {
    this.enviarDia('post');
  }

  private reabrir() {
    this.enviarDia('delete');
  }

  private enviarDia(metodo: 'post' | 'delete') {
    this.erro = '';
    const url = `${this.apiUrl}/dias/${this.funcionarioId}/${this.dia}/aprovacao`;
    const pedido =
      metodo === 'post'
        ? this.http.post(url, null, { responseType: 'text' })
        : this.http.delete(url, { responseType: 'text' });
    pedido.subscribe({
      next: () => this.carregar(),
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

@Component({
  selector: 'ponto-fechamento',
  standalone: true,
  imports: [PeriodoComponent],
  template: `
    <h1 class="mb-4 text-xl">Fechamento</h1>
    <section class="rounded-xl bg-superficie p-6">
      @if (erro) {
        <p class="text-secundario">{{ erro }}</p>
      }

      <form class="flex flex-col gap-4" aria-label="Fechamento" (submit)="baixar($event)">
        <ui-periodo
          tipo="date"
          [inicio]="inicio"
          [fim]="fim"
          (inicioChange)="inicio = $event"
          (fimChange)="fim = $event"
        />
        <label class="flex flex-col gap-1 text-secundario">
          Formato
          <select class="rounded-md bg-fundo px-3 py-2 text-texto" (change)="definirFormato($event)">
            <option value="csv">csv</option>
            <option value="excel">excel</option>
          </select>
        </label>
        <button type="submit" class="rounded-md bg-acento px-4 py-2 text-texto">Baixar</button>
      </form>
    </section>
  `,
})
export class FechamentoComponent {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  inicio = '';
  fim = '';
  formato = 'csv';
  erro = '';

  baixar(evento: Event) {
    evento.preventDefault();
    this.erro = '';
    const url = `${this.apiUrl}/fechamento?inicio=${this.inicio}&fim=${this.fim}&formato=${this.formato}`;
    this.http.get(url, { responseType: 'blob', observe: 'response' }).subscribe({
      next: (resposta) => this.salvar(resposta),
      error: () => {
        this.erro = 'A operação foi recusada.';
      },
    });
  }

  definirFormato(evento: Event) {
    this.formato = (evento.target as HTMLSelectElement).value;
  }

  private salvar(resposta: HttpResponse<Blob>) {
    const blob = resposta.body;
    if (!blob) {
      return;
    }
    const disposicao = resposta.headers.get('content-disposition') ?? '';
    const nome = /filename="?([^"]+)"?/.exec(disposicao)?.[1] ?? 'fechamento';
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = nome;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  }
}
