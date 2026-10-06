import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const gestaoGuard: CanActivateFn = () => {
  const papel = sessionStorage.getItem('koinonia.papel');
  if (papel?.toLowerCase() === 'gestao') {
    return true;
  }
  return inject(Router).createUrlTree(['/ponto']);
};
