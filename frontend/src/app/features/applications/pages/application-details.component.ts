import {
  Component,
  inject,
  signal
} from '@angular/core';
import {
  DatePipe
} from '@angular/common';
import {
  ActivatedRoute,
  Router
} from '@angular/router';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';

import { ApplicationApiService } from '../services/application-api.service';
import {
  ApplicationResponse,
  applicationTypeLabel
} from '../models/application.models';
import { ErrorService } from '../../../core/api/error.service';
import { ApiError } from '../../../core/api/api-error';

@Component({
  selector: 'app-application-details',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule
  ],
  template: `
    @if (loading()) {
      <p class="text-slate-500">
        Loading application...
      </p>
    } @else if (application(); as item) {
      <div class="flex flex-wrap items-start
                  justify-between gap-4">
        <div>
          <p class="text-sm font-medium uppercase
                    tracking-wider text-blue-700">
            {{ item.applicationNumber }}
          </p>

          <h1 class="mt-2 text-3xl font-semibold text-slate-950">
            {{ label(item.applicationType) }}
          </h1>
        </div>

        <span
          class="rounded-full bg-slate-200 px-4 py-2
                 text-sm font-semibold text-slate-800">
          {{ item.status }}
        </span>
      </div>

      @if (error()) {
        <div class="mt-6 rounded-xl border p-4"
             [class.border-amber-300]="error()?.status === 409"
             [class.bg-amber-50]="error()?.status === 409"
             [class.border-red-200]="error()?.status !== 409"
             [class.bg-red-50]="error()?.status !== 409">

          <p class="font-medium">
            {{ error()?.title }}
          </p>

          <p class="mt-1 text-sm">
            {{ error()?.message }}
          </p>

          @if (error()?.status === 409) {
            <button
              type="button"
              (click)="reload()"
              class="mt-3 rounded-lg bg-slate-900
                     px-4 py-2 text-sm text-white">
              Reload latest state
            </button>
          }
        </div>
      }

      <div class="mt-8 grid gap-6 lg:grid-cols-3">
        <section
          class="rounded-2xl border border-slate-200
                 bg-white p-6 lg:col-span-2">

          <h2 class="font-semibold text-slate-950">
            Application summary
          </h2>

          <dl class="mt-5 grid gap-5 sm:grid-cols-2">
            <div>
              <dt class="text-xs uppercase text-slate-500">
                Created
              </dt>
              <dd class="mt-1 text-sm text-slate-900">
                {{ item.createdAtUtc | date:'medium' }}
              </dd>
            </div>

            <div>
              <dt class="text-xs uppercase text-slate-500">
                Submitted
              </dt>
              <dd class="mt-1 text-sm text-slate-900">
                {{
                  item.submittedAtUtc
                    ? (item.submittedAtUtc | date:'medium')
                    : 'Not submitted'
                }}
              </dd>
            </div>

            <div>
              <dt class="text-xs uppercase text-slate-500">
                Application type
              </dt>
              <dd class="mt-1 text-sm text-slate-900">
                {{ item.applicationType }}
              </dd>
            </div>

            <div>
              <dt class="text-xs uppercase text-slate-500">
                Applicant
              </dt>
              <dd class="mt-1 text-sm text-slate-900">
                {{
                  item.isSelfApplication
                    ? 'Applying for myself'
                    : 'Represented citizen'
                }}
              </dd>
            </div>
          </dl>
        </section>

        <section
          class="rounded-2xl border border-slate-200
                 bg-white p-6">

          <h2 class="font-semibold text-slate-950">
            Actions
          </h2>

          @if (item.status === 'DRAFT') {
            <div class="mt-5 space-y-3">
              <button
                type="button"
                (click)="submitApplication()"
                [disabled]="mutating()"
                class="w-full rounded-xl bg-blue-700
                       px-4 py-3 font-medium text-white
                       disabled:opacity-50">
                Submit application
              </button>

              <textarea
                [(ngModel)]="cancelReason"
                rows="3"
                placeholder="Reason for cancellation"
                class="w-full rounded-xl border border-slate-300
                       px-3 py-2 text-sm">
              </textarea>

              <button
                type="button"
                (click)="cancelApplication()"
                [disabled]="mutating() || !cancelReason.trim()"
                class="w-full rounded-xl border border-red-300
                       px-4 py-3 font-medium text-red-700
                       disabled:opacity-50">
                Cancel application
              </button>
            </div>
          } @else {
            <p class="mt-4 text-sm text-slate-500">
              This application is no longer editable as a draft.
            </p>
          }
        </section>
      </div>

      <section class="mt-6">
        <h2 class="text-xl font-semibold text-slate-950">
          Documents
        </h2>

        <p class="mt-2 text-sm text-slate-500">
          Document management is provided by Frontend 1C below.
        </p>
      </section>
    }
  `
})
export class ApplicationDetailsComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(ApplicationApiService);
  private readonly errorService = inject(ErrorService);

  readonly application =
    signal<ApplicationResponse | null>(null);

  readonly loading = signal(true);
  readonly mutating = signal(false);
  readonly error = signal<ApiError | null>(null);

  readonly label = applicationTypeLabel;

  cancelReason = '';

  private readonly applicationId =
    this.route.snapshot.paramMap.get('applicationId')!;

  constructor() {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.getById(this.applicationId)
      .pipe(
        finalize(() => this.loading.set(false))
      )
      .subscribe({
        next: application =>
          this.application.set(application),

        error: error => {
          if (error.status === 404) {
            void this.router.navigate(['/not-found']);
            return;
          }

          this.error.set(
            this.errorService.fromHttpError(error)
          );
        }
      });
  }

  submitApplication(): void {
    const application = this.application();

    if (!application) {
      return;
    }

    this.mutating.set(true);
    this.error.set(null);

    this.api.submit(
      application.applicationId,
      application.rowVersion
    )
    .pipe(
      finalize(() => this.mutating.set(false))
    )
    .subscribe({
      next: response => {
        // IMPORTANT:
        // replace the complete object so we retain
        // the NEW server rowVersion.
        this.application.set(response);
      },
      error: error => {
        this.error.set(
          this.errorService.fromHttpError(error)
        );
      }
    });
  }

  cancelApplication(): void {
    const application = this.application();
    const reason = this.cancelReason.trim();

    if (!application || !reason) {
      return;
    }

    this.mutating.set(true);
    this.error.set(null);

    this.api.cancel(
      application.applicationId,
      application.rowVersion,
      reason
    )
    .pipe(
      finalize(() => this.mutating.set(false))
    )
    .subscribe({
      next: response => {
        this.application.set(response);
        this.cancelReason = '';
      },
      error: error => {
        this.error.set(
          this.errorService.fromHttpError(error)
        );
      }
    });
  }
}