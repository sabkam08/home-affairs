import {
  Component,
  inject,
  signal
} from '@angular/core';
import {
  DatePipe
} from '@angular/common';
import {
  RouterLink
} from '@angular/router';

import { ApplicationApiService } from '../services/application-api.service';
import {
  ApplicationResponse,
  applicationTypeLabel
} from '../models/application.models';

@Component({
  selector: 'app-my-applications',
  standalone: true,
  imports: [
    DatePipe,
    RouterLink
  ],
  template: `
    <div class="flex flex-wrap items-start justify-between gap-4">
      <div>
        <h1 class="text-3xl font-semibold tracking-tight text-slate-950">
          My applications
        </h1>

        <p class="mt-2 text-slate-600">
          Track and manage your Home Affairs applications.
        </p>
      </div>

      <a
        routerLink="/applications/new"
        class="rounded-xl bg-blue-700 px-5 py-3
               font-medium text-white hover:bg-blue-800">
        New application
      </a>
    </div>

    @if (loading()) {
      <div class="mt-8 rounded-2xl border border-slate-200
                  bg-white p-8 text-slate-500">
        Loading applications...
      </div>
    } @else if (applications().length === 0) {
      <div class="mt-8 rounded-2xl border border-dashed
                  border-slate-300 bg-white p-12 text-center">
        <h2 class="font-semibold text-slate-900">
          No applications yet
        </h2>

        <p class="mt-2 text-sm text-slate-500">
          Create your first Home Affairs application.
        </p>
      </div>
    } @else {
      <div class="mt-8 overflow-hidden rounded-2xl
                  border border-slate-200 bg-white">
        @for (
          application of applications();
          track application.applicationId
        ) {
          <a
            [routerLink]="[
              '/applications',
              application.applicationId
            ]"
            class="grid gap-3 border-b border-slate-100
                   p-5 last:border-0 hover:bg-slate-50
                   md:grid-cols-[1fr_auto]">

            <div>
              <div class="flex flex-wrap items-center gap-3">
                <p class="font-semibold text-slate-950">
                  {{ application.applicationNumber }}
                </p>

                <span
                  class="rounded-full bg-slate-100 px-3 py-1
                         text-xs font-semibold text-slate-700">
                  {{ application.status }}
                </span>
              </div>

              <p class="mt-2 text-sm text-slate-600">
                {{ label(application.applicationType) }}
              </p>
            </div>

            <div class="text-sm text-slate-500 md:text-right">
              Created
              {{ application.createdAtUtc | date:'mediumDate' }}
            </div>
          </a>
        }
      </div>
    }
  `
})
export class MyApplicationsComponent {
  private readonly api = inject(ApplicationApiService);

  readonly applications =
    signal<ApplicationResponse[]>([]);

  readonly loading = signal(true);

  readonly label = applicationTypeLabel;

  constructor() {
    this.api.getMine().subscribe({
      next: applications => {
        this.applications.set(applications);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }
}