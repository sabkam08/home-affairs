import { Routes } from '@angular/router';
import { authenticatedGuard } from './core/auth/auth.guard';
import { permissionGuard } from './core/auth/permission.guard';
import { AuthenticatedLayoutComponent } from './core/layout/authenticated-layout.component';
import { LoginComponent } from './features/identity/login/login.component';
import { ForbiddenComponent } from './features/identity/forbidden/forbidden.component';
import { NotFoundComponent } from './features/identity/not-found/not-found.component';
import { MyApplicationsComponent } from './features/applications/pages/my-applications.component';
import { CreateApplicationComponent } from './features/applications/pages/create-application.component';
import { ApplicationDetailsComponent } from './features/applications/pages/application-details.component';
import { ProfileComponent } from './features/identity/profile/profile.component';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
    title: 'Sign in | Home Affairs'
  },
  {
    path: '',
    component: AuthenticatedLayoutComponent,
    canActivate: [authenticatedGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'applications'
      },
      {
        path: 'applications',
        component: MyApplicationsComponent,
        title: 'My applications | Home Affairs'
      },
      {
        path: 'applications/new',
        component: CreateApplicationComponent,
        canActivate: [permissionGuard],
        data: { permission: 'APPLICATION.CREATE' },
        title: 'New application | Home Affairs'
      },
      {
        path: 'applications/:applicationId',
        component: ApplicationDetailsComponent,
        title: 'Application details | Home Affairs'
      },
      {
        path: 'profile',
        component: ProfileComponent,
        title: 'My profile | Home Affairs'
      }
    ]
  },
  {
    path: 'forbidden',
    component: ForbiddenComponent,
    title: 'Permission denied | Home Affairs'
  },
  {
    path: 'not-found',
    component: NotFoundComponent,
    title: 'Not found | Home Affairs'
  },
  {
    path: '**',
    redirectTo: 'not-found'
  }
];
