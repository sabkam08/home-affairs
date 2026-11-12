import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { API_CONFIG } from '../../../core/api/api-config';
import {
  ApplicationResponse,
  ApplicationStatusHistoryResponse,
  CancelApplicationRequest,
  CreateApplicationRequest,
  SubmitApplicationRequest,
  UpdateApplicationRequest
} from '../models/application.models';

@Injectable({
  providedIn: 'root'
})
export class ApplicationApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(API_CONFIG);

  getMine() {
    return this.http.get<ApplicationResponse[]>(
      `${this.config.baseUrl}/applications/mine`
    );
  }

  getById(applicationId: string) {
    return this.http.get<ApplicationResponse>(
      `${this.config.baseUrl}/applications/${applicationId}`
    );
  }

  getHistory(applicationId: string) {
    return this.http.get<ApplicationStatusHistoryResponse[]>(
      `${this.config.baseUrl}/applications/${applicationId}/history`
    );
  }

  create(request: CreateApplicationRequest) {
    return this.http.post<ApplicationResponse>(
      `${this.config.baseUrl}/applications`,
      request
    );
  }

  update(
    applicationId: string,
    request: UpdateApplicationRequest
  ) {
    return this.http.put<ApplicationResponse>(
      `${this.config.baseUrl}/applications/${applicationId}`,
      request
    );
  }

  submit(
    applicationId: string,
    rowVersion: string
  ) {
    const request: SubmitApplicationRequest = {
      rowVersion
    };

    return this.http.post<ApplicationResponse>(
      `${this.config.baseUrl}/applications/${applicationId}/submit`,
      request
    );
  }

  cancel(
    applicationId: string,
    rowVersion: string,
    reason: string
  ) {
    const request: CancelApplicationRequest = {
      rowVersion,
      reason
    };

    return this.http.post<ApplicationResponse>(
      `${this.config.baseUrl}/applications/${applicationId}/cancel`,
      request
    );
  }
}