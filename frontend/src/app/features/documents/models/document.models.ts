export interface DocumentResponse {
  documentId: string;
  logicalDocumentId: string;
  versionNumber: number;
  applicationId: string;
  documentType: string;
  fileName: string;
  contentType: string;
  fileExtension: string;
  fileSizeBytes: number;
  contentHash: string;
  uploadedByUserId: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  rowVersion: string;
}

export interface DocumentRequirementStatusResponse {
  documentType: string;
  displayName: string;
  description: string;
  minimumCount: number;
  uploadedCount: number;
  satisfied?: boolean;
  isSatisfied?: boolean;
}

export interface DocumentReadinessResponse {
  applicationId: string;
  isReady: boolean;
  requiredCount: number;
  satisfiedCount: number;
  requirements: DocumentRequirementStatusResponse[];
}

export const DOCUMENT_TYPES = [
  'IDENTITY_DOCUMENT',
  'BIRTH_CERTIFICATE',
  'PROOF_OF_ADDRESS',
  'AFFIDAVIT',
  'SUPPORTING_DOCUMENT',
  'GUARDIAN_PROOF'
] as const;

export type DocumentType =
  typeof DOCUMENT_TYPES[number];

export const ACCEPTED_DOCUMENT_TYPES = [
  'application/pdf',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'image/jpeg',
  'image/png'
] as const;

export const MAX_DOCUMENT_SIZE_BYTES =
  15 * 1024 * 1024;

export function requirementSatisfied(
  requirement: DocumentRequirementStatusResponse
): boolean {
  return requirement.satisfied ?? requirement.isSatisfied ?? false;
}

export function documentTypeLabel(documentType: string): string {
  return documentType
    .toLowerCase()
    .split('_')
    .map(word => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ');
}