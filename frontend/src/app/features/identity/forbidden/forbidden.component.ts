import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-forbidden',
  standalone: true,
  imports: [RouterLink],
  template: `
    <main class="flex min-h-screen items-center justify-center
                 bg-slate-50 px-6">
      <div class="max-w-lg text-center">
        <p class="text-sm font-semibold uppercase tracking-widest
                  text-red-600">
          403
        </p>

        <h1 class="mt-3 text-4xl font-semibold text-slate-950">
          Permission denied
        </h1>

        <p class="mt-4 text-slate-600">
          Your account does not have permission to access this
          functionality.
        </p>

        <a
          routerLink="/applications"
          class="mt-8 inline-flex rounded-xl bg-blue-700
                 px-5 py-3 font-medium text-white hover:bg-blue-800">
          Return to portal
        </a>
      </div>
    </main>
  `
})
export class ForbiddenComponent {}