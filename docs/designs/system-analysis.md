# System Analysis and Feasibility Study

## Overview

The Home Affairs system is a secure document and citizen application platform for internal staff and public users. It supports document management, access control, collaboration, workflow approvals, and a citizen portal. The current implementation is designed as a React frontend with a Java HTTP backend and local file storage for uploaded documents.

## Functional Requirements

### Document Management

- Upload documents in multiple formats using drag-and-drop or standard file selection.
- Detect duplicate uploads before storing a file.
- Organize general access folders for shared internal documents such as employee policies.
- Filter and search documents by keywords, file type, and date modified.
- Support version control so changes to documents can be tracked over time.
- Provide a recycle bin for accidental deletion and recovery.

### Access Control and Security

- Enforce role-based access and encryption so users only see content allowed by their role.
- Lock accounts after repeated failed login attempts, with a maximum of five incorrect tries.
- Provide a password reset flow through email for forgotten passwords.
- Automatically time out inactive sessions after 10 minutes in production and 3 minutes for the demo build.

### Collaboration and Notifications

- Allow document owners to invite employees to collaborate and adjust permissions.
- Support comments on documents for review and discussion.
- Notify collaborators when documents change, with the option to silence alerts.
- Send automated approval and rejection notifications by email.

### Administration

- Allow an IT superuser to create and delete user accounts.
- Maintain access rules for staff, reviewers, and administrators.


### Excluded Nice-to-Have Items

- Document scanning from a printer into the system.
- Retrieval from the national citizen register (ABIS).
- Biometric scanning.

## Non-Functional Requirements

### Performance

- File edits, deletes, saves, and searches must respond in a timely manner.
- The system should remain usable as document volume grows.

### Scalability

- The platform must handle large numbers of records, files, and user accounts.
- The design should support growth in both internal staff usage and citizen submissions.

### Availability and Reliability

- The service should be available continuously for operational use.
- Runtime monitoring and recovery procedures should reduce downtime.

### Disaster Recovery

- System data and uploaded files should be recoverable after failure or accidental loss.
- Backup and restore procedures should be defined for operational continuity.

### Security

- Access control must be role-aware and enforced consistently.
- Session management, account locking, and password recovery must protect against unauthorized access.

## Feasibility Study

### Technical Feasibility

The system is technically feasible with the current stack. The repository already contains a Java backend and React frontend, which are sufficient for implementing document workflows, authentication, and the citizen portal. Local file storage is adequate for the current scope, especially for a demo or early deployment. The main technical constraints are advanced integrations such as ABIS access and biometric support, which are intentionally out of scope.

### Operational Feasibility

The system is operationally feasible because it matches the expected work of Home Affairs staff: document review, approvals, account administration, and citizen application intake. Role-based permissions and notifications support day-to-day work without requiring users to learn a complex new process. The short demo session timeout is workable for demonstration, while the production timeout is appropriate for secure operations.

### Economic Feasibility

The solution is economically feasible because it relies on a lightweight web architecture rather than expensive enterprise platforms. Using existing Java and React tooling reduces implementation cost and training overhead. Local storage keeps the initial deployment cost low, although long-term production use may require investment in more durable storage, backups, and hosting.

### Schedule Feasibility

The delivery is schedule feasible because the core scope is focused on standard web application features: authentication, document management, notifications, and citizen intake. The excluded items remove high-risk dependencies that would otherwise slow delivery. The main schedule risk is adding advanced integrations later, so they should remain separate phases.

### Legal and Compliance Feasibility

The system is feasible from a compliance perspective if access control, auditability, and data protection are implemented correctly. Personal and citizen data should be handled according to government security and privacy expectations. Sensitive integrations and biometric processing are deferred, which reduces immediate compliance risk.

## Conclusion

The requirements describe a practical and achievable system for secure document management and citizen applications. The current architecture supports the essential functional needs, and the non-functional requirements are realistic for the chosen technology stack. Overall, the project is feasible, with the clearest path to delivery being a phased implementation that focuses first on document workflows, access control, and the citizen portal.