import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export function papelEhGestao(papel: string | null): boolean {
  return papel?.toLowerCase() === 'gestao';
}

export const gestaoGuard: CanActivateFn = () => {
  if (papelEhGestao(sessionStorage.getItem('koinonia.papel'))) {
    return true;
  }
  return inject(Router).createUrlTree(['/ponto']);
};
