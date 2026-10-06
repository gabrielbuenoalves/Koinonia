import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'ui-campo',
  standalone: true,
  template: `
    <label>
      {{ rotulo }}
      <input [attr.type]="tipo" [value]="valor" (input)="aoDigitar($event)" />
    </label>
  `,
})
export class CampoComponent {
  @Input() rotulo = '';
  @Input() valor = '';
  @Input() tipo = 'text';
  @Output() valorChange = new EventEmitter<string>();

  aoDigitar(evento: Event) {
    this.valorChange.emit((evento.target as HTMLInputElement).value);
  }
}
