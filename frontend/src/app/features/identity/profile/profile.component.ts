import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { ErrorService } from '../../../core/api/error.service';
import { ApiError } from '../../../core/api/api-error';
import { ProfileApiService } from './profile-api.service';
import { CitizenProfileResponse } from './profile.models';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [DatePipe, RouterLink],
  template: `
    <div class="max-w-3xl">
      <a routerLink="/applications" class="text-sm font-medium text-[#2f7b4b] hover:underline">
        ← Back to applications
      </a>
      <div class="mt-5">
        <p class="text-sm font-semibold uppercase tracking-wider text-[#2f7b4b]">Account</p>
        <h1 class="mt-2 text-3xl font-semibold tracking-tight text-slate-950">My profile</h1>
        <p class="mt-2 text-slate-600">Your verified citizen information used for applications.</p>
      </div>

      @if (loading()) {
        <div class="mt-8 rounded-2xl border border-[#d8d2c4] bg-white p-8 text-slate-500">Loading profile...</div>
      } @else if (error(); as currentError) {
        <div class="mt-8 rounded-2xl border border-red-200 bg-red-50 p-5 text-red-800">
          <p class="font-medium">{{ currentError.title }}</p>
          <p class="mt-1 text-sm">{{ currentError.message }}</p>
          <button type="button" (click)="load()" class="mt-4 rounded-lg bg-[#1f5132] px-4 py-2 text-sm font-medium text-white">Try again</button>
        </div>
      } @else if (profile(); as citizen) {
        <section class="mt-8 rounded-2xl border border-[#d8d2c4] bg-white p-6 shadow-sm">
          <div class="flex items-center gap-4 border-b border-slate-100 pb-6">
            <div class="flex h-14 w-14 items-center justify-center rounded-full bg-[#d6eadb] text-xl font-bold text-[#1f5132]">
              {{ citizen.firstName.charAt(0) }}{{ citizen.lastName.charAt(0) }}
            </div>
            <div>
              <h2 class="text-xl font-semibold text-slate-950">
                {{ citizen.firstName }} {{ citizen.middleNames ? citizen.middleNames + ' ' : '' }}{{ citizen.lastName }}
              </h2>
              <p class="text-sm text-slate-500">Citizen profile</p>
            </div>
          </div>
          <dl class="mt-6 grid gap-6 sm:grid-cols-2">
            <div><dt class="text-xs font-semibold uppercase tracking-wide text-slate-500">South African ID</dt><dd class="mt-1 text-sm text-slate-900">{{ citizen.southAfricanIdNumber || 'Not provided' }}</dd></div>
            <div><dt class="text-xs font-semibold uppercase tracking-wide text-slate-500">Date of birth</dt><dd class="mt-1 text-sm text-slate-900">{{ citizen.dateOfBirth ? (citizen.dateOfBirth | date:'longDate') : 'Not provided' }}</dd></div>
            <div><dt class="text-xs font-semibold uppercase tracking-wide text-slate-500">Sex</dt><dd class="mt-1 text-sm text-slate-900">{{ citizen.sex || 'Not provided' }}</dd></div>
            <div><dt class="text-xs font-semibold uppercase tracking-wide text-slate-500">Profile status</dt><dd class="mt-1 text-sm text-slate-900">{{ citizen.isActive === false ? 'Inactive' : 'Active' }}</dd></div>
          </dl>
        </section>
      }
    </div>
  `
})
export class ProfileComponent {
  private readonly api = inject(ProfileApiService);
  private readonly errorService = inject(ErrorService);

  readonly profile = signal<CitizenProfileResponse | null>(null);
  readonly loading = signal(true);
  readonly error = signal<ApiError | null>(null);

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getMine().pipe(finalize(() => this.loading.set(false))).subscribe({
      next: profile => this.profile.set(profile),
      error: error => this.error.set(this.errorService.fromHttpError(error))
    });
  }
}
