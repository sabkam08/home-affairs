import { inject } from '@angular/core';
import {
  CanActivateFn,
  Router
} from '@angular/router';
import {
  catchError,
  map,
  of
} from 'rxjs';

import { AuthService } from './auth.service';

export const authenticatedGuard: CanActivateFn = (
  _route,
  state
) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.authenticated()) {
    return router.createUrlTree(
      ['/login'],
      {
        queryParams: {
          returnUrl: state.url
        }
      }
    );
  }

  if (auth.currentUser()) {
    return true;
  }

  return auth.initializeAuthenticatedUser().pipe(
    map(initialized =>
      initialized
        ? true
        : router.createUrlTree(
            ['/login'],
            {
              queryParams: {
                returnUrl: state.url
              }
            }
          )
    ),
    catchError(() =>
      of(
        router.createUrlTree(['/login'])
      )
    )
  );
};