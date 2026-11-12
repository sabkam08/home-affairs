import {
  Component,
  inject,
  signal
} from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import { ApplicationApiService } from '../services/application-api.service';
import { APPLICATION_TYPES } from '../models/application.models';
import { ErrorService } from '../../../core/api/error.service';
import { ApiError } from '../../../core/api/api-error';

@Component({
  selector: 'app-create-application',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="max-w-3xl">
      <h1 class="text-3xl font-semibold text-slate-950">
        New application
      </h1>

      <p class="mt-2 text-slate-600">
        Select the Home Affairs service you want to apply for.
      </p>

      @if (error()) {
        <div class="mt-6 rounded-xl border border-red-200
                    bg-red-50 p-4 text-sm text-red-800">
          {{ error()?.message }}
        </div>
      }

      <form
        [formGroup]="form"
        (ngSubmit)="submit()"
        class="mt-8 space-y-6 rounded-2xl border
               border-slate-200 bg-white p-6">

        <div>
          <label class="block text-sm font-medium text-slate-700">
            Service
          </label>

          <select
            formControlName="applicationType"
            class="mt-2 w-full rounded-xl border border-slate-300
                   px-4 py-3">
            <option value="">
              Select a service
            </option>

            @for (type of applicationTypes; track type.code) {
              <option [value]="type.code">
                {{ type.label }}
              </option>
            }
          </select>
        </div>

        <div class="rounded-xl bg-blue-50 p-4 text-sm text-blue-900">
          This first application flow creates an application for
          yourself. Dependant/represented-citizen selection is added
          in Frontend 2B.
        </div>

        <button
          type="submit"
          [disabled]="loading()"
          class="rounded-xl bg-blue-700 px-5 py-3
                 font-semibold text-white disabled:opacity-50">
          {{ loading() ? 'Creating...' : 'Create draft' }}
        </button>
      </form>
    </div>
  `
})
export class CreateApplicationComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApplicationApiService);
  private readonly router = inject(Router);
  private readonly errorService = inject(ErrorService);

  readonly applicationTypes = APPLICATION_TYPES;
  readonly loading = signal(false);
  readonly error = signal<ApiError | null>(null);

  readonly form = this.fb.nonNullable.group({
    applicationType: [
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

    this.api.create({
      applicationType:
        this.form.controls.applicationType.value,
      subjectCitizenId: null
    })
    .pipe(
      finalize(() => this.loading.set(false))
    )
    .subscribe({
      next: application => {
        void this.router.navigate([
          '/applications',
          application.applicationId
        ]);
      },
      error: error => {
        this.error.set(
          this.errorService.fromHttpError(error)
        );
      }
    });
  }
}