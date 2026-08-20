# ERD and State Model

## Entity Relationship Diagram

```mermaid
%%{init: {'themeVariables': {'fontSize': '22px', 'fontFamily': 'Arial'}}}%%
erDiagram
  ACCOUNT ||--o| CITIZEN : profile
  ACCOUNT ||--o| PRACTITIONER : profile
  ACCOUNT ||--o{ DOCUMENT : uploads
  ACCOUNT ||--o{ DOCUMENT_VERSION : uploads
  ACCOUNT ||--o{ DOCUMENT_COMMENT : writes
  ACCOUNT ||--o{ DOCUMENT_PERMISSION : receives
  ACCOUNT ||--o{ DOCUMENT_PERMISSION : grants
  ACCOUNT ||--o{ WORKFLOW_STAGE_HISTORY : actionedBy
  ACCOUNT ||--o{ NOTIFICATION : receives
  ACCOUNT ||--o{ NOTIFICATION : triggers
  ACCOUNT ||--o{ FOLDER : owns
  ACCOUNT ||--o{ PASSWORD_RESET_TOKEN : requests

  BRANCH_UNIT ||--o{ PRACTITIONER : contains
  BRANCH_UNIT ||--o{ APPLICATION : processes
  BRANCH_UNIT ||--o{ FOLDER : scopes
  BRANCH_UNIT ||--o{ FOLDER_PERMISSION : grants
  BRANCH_UNIT ||--o{ BRANCH_UNIT : parentOf

  CITIZEN ||--o{ APPLICATION : submits
  PRACTITIONER ||--o{ APPLICATION : assignedTo
  PRACTITIONER ||--o{ WORKFLOW_INSTANCE : assignedTo
  PRACTITIONER ||--o{ WORKFLOW_STAGE_HISTORY : actionedBy

  APPLICATION_TYPE ||--o{ APPLICATION : categorizes
  APPLICATION_TYPE ||--o{ REQUIRED_DOCUMENT_RULE : defines
  APPLICATION_TYPE ||--o{ FOLDER : groups

  DOCUMENT_TYPE ||--o{ DOCUMENT : classifies
  DOCUMENT_TYPE ||--o{ REQUIRED_DOCUMENT_RULE : requiredBy

  APPLICATION ||--o| WORKFLOW_INSTANCE : has
  APPLICATION ||--o{ DOCUMENT : includes
  APPLICATION ||--o{ NOTIFICATION : triggers

  WORKFLOW_STAGE ||--o{ WORKFLOW_INSTANCE : currentStage
  WORKFLOW_STAGE ||--o{ WORKFLOW_STAGE_HISTORY : fromStage
  WORKFLOW_STAGE ||--o{ WORKFLOW_STAGE_HISTORY : toStage
  WORKFLOW_STAGE ||--o{ FOLDER : organizes

  FOLDER ||--o{ DOCUMENT : contains
  FOLDER ||--o{ FOLDER : parentOf
  FOLDER ||--o{ FOLDER_PERMISSION : permits

  DOCUMENT ||--o{ DOCUMENT_VERSION : has
  DOCUMENT ||--o{ DOCUMENT_PERMISSION : sharedWith
  DOCUMENT ||--o{ DOCUMENT_COMMENT : has
  DOCUMENT ||--o{ NOTIFICATION : triggers

  DOCUMENT_COMMENT ||--o{ DOCUMENT_COMMENT : repliesTo
  DOCUMENT_COMMENT ||--o{ NOTIFICATION : triggers

  WORKFLOW_INSTANCE ||--o{ WORKFLOW_STAGE_HISTORY : history

  ACCOUNT {
    int AccountId PK
    string Email
    string PasswordHash
    string Role
    string Status
    boolean IsEmailVerified
    int FailedLoginAttempts
    datetime LockedUntil
    datetime LastLoginAt
    datetime CreatedAt
    datetime UpdatedAt
  }

  CITIZEN {
    int CitizenId PK
    int AccountId FK
    string NationalIdentityNumber
    string GivenNames
    string Surname
    datetime DateOfBirth
    string PhoneNumber
  }

  PRACTITIONER {
    int PractitionerId PK
    int AccountId FK
    int BranchUnitId FK
    string EmployeeNumber
    string GivenNames
    string Surname
    string JobTitle
    boolean IsActive
  }

  BRANCH_UNIT {
    int BranchUnitId PK
    int ParentBranchUnitId FK
    string Name
    string Code
  }

  APPLICATION_TYPE {
    int ApplicationTypeId PK
    string Name
    string Code
    string Description
    boolean IsActive
  }

  DOCUMENT_TYPE {
    int DocumentTypeId PK
    string Name
    string Code
    string Description
    string AllowedExtensions
  }

  REQUIRED_DOCUMENT_RULE {
    int RequiredDocumentRuleId PK
    int ApplicationTypeId FK
    int DocumentTypeId FK
    boolean IsRequired
    boolean AcceptsMultiple
    int SortOrder
    string Description
  }

  APPLICATION {
    int ApplicationId PK
    int CitizenId FK
    int ApplicationTypeId FK
    int ProcessingBranchUnitId FK
    int AssignedPractitionerId FK
    string ReferenceNumber
    string Status
    datetime SubmittedAt
    datetime LastUpdatedAt
    datetime ApprovedAt
    datetime RejectedAt
    datetime CollectionDate
    string RejectionReason
  }

  WORKFLOW_STAGE {
    int WorkflowStageId PK
    string Name
    string Code
    int SequenceOrder
    boolean IsFinalStage
  }

  WORKFLOW_INSTANCE {
    int WorkflowInstanceId PK
    int ApplicationId FK
    int CurrentWorkflowStageId FK
    int AssignedPractitionerId FK
    datetime StartedAt
    datetime LastMovedAt
    datetime CompletedAt
  }

  WORKFLOW_STAGE_HISTORY {
    int WorkflowStageHistoryId PK
    int WorkflowInstanceId FK
    int FromWorkflowStageId FK
    int ToWorkflowStageId FK
    int ActionedByPractitionerId FK
    string ActionType
    string Remarks
    datetime ActionedAt
  }

  FOLDER {
    int FolderId PK
    string Name
    string Code
    string Scope
    int ParentFolderId FK
    int BranchUnitId FK
    int ApplicationTypeId FK
    int WorkflowStageId FK
    int OwnerAccountId FK
    string Description
    boolean IsArchived
  }

  FOLDER_PERMISSION {
    int FolderPermissionId PK
    int FolderId FK
    int BranchUnitId FK
    int GrantedByAccountId FK
    boolean CanView
    boolean CanAdd
    boolean CanEdit
    boolean CanDelete
    boolean CanApprove
    datetime GrantedAt
    string Notes
  }

  DOCUMENT {
    int DocumentId PK
    int ApplicationId FK
    int FolderId FK
    int DocumentTypeId FK
    int UploadedByAccountId FK
    int CurrentVersionId FK
    string Title
    string FileType
    string OriginalFileName
    string StoragePath
    string ContentHash
    boolean IsEncrypted
    boolean IsDuplicate
    boolean IsInRecycleBin
    datetime UploadedAt
    datetime UpdatedAt
    datetime DeletedAt
  }

  DOCUMENT_VERSION {
    int DocumentVersionId PK
    int DocumentId FK
    int UploadedByAccountId FK
    int VersionNumber
    string FileName
    string StoragePath
    string MimeType
    long FileSizeBytes
    string ContentHash
    boolean IsCurrent
    datetime CreatedAt
  }

  DOCUMENT_PERMISSION {
    int DocumentPermissionId PK
    int DocumentId FK
    int AccountId FK
    int GrantedByAccountId FK
    string PermissionLevel
    datetime GrantedAt
    boolean IsMuted
  }

  DOCUMENT_COMMENT {
    int DocumentCommentId PK
    int DocumentId FK
    int AuthorAccountId FK
    int ParentCommentId FK
    string CommentText
    datetime CreatedAt
    datetime UpdatedAt
    boolean IsResolved
  }

  NOTIFICATION {
    int NotificationId PK
    int RecipientAccountId FK
    int TriggeredByAccountId FK
    int ApplicationId FK
    int DocumentId FK
    int CommentId FK
    string NotificationType
    string Message
    boolean IsRead
    datetime CreatedAt
    datetime ReadAt
  }

  PASSWORD_RESET_TOKEN {
    int PasswordResetTokenId PK
    int AccountId FK
    string Token
    datetime ExpiresAt
    datetime UsedAt
    datetime CreatedAt
  }
```

