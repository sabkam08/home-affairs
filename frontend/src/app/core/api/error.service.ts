import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiError } from './api-error';
import { ProblemDetails } from './problem-details';

@Injectable({
  providedIn: 'root'
})
export class ErrorService {
  fromHttpError(error: HttpErrorResponse): ApiError {
    const problem = this.readProblemDetails(error);
    const status = problem?.status ?? error.status;

    return {
      status,
      title: problem?.title ?? this.defaultTitle(status),
      message: problem?.detail ?? this.defaultMessage(status),
      correlationId: problem?.correlationId,
      validationErrors: this.normalizeValidationErrors(problem?.errors),
      problem
    };
  }

  private readProblemDetails(
    error: HttpErrorResponse
  ): ProblemDetails | undefined {
    if (!error.error || typeof error.error !== 'object') {
      return undefined;
    }

    return error.error as ProblemDetails;
  }

  private normalizeValidationErrors(
    errors?: Record<string, string[] | string>
  ): Record<string, string[]> {
    if (!errors) {
      return {};
    }

    return Object.fromEntries(
      Object.entries(errors).map(([key, value]) => [
        key,
        Array.isArray(value) ? value : [value]
      ])
    );
  }

  private defaultTitle(status: number): string {
    switch (status) {
      case 400:
        return 'Validation failed';
      case 401:
        return 'Authentication required';
      case 403:
        return 'Permission denied';
      case 404:
        return 'Resource not found';
      case 409:
        return 'State conflict';
      case 500:
        return 'Server error';
      default:
        return 'Request failed';
    }
  }

  private defaultMessage(status: number): string {
    switch (status) {
      case 400:
        return 'One or more values were invalid.';
      case 401:
        return 'Your session is no longer valid. Please sign in again.';
      case 403:
        return 'You do not have permission to perform this action.';
      case 404:
        return 'The requested resource could not be found.';
      case 409:
        return 'The resource has changed. Reload the latest state before trying again.';
      case 500:
        return 'An unexpected server error occurred.';
      default:
        return 'The request could not be completed.';
    }
  }
}