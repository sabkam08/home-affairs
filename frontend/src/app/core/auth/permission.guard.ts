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

export const permissionGuard: CanActivateFn = route => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const requiredPermission =
    route.data['permission'] as string | undefined;

  if (!requiredPermission) {
    return true;
  }

  if (auth.hasPermission(requiredPermission)) {
    return true;
  }

  if (auth.currentUser()) {
    return router.createUrlTree(['/forbidden']);
  }

  return auth.initializeAuthenticatedUser().pipe(
    map(() =>
      auth.hasPermission(requiredPermission)
        ? true
        : router.createUrlTree(['/forbidden'])
    ),
    catchError(() =>
      of(router.createUrlTree(['/forbidden']))
    )
  );
};