## Document State Diagram

```mermaid
%%{init: {'themeVariables': {'fontSize': '22px', 'fontFamily': 'Arial'}}}%%
stateDiagram-v2
  [*] --> Draft
  Draft --> Submitted: citizen submits
  Submitted --> InReview: practitioner opens
  InReview --> AdditionalInformationRequired: more documents needed
  AdditionalInformationRequired --> InReview: citizen resubmits
  InReview --> Approved: practitioner approves
  InReview --> Rejected: practitioner rejects
  Approved --> ReadyForCollection: collection date set
  ReadyForCollection --> Collected: citizen collects
  Draft --> Trashed: move to recycle bin
  Submitted --> Trashed: move to recycle bin
  InReview --> Trashed: move to recycle bin
  AdditionalInformationRequired --> Trashed: move to recycle bin
  Approved --> Trashed: move to recycle bin
  Rejected --> Trashed: move to recycle bin
  ReadyForCollection --> Trashed: move to recycle bin
  Trashed --> Draft: restore
```

## Application State Diagram

```mermaid
stateDiagram-v2
  [*] --> Draft
  Draft --> Submitted
  Submitted --> InReview
  InReview --> PendingDocuments
  PendingDocuments --> InReview
  InReview --> Approved
  InReview --> Rejected
  Approved --> ReadyForCollection
  ReadyForCollection --> Collected
  Draft --> Cancelled
  Submitted --> Cancelled
  InReview --> Cancelled
  PendingDocuments --> Cancelled
  Approved --> Cancelled
```
