import {
  HttpHandlerFn,
  HttpRequest
} from '@angular/common/http';
import { inject } from '@angular/core';

import { API_CONFIG } from '../api/api-config';
import { AuthService } from './auth.service';

export function authInterceptor(
  request: HttpRequest<unknown>,
  next: HttpHandlerFn
) {
  const auth = inject(AuthService);
  const config = inject(API_CONFIG);

  if (!request.url.startsWith(config.baseUrl)) {
    return next(request);
  }

  const token = auth.accessToken();

  if (!token) {
    return next(request);
  }

  return next(
    request.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    })
  );
}