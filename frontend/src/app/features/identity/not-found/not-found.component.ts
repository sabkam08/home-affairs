import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterLink],
  template: `
    <main class="flex min-h-screen items-center justify-center
                 bg-slate-50 px-6">
      <div class="max-w-lg text-center">
        <p class="text-sm font-semibold uppercase tracking-widest
                  text-slate-500">
          404
        </p>

        <h1 class="mt-3 text-4xl font-semibold text-slate-950">
          Resource not found
        </h1>

        <p class="mt-4 text-slate-600">
          The requested resource could not be found or is not
          available to your account.
        </p>

        <a
          routerLink="/applications"
          class="mt-8 inline-flex rounded-xl bg-blue-700
                 px-5 py-3 font-medium text-white">
          Return to portal
        </a>
      </div>
    </main>
  `
})
export class NotFoundComponent {}