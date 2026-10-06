import { Component, EventEmitter, Input, Output } from '@angular/core';
import { acoesDoEspelho, EstadoEspelho, Papel } from './espelho-acoes';

@Component({
  selector: 'ponto-espelho',
  standalone: true,
  template: `
    <p class="text-texto">{{ estado === 'aprovado' ? 'Cópia aprovada' : 'Prévia' }}</p>
    @for (acao of acoes; track acao) {
      <button type="button" class="rounded-md bg-acento px-4 py-2 text-texto" (click)="acaoEscolhida.emit(acao)">{{ acao }}</button>
    }
  `
})
export class EspelhoComponent {
  @Input() estado: EstadoEspelho = 'previa';
  @Input() papel: Papel = 'empregado';
  @Input() trechos: unknown[] = [];
  @Output() acaoEscolhida = new EventEmitter<string>();
  get acoes() { return acoesDoEspelho(this.estado, this.papel); }
}
