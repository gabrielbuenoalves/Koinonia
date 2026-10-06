import { HttpInterceptorFn } from '@angular/common/http';
import { apiUrl } from './api-url';
import { papelHttp } from './sessao';

export const cabecalhosInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(apiUrl)) {
    return next(req);
  }

  return next(
    req.clone({
      setHeaders: {
        'X-Papel': papelHttp(sessionStorage.getItem('koinonia.papel')),
        'X-Funcionario-Id': sessionStorage.getItem('koinonia.funcionarioId') ?? '',
      },
    }),
  );
};
