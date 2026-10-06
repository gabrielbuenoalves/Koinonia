import { Routes } from '@angular/router';
import { PlanejamentoComponent } from 'planejamento';
import { FechamentoComponent, PontoComponent } from 'ponto';
import { gestaoGuard } from './gestao.guard';

export const routes: Routes = [
  { path: 'planejamento', component: PlanejamentoComponent, canActivate: [gestaoGuard] },
  { path: 'ponto', component: PontoComponent },
  { path: 'fechamento', component: FechamentoComponent, canActivate: [gestaoGuard] },
];
