import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export function papelEhGestao(papel: string | null): boolean {
  return papel?.toLowerCase() === 'gestao';
}

export function rotaDeGestao(url: string): boolean {
  const caminho = url.split(/[?#]/)[0];
  return caminho === '/planejamento' || caminho === '/fechamento';
}

export const gestaoGuard: CanActivateFn = () => {
  if (papelEhGestao(sessionStorage.getItem('koinonia.papel'))) {
    return true;
  }
  return inject(Router).createUrlTree(['/ponto']);
};

export function sairDaGestao(router: Router, papel: string | null): void {
  if (!papelEhGestao(papel) && rotaDeGestao(router.url)) {
    void router.navigateByUrl('/ponto');
  }
}
