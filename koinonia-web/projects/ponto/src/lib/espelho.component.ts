import { Component, Input } from '@angular/core';
import { acoesDoEspelho, EstadoEspelho, Papel } from './espelho-acoes';

@Component({
  selector: 'ponto-espelho',
  standalone: true,
  template: `
    <p>{{ estado === 'aprovado' ? 'Cópia aprovada' : 'Prévia' }}</p>
    @for (acao of acoes; track acao) {
      <button type="button">{{ acao }}</button>
    }
  `
})
export class EspelhoComponent {
  @Input() estado: EstadoEspelho = 'previa';
  @Input() papel: Papel = 'empregado';
  @Input() trechos: unknown[] = [];
  get acoes() { return acoesDoEspelho(this.estado, this.papel); }
}
