import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { API_URL } from 'ui';

import { apiUrl } from './api-url';
import { routes } from './app.routes';
import { cabecalhosInterceptor } from './cabecalhos.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([cabecalhosInterceptor])),
    { provide: API_URL, useValue: apiUrl },
  ]
};
