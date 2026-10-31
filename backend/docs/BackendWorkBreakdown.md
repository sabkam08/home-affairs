Where we are now
Foundation ✅

Solution architecture ✅

API
Application
Contracts
Domain
Infrastructure
Worker
Unit / Integration / Architecture tests

API infrastructure ✅

Problem Details
exception handling
correlation IDs
Swagger
Serilog
OpenTelemetry
health checks

Persistence foundation ✅

SQL Server/Docker
schemas
Dapper
connection factory
stored-procedure approach
database readiness checks
Identity & Security 🟢 Nearly complete
Registration/login/JWT ✅
Failed-login + lockout + security events ✅
Refresh tokens + rotation + reuse detection + logout ✅, pending final checkpoint recording

So Identity has gone from being a large unknown to being mostly done.

What remains
Slice 1: Finish Identity authorization

This should be our next checkpoint.

We'll add:

permissions
role-permission mapping
authorization policies
current-user abstraction
practitioner/supervisor/admin authorization
administrator-created staff users
role assignment rules
authorization tests

This finishes the security boundary that everything else depends upon.

Slice 2: Automated Identity testing

Before moving deep into the business domain:

unit tests
registration tests
login tests
lockout tests
refresh rotation tests
replay/reuse tests
authorization tests
Testcontainers SQL integration tests
architecture tests

This is also important for your marking rubric. We shouldn't get to the end with only manual PowerShell evidence.

Citizen and application domain
Slice 3: Citizen Registry

Implement:

Plain Text
Citizen
Addresses
Contact details
Applicant information
Subjects
Guardian/parent relationships

Including:

citizen creation
retrieval
update
South African identity-related validation where appropriate
parent/guardian relationship handling

This supports the rule we established earlier that:

Applicant and Subject are not necessarily the same person.

Slice 4: Application Management

This is one of the biggest slices.

We'll implement the application aggregate:

Plain Text
Application
├── Applicant
├── Subject
├── ApplicationType
├── Status
├── Assignment
├── Documents
└── WorkflowInstance

Including:

submit
retrieve
list/search
update permitted information
practitioner assignment
reassignment
statuses
application history
Workflow
Slice 5: Workflow engine

Implement the direct workflow definitions we agreed on rather than one universal global sequence.

Including:

Plain Text
WorkflowDefinition
WorkflowStep
WorkflowInstance
WorkflowTransition
Assignment
Approval
Rejection
Return-for-information

And importantly:

valid transition enforcement
practitioner reassignment
supervisor approvals
concurrency protection
workflow history
Slice 6: Virtual folder/work queue

Remember our earlier architectural decision:

A folder is a virtual view over applications, not a physical document folder.

We'll implement queries such as:

Plain Text
Pending Initial Review
Pending Documents
Awaiting Supervisor Approval
Assigned to Me
Unassigned
Approved
Ready for Collection

That becomes a major practitioner-facing backend feature.

Document Management
Slice 7: Core document storage

Implement:

Plain Text
Document
DocumentVersion
DocumentMetadata
DocumentType
ApplicationDocument

With:

upload
download
SQL Server VARBINARY
MIME/type validation
file-size limits
versioning
hashes
document/application linking
access authorization
Slice 8: OCR and document search

Then the intelligent document layer:

Plain Text
PDF extraction
Word extraction
OCR
Extracted text
Indexing
Search

Including:

searchable document contents
metadata search
application-based filtering
asynchronous OCR/indexing through HomeAffairs.Worker

This is one of the system's standout features, so we'll give it proper evaluation evidence rather than treating it as a small helper.

Home Affairs service modules
Slice 9: Birth Certificate services

Implement the application types we scoped earlier:

Plain Text
Birth Registration
Birth Certificate Re-Issue
Late Birth Registration
Birth Certificate Amendment

Each gets:

validation rules
required-document rules
workflow definition
business decisions
application status behavior
Slice 10: Smart ID services

Implement:

Plain Text
New Smart ID
Replacement Due To Loss
Replacement Due To Theft
Replacement Due To Damage
Correction/Reprint

And enforce the important rule:

One citizen may have only one active Smart ID application at a time.

We'll enforce that at both the application/domain level and the database concurrency boundary.

Reliability
Slice 11: Auditing, outbox and Worker

Then finish the asynchronous/reliability architecture:

Plain Text
Audit events
Outbox messages
Background processing
Notification dispatch
OCR jobs
Indexing jobs
Retry handling
Failure handling

This is where HomeAffairs.Worker becomes meaningful rather than just existing in the solution.

Final backend validation
Slice 12: Full system testing and hardening

Finally:

Functional
application lifecycle
practitioner workflow
supervisor approval
reassignment
documents
Smart ID rules
birth certificate rules
folder/work queues
Security
authorization boundaries
JWT
refresh-token replay
role escalation attempts
unauthenticated access
invalid documents
account lockout
Persistence
transactions
concurrency
duplicate submissions
rollback behavior
stored procedures
Quality
unit tests
integration tests
architecture tests
Testcontainers
edge cases
Evaluation
API response measurements
database/query behavior
OCR/search evaluation
workflow evaluation

And at that point we'll finalize the testing evidence document rather than trying to reconstruct testing retrospectively.

Overall position

A useful way of seeing it is:

Plain Text
FOUNDATION
████████████████████ Done
 
IDENTITY
██████████████████░░ Nearly done
 
CITIZENS
░░░░░░░░░░░░░░░░░░░░
 
APPLICATIONS
░░░░░░░░░░░░░░░░░░░░
 
WORKFLOW
░░░░░░░░░░░░░░░░░░░░
 
DOCUMENTS
░░░░░░░░░░░░░░░░░░░░
 
BIRTH CERTIFICATES
░░░░░░░░░░░░░░░░░░░░
 
SMART ID
░░░░░░░░░░░░░░░░░░░░
 
WORKER / OUTBOX / AUDIT
░░░░░░░░░░░░░░░░░░░░
 
FINAL TESTING
░░░░░░░░░░░░░░░░░░░░

So I wouldn't say we're close to the entire backend yet. We're approximately at the point where the technical platform and authentication foundation are established, and we're about to enter the bulk of the actual Home Affairs business domain.

That's a good position to be in. The subsequent slices won't repeatedly require us to stop and invent authentication, database connectivity, error handling, tracing, Docker, etc.

Next checkpoint: finish Identity with permissions, authorization policies, staff-user creation/role assignment, current-user abstraction, and authorization testing.

Application
├── use cases
├── interfaces
├── validation
├── authorization abstractions
└── no IConfiguration / infrastructure options

Infrastructure
├── Dapper
├── SQL implementations
├── JWT
├── cryptography
├── configuration binding
└── permission persistence

API
├── authentication middleware
├── authorization middleware
├── HTTP current-user context
└── controllers
