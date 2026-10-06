// O ng-packagr resolve `ui` aqui. O app continua usando projects/ui.
import { Component, EventEmitter, InjectionToken, Input, Output } from '@angular/core';

export const API_URL = new InjectionToken<string>('API_URL');

@Component({
  selector: 'ui-campo',
  standalone: true,
  template: '',
})
export class CampoComponent {
  @Input() rotulo = '';
  @Input() valor = '';
  @Input() tipo = 'text';
  @Output() valorChange = new EventEmitter<string>();
}

@Component({
  selector: 'ui-periodo',
  standalone: true,
  template: '',
})
export class PeriodoComponent {
  @Input() inicio = '';
  @Input() fim = '';
  @Input() tipo = 'text';
  @Output() inicioChange = new EventEmitter<string>();
  @Output() fimChange = new EventEmitter<string>();
}

@Component({
  selector: 'ui-estado-vazio',
  standalone: true,
  template: '',
})
export class EstadoVazioComponent {
  @Input() mensagem = '';
}
