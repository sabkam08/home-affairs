import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { API_CONFIG } from '../../../core/api/api-config';
import { CitizenProfileResponse } from './profile.models';

@Injectable({ providedIn: 'root' })
export class ProfileApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(API_CONFIG);

  getMine() {
    return this.http.get<CitizenProfileResponse>(
      `${this.config.baseUrl}/citizens/me`
    );
  }
}
