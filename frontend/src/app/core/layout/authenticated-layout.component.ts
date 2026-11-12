import {
  Component,
  inject
} from '@angular/core';
import {
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';

import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-authenticated-layout',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive
  ],
  template: `
    <div class="min-h-screen bg-slate-50">
      <header class="border-b border-slate-200 bg-white">
        <div
          class="mx-auto flex max-w-7xl items-center justify-between
                 px-6 py-4">

          <a
            routerLink="/applications"
            class="flex items-center gap-3">
            <div
              class="flex h-10 w-10 items-center justify-center
                     rounded-xl bg-blue-700 font-bold text-white">
              HA
            </div>

            <div>
              <p class="font-semibold text-slate-950">
                Home Affairs
              </p>
              <p class="text-xs text-slate-500">
                Application Portal
              </p>
            </div>
          </a>

          <div class="flex items-center gap-4">
            @if (auth.currentUser(); as user) {
              <div class="hidden text-right sm:block">
                <p class="text-sm font-medium text-slate-800">
                  {{ user.email || 'Signed in user' }}
                </p>

                <p class="text-xs text-slate-500">
                  {{ user.role || 'Authenticated' }}
                </p>
              </div>
            }

            <button
              type="button"
              (click)="logout()"
              class="rounded-lg border border-slate-300
                     px-4 py-2 text-sm font-medium text-slate-700
                     hover:bg-slate-100">
              Sign out
            </button>
          </div>
        </div>
      </header>

      <div class="mx-auto grid max-w-7xl lg:grid-cols-[240px_1fr]">
        <aside
          class="border-r border-slate-200 bg-white
                 px-4 py-6 lg:min-h-[calc(100vh-73px)]">

          <nav class="space-y-1">
            <a
              routerLink="/applications"
              routerLinkActive="bg-blue-50 text-blue-700"
              [routerLinkActiveOptions]="{ exact: false }"
              class="block rounded-lg px-4 py-3 text-sm
                     font-medium text-slate-700 hover:bg-slate-100">
              Applications
            </a>

            @if (
              auth.hasAnyPermission(
                'APPLICATION.PROCESS',
                'APPLICATION.READ_ALL'
              )
            ) {
              <a
                routerLink="/work"
                routerLinkActive="bg-blue-50 text-blue-700"
                class="block rounded-lg px-4 py-3 text-sm
                       font-medium text-slate-700 hover:bg-slate-100">
                Work queue
              </a>
            }

            @if (auth.hasPermission('APPLICATION.APPROVE')) {
              <a
                routerLink="/approvals"
                routerLinkActive="bg-blue-50 text-blue-700"
                class="block rounded-lg px-4 py-3 text-sm
                       font-medium text-slate-700 hover:bg-slate-100">
                Approvals
              </a>
            }

            @if (auth.hasPermission('APPLICATION.READ_ALL')) {
              <a
                routerLink="/search"
                routerLinkActive="bg-blue-50 text-blue-700"
                class="block rounded-lg px-4 py-3 text-sm
                       font-medium text-slate-700 hover:bg-slate-100">
                Search
              </a>
            }
          </nav>
        </aside>

        <main class="min-w-0 p-6 lg:p-8">
          <router-outlet />
        </main>
      </div>
    </div>
  `
})
export class AuthenticatedLayoutComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  logout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}