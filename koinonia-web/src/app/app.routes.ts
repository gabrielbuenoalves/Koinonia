import { Routes } from '@angular/router';
import { PlanejamentoComponent } from 'planejamento';
import { FechamentoComponent, PontoComponent } from 'ponto';
import { EntradaComponent } from './entrada.component';
import { gestaoGuard } from './gestao.guard';
import { sessaoGuard } from './sessao.guard';
import { VisaoGeralComponent } from './visao-geral.component';

export const routes: Routes = [
  { path: 'entrar', component: EntradaComponent },
  { path: '', component: VisaoGeralComponent, canActivate: [sessaoGuard] },
  { path: 'planejamento', component: PlanejamentoComponent, canActivate: [sessaoGuard, gestaoGuard] },
  { path: 'ponto', component: PontoComponent, canActivate: [sessaoGuard] },
  { path: 'fechamento', component: FechamentoComponent, canActivate: [sessaoGuard, gestaoGuard] },
];
