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
    <div class="min-h-screen bg-[#f5f1e8]">
      <header class="border-b border-[#285c3a] bg-[#1f5132] text-white">
        <div
          class="mx-auto flex max-w-7xl items-center justify-between
                 px-6 py-4">

          <a
            routerLink="/applications"
            class="flex items-center gap-3">
            <div
              class="flex h-10 w-10 items-center justify-center
                     rounded-xl bg-[#d6eadb] font-bold text-[#1f5132]">
              HA
            </div>

            <div>
              <p class="font-semibold text-white">
                Home Affairs
              </p>

              <p class="text-xs text-[#d6eadb]">
                Citizen services portal
              </p>
            </div>
          </a>

          <div class="flex items-center gap-4">
            @if (auth.currentUser(); as user) {
              <div class="hidden text-right sm:block">
                <p class="text-sm font-medium text-white">
                  {{ user.email || 'Signed in user' }}
                </p>

                <p class="text-xs text-[#d6eadb]">
                  {{ user.role || 'Authenticated' }}
                </p>
              </div>
            }

            <button
              type="button"
              (click)="logout()"
              class="rounded-lg border border-[#8fc5a1]
                     px-4 py-2 text-sm font-medium text-white
                     hover:bg-[#285c3a]">
              Sign out
            </button>
          </div>
        </div>
      </header>

      <div class="mx-auto grid max-w-7xl lg:grid-cols-[240px_1fr]">
        <aside
          class="border-r border-[#285c3a] bg-[#173b26]
                 px-4 py-6 lg:min-h-[calc(100vh-73px)]">

          <nav aria-label="Main navigation" class="space-y-2">
            <a
              routerLink="/applications"
              routerLinkActive="bg-[#d6eadb] text-[#173b26]"
              [routerLinkActiveOptions]="{ exact: false }"
              class="block rounded-lg px-4 py-3 text-sm
                     font-medium text-[#dceee0] hover:bg-[#285c3a]">
              Applications
            </a>

            <a
              routerLink="/profile"
              routerLinkActive="bg-[#d6eadb] text-[#173b26]"
              class="block rounded-lg px-4 py-3 text-sm
                     font-medium text-[#dceee0] hover:bg-[#285c3a]">
              My profile
            </a>
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