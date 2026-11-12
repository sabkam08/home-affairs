export interface ApplicationResponse {
  applicationId: string;
  applicationNumber: string;
  applicationType: string;
  applicantCitizenId: string;
  subjectCitizenId: string;
  status: string;
  isSelfApplication: boolean;
  submittedAtUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  rowVersion: string;
}

export interface CreateApplicationRequest {
  applicationType: string;
  subjectCitizenId: string | null;
}

export interface UpdateApplicationRequest {
  applicationType: string;
  subjectCitizenId: string | null;
  rowVersion: string;
}

export interface SubmitApplicationRequest {
  rowVersion: string;
}

export interface CancelApplicationRequest {
  rowVersion: string;
  reason: string;
}

export interface ApplicationStatusHistoryResponse {
  applicationStatusHistoryId: string;
  applicationId: string;
  fromStatus: string;
  toStatus: string;
  changedByUserId: string;
  reason: string;
  changedAtUtc: string;
}

export const APPLICATION_TYPES = [
  {
    code: 'BIRTH_REGISTRATION',
    label: 'Birth Registration'
  },
  {
    code: 'BIRTH_CERTIFICATE_REISSUE',
    label: 'Birth Certificate Re-Issue'
  },
  {
    code: 'LATE_BIRTH_REGISTRATION',
    label: 'Late Birth Registration'
  },
  {
    code: 'BIRTH_CERTIFICATE_AMENDMENT',
    label: 'Birth Certificate Amendment'
  },
  {
    code: 'SMART_ID_NEW',
    label: 'New Smart ID'
  },
  {
    code: 'SMART_ID_REPLACEMENT_LOST',
    label: 'Replace Lost Smart ID'
  },
  {
    code: 'SMART_ID_REPLACEMENT_STOLEN',
    label: 'Replace Stolen Smart ID'
  },
  {
    code: 'SMART_ID_REPLACEMENT_DAMAGED',
    label: 'Replace Damaged Smart ID'
  },
  {
    code: 'SMART_ID_CORRECTION',
    label: 'Smart ID Correction'
  }
] as const;

export function applicationTypeLabel(code: string): string {
  return APPLICATION_TYPES.find(
    item => item.code === code
  )?.label ?? code;
}