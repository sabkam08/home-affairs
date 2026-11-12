import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { API_CONFIG } from '../api/api-config';
import {
  CurrentUser,
  LoginRequest,
  LoginResponse,
  PermissionResponse
} from './auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(API_CONFIG);

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(
      `${this.config.baseUrl}/auth/login`,
      request
    );
  }

  getCurrentUser(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>(
      `${this.config.baseUrl}/auth/me`
    );
  }

  getPermissions(): Observable<PermissionResponse> {
    return this.http.get<PermissionResponse>(
      `${this.config.baseUrl}/auth/permissions`
    );
  }
}