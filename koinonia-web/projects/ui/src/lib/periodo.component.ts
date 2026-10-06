import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CampoComponent } from './campo.component';

@Component({
  selector: 'ui-periodo',
  standalone: true,
  imports: [CampoComponent],
  template: `
    <ui-campo
      rotulo="Início"
      [tipo]="tipo"
      [valor]="inicio"
      (valorChange)="inicioChange.emit($event)"
    />
    <ui-campo rotulo="Fim" [tipo]="tipo" [valor]="fim" (valorChange)="fimChange.emit($event)" />
  `,
})
export class PeriodoComponent {
  @Input() inicio = '';
  @Input() fim = '';
  @Input() tipo = 'text';
  @Output() inicioChange = new EventEmitter<string>();
  @Output() fimChange = new EventEmitter<string>();
}
