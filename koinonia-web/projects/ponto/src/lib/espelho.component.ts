import { Component, EventEmitter, Input, Output } from '@angular/core';
import { acoesDoEspelho, EstadoEspelho, Papel, rotuloDaAcao } from './espelho-acoes';

@Component({
  selector: 'ponto-espelho',
  standalone: true,
  template: `
    <p class="inline-flex rounded-full bg-realce px-3 py-1 text-xs font-semibold uppercase tracking-wide text-acento">{{ estado === 'aprovado' ? 'Cópia aprovada' : 'Prévia' }}</p>
    <div class="mt-4 flex flex-wrap gap-2">
      @for (acao of acoes; track acao) {
        <button type="button" class="rounded-full bg-acento px-4 py-2 text-sobre" (click)="acaoEscolhida.emit(acao)">{{ rotuloDaAcao(acao) }}</button>
      }
    </div>
  `
})
export class EspelhoComponent {
  @Input() estado: EstadoEspelho = 'previa';
  @Input() papel: Papel = 'empregado';
  @Input() trechos: unknown[] = [];
  @Output() acaoEscolhida = new EventEmitter<string>();
  protected readonly rotuloDaAcao = rotuloDaAcao;
  get acoes() { return acoesDoEspelho(this.estado, this.papel); }
}
