import { Component, Input } from '@angular/core';

@Component({
  selector: 'ui-estado-vazio',
  standalone: true,
  template: `<p>{{ mensagem }}</p>`
})
export class EstadoVazioComponent {
  @Input() mensagem = '';
}
