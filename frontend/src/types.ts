export type Role = 'IT_SUPERUSER' | 'REVIEWER' | 'EMPLOYEE' | 'CITIZEN';

export interface UserView {
  id: number;
  fullName: string;
  email: string;
  role: Role;
  active: boolean;
  failedAttempts: number;
  lockedUntil: string | null;
  createdAt: string | null;
  lastLoginAt: string | null;
}

export interface FolderView {
  id: number;
  name: string;
  description: string;
  generalAccess: boolean;
  parentFolderId: number | null;
  createdAt: string;
}

export interface DocumentSummary {
  id: number;
  title: string;
  description: string;
  folderId: number;
  folderName: string | null;
  ownerName: string | null;
  status: string;
  fileType: string;
  checksum: string;
  currentVersionNumber: number;
  deleted: boolean;
  updatedAt: string;
  canEdit: boolean;
  canView: boolean;
}

export interface DocumentVersionView {
  id: number;
  documentId: number;
  versionNumber: number;
  fileName: string;
  mimeType: string;
  checksum: string;
  sizeBytes: number;
  createdBy: string | null;
  createdAt: string;
}

export interface CommentView {
  id: number;
  documentId: number;
  authorName: string | null;
  body: string;
  createdAt: string;
}

export interface PermissionView {
  id: number;
  documentId: number;
  userId: number;
  userName: string | null;
  email: string | null;
  canView: boolean;
  canEdit: boolean;
  canComment: boolean;
  canShare: boolean;
  canApprove: boolean;
  grantedAt: string;
}

export interface WorkflowView {
  id: number;
  documentId: number;
  documentTitle: string | null;
  status: string;
  currentStep: string;
  requestedBy: string | null;
  reviewer: string | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface NotificationView {
  id: number;
  userId: number;
  type: string;
  title: string;
  message: string;
  relatedType: string | null;
  relatedId: number | null;
  read: boolean;
  silenced: boolean;
  createdAt: string;
}

export interface CitizenApplicationView {
  id: number;
  applicationNumber: string;
  applicationType: string;
  fullName: string;
  email: string;
  idNumber: string;
  phoneNumber: string;
  notes: string;
  status: string;
  attachedDocumentId: number | null;
  attachedDocumentTitle: string | null;
  submittedAt: string;
  reviewedByUserId: number | null;
  reviewedAt: string | null;
}

export interface DashboardView {
  totalDocuments: number;
  totalFolders: number;
  pendingWorkflows: number;
  unreadNotifications: number;
  citizenApplications: number;
  recentDocuments: DocumentSummary[];
  role: Role;
}

export interface SessionResponse {
  token: string;
  expiresAt: string;
  sessionTimeoutMinutes: number;
  user: UserView;
}

export interface DocumentDetail extends DocumentSummary {
  workflow: WorkflowView | null;
  versions: DocumentVersionView[];
  comments: CommentView[];
  permissions: PermissionView[];
}

export interface ApiList<T> {
  items: T[];
}
