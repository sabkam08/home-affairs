import {
  HttpErrorResponse,
  HttpHandlerFn,
  HttpRequest
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { AuthService } from './auth.service';

export function errorInterceptor(
  request: HttpRequest<unknown>,
  next: HttpHandlerFn
) {
  const auth = inject(AuthService);
  const router = inject(Router);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      if (error.status === 401) {
        auth.logout();

        if (!request.url.endsWith('/auth/login')) {
          void router.navigate(['/login'], {
            queryParams: {
              reason: 'session-expired'
            }
          });
        }
      }

      if (error.status === 403) {
        void router.navigate(['/forbidden']);
      }

      return throwError(() => error);
    })
  );
}