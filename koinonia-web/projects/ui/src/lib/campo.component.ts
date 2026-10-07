import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'ui-campo',
  standalone: true,
  template: `
    <label class="flex flex-col gap-1 text-secundario">
      {{ rotulo }}
      <input class="rounded-xl border border-trilho bg-campo px-3 py-2 text-texto" [attr.type]="tipo" [value]="valor" (input)="aoDigitar($event)" />
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
