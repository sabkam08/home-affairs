import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import {
  ActivatedRoute,
  Router
} from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { ErrorService } from '../../../core/api/error.service';
import { ApiError } from '../../../core/api/api-error';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="min-h-screen bg-[#173b26] lg:grid lg:grid-cols-2">
      <section
        class="hidden lg:flex flex-col justify-between p-12
               bg-gradient-to-br from-[#173b26] via-[#1f5132] to-[#285c3a]
               text-white">

        <div>
          <div class="text-sm font-semibold tracking-[0.25em]
                      uppercase text-[#d6eadb]">
            Home Affairs
          </div>

          <h1 class="mt-8 max-w-xl text-5xl font-semibold
                     leading-tight tracking-tight">
            Digital application management
          </h1>

          <p class="mt-6 max-w-lg text-lg leading-8 text-slate-300">
            Manage citizen applications, supporting documents,
            processing workflows and approvals securely.
          </p>
        </div>

        <p class="text-sm text-slate-400">
          Secure government service portal
        </p>
      </section>

      <section
        class="flex min-h-screen items-center justify-center
               bg-slate-50 px-6 py-12">

        <div class="w-full max-w-md">
          <div class="mb-8 lg:hidden">
            <p class="font-semibold tracking-widest text-[#1f5132] uppercase">
              Home Affairs
            </p>
          </div>

          <div class="rounded-3xl border border-slate-200
                      bg-white p-8 shadow-xl shadow-[#173b26]/15">

            <h2 class="text-3xl font-semibold tracking-tight text-slate-950">
              Sign in
            </h2>

            <p class="mt-2 text-sm text-slate-500">
              Use your Home Affairs account to continue.
            </p>

            @if (sessionExpired()) {
              <div
                class="mt-6 rounded-xl border border-amber-200
                       bg-amber-50 px-4 py-3 text-sm text-amber-800">
                Your session has expired. Please sign in again.
              </div>
            }

            @if (error()) {
              <div
                class="mt-6 rounded-xl border border-red-200
                       bg-red-50 px-4 py-3">

                <p class="font-medium text-red-900">
                  {{ error()?.title }}
                </p>

                <p class="mt-1 text-sm text-red-700">
                  {{ error()?.message }}
                </p>

                @if (error()?.correlationId) {
                  <p class="mt-2 text-xs text-red-600">
                    Correlation ID:
                    {{ error()?.correlationId }}
                  </p>
                }
              </div>
            }

            <form
              class="mt-8 space-y-5"
              [formGroup]="form"
              (ngSubmit)="submit()">

              <div>
                <label
                  for="email"
                  class="block text-sm font-medium text-slate-700">
                  Email address
                </label>

                <input
                  id="email"
                  type="email"
                  autocomplete="username"
                  formControlName="email"
                  class="mt-2 w-full rounded-xl border border-slate-300
                         bg-white px-4 py-3 outline-none
                         focus:border-[#2f7b4b] focus:ring-4
                         focus:ring-[#d6eadb]">

                @if (
                  form.controls.email.touched &&
                  form.controls.email.invalid
                ) {
                  <p class="mt-2 text-sm text-red-600">
                    Enter a valid email address.
                  </p>
                }
              </div>

              <div>
                <label
                  for="password"
                  class="block text-sm font-medium text-slate-700">
                  Password
                </label>

                <input
                  id="password"
                  type="password"
                  autocomplete="current-password"
                  formControlName="password"
                  class="mt-2 w-full rounded-xl border border-slate-300
                         bg-white px-4 py-3 outline-none
                         focus:border-[#2f7b4b] focus:ring-4
                         focus:ring-[#d6eadb]">

                @if (
                  form.controls.password.touched &&
                  form.controls.password.invalid
                ) {
                  <p class="mt-2 text-sm text-red-600">
                    Password is required.
                  </p>
                }
              </div>

              <button
                type="submit"
                [disabled]="loading()"
                class="w-full rounded-xl bg-[#1f5132] px-4 py-3
                       font-semibold text-white hover:bg-[#285c3a]
                       disabled:opacity-50">

                @if (loading()) {
                  Signing in...
                } @else {
                  Sign in
                }
              </button>
            </form>
          </div>
        </div>
      </section>
    </main>
  `
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly errorService = inject(ErrorService);

  readonly loading = signal(false);
  readonly error = signal<ApiError | null>(null);

  readonly sessionExpired = signal(
    this.route.snapshot.queryParamMap.get('reason') ===
    'session-expired'
  );

  readonly form = this.fb.nonNullable.group({
    email: [
      '',
      [
        Validators.required,
        Validators.email
      ]
    ],
    password: [
      '',
      Validators.required
    ]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.auth
      .login(this.form.getRawValue())
      .pipe(
        finalize(() => this.loading.set(false))
      )
      .subscribe({
        next: () => {
          this.auth.initializeAuthenticatedUser()
            .subscribe({
              next: initialized => {
                if (!initialized) {
                  return;
                }

                const returnUrl =
                  this.route.snapshot.queryParamMap.get(
                    'returnUrl'
                  ) ?? '/applications';

                void this.router.navigateByUrl(returnUrl);
              }
            });
        },
        error: error => {
          this.error.set(
            this.errorService.fromHttpError(error)
          );
        }
      });
  }
}