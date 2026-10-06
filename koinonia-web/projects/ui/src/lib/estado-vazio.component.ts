import { Component, Input } from '@angular/core';

@Component({
  selector: 'ui-estado-vazio',
  standalone: true,
  template: `<p class="text-secundario">{{ mensagem }}</p>`,
})
export class EstadoVazioComponent {
  @Input() mensagem = '';
}
