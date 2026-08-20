using System;

namespace Backend.Domain.Entities
{
    public enum AccountRole
    {
        Citizen = 1,
        Practitioner = 2,
        SuperUser = 3
    }

    public enum AccountStatus
    {
        Active = 1,
        Locked = 2,
        Disabled = 3,
        PendingReset = 4
    }

    public enum ApplicationStatus
    {
        Draft = 1,
        Submitted = 2,
        InReview = 3,
        PendingDocuments = 4,
        Approved = 5,
        Rejected = 6,
        ReadyForCollection = 7,
        Collected = 8,
        Cancelled = 9
    }

    public enum FolderScope
    {
        General = 1,
        BranchUnit = 2,
        ApplicationProcessing = 3,
        Private = 4
    }

    public enum DocumentPermissionLevel
    {
        View = 1,
        Comment = 2,
        Edit = 3,
        Approve = 4,
        Manage = 5
    }

    public enum NotificationType
    {
        DocumentChanged = 1,
        DocumentApproved = 2,
        DocumentRejected = 3,
        ApplicationUpdated = 4,
        CollectionReady = 5,
        AccountLocked = 6
    }

    public enum WorkflowActionType
    {
        Submitted = 1,
        Assigned = 2,
        MovedStage = 3,
        Approved = 4,
        Rejected = 5,
        CollectionScheduled = 6,
        Collected = 7,
        Commented = 8
    }
}
