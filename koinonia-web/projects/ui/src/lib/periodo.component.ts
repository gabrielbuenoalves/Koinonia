import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CampoComponent } from './campo.component';

@Component({
  selector: 'ui-periodo',
  standalone: true,
  imports: [CampoComponent],
  template: `
    <div class="grid gap-4 sm:grid-cols-2">
      <ui-campo
        rotulo="Início"
        [tipo]="tipo"
        [valor]="inicio"
        (valorChange)="inicioChange.emit($event)"
      />
      <ui-campo rotulo="Fim" [tipo]="tipo" [valor]="fim" (valorChange)="fimChange.emit($event)" />
    </div>
  `,
})
export class PeriodoComponent {
  @Input() inicio = '';
  @Input() fim = '';
  @Input() tipo = 'text';
  @Output() inicioChange = new EventEmitter<string>();
  @Output() fimChange = new EventEmitter<string>();
}
