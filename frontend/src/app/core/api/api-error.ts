import { ProblemDetails } from './problem-details';

export interface ApiError {
  status: number;
  title: string;
  message: string;
  correlationId?: string;
  validationErrors: Record<string, string[]>;
  problem?: ProblemDetails;
}