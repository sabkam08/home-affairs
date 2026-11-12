export interface CitizenProfileResponse {
  citizenId: string;
  userId?: string;
  southAfricanIdNumber?: string | null;
  firstName: string;
  middleNames?: string | null;
  lastName: string;
  dateOfBirth?: string | null;
  sex?: string | null;
  isActive?: boolean;
  createdAtUtc?: string;
  updatedAtUtc?: string | null;
  rowVersion?: string;
}
