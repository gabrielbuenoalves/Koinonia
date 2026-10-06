import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { sessaoValida } from './sessao';

export const sessaoGuard: CanActivateFn = () => {
  if (sessaoValida(sessionStorage.getItem('koinonia.papel'), sessionStorage.getItem('koinonia.funcionarioId'))) {
    return true;
  }
  return inject(Router).createUrlTree(['/entrar']);
};
