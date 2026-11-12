import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners
} from '@angular/core';
import {
  provideHttpClient,
  withInterceptors
} from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { API_CONFIG } from './core/api/api-config';
import { environment } from '../environments/environment';
import { authInterceptor } from './core/auth/auth.interceptor';
import { errorInterceptor } from './core/auth/error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([
        authInterceptor,
        errorInterceptor
      ])
    ),
    {
      provide: API_CONFIG,
      useValue: {
        baseUrl: environment.apiBaseUrl
      }
    }
  ]
};
