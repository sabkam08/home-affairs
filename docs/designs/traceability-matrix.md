# Requirements Traceability Matrix

| Requirement ID | Requirement | Design Element | Implementation Surface |
|---|---|---|---|
| FR-01 | Intelligent search | Documents workspace and filter controls | `GET /api/documents?query=&fileType=&folderId=` |
| FR-02 | Filter documents by type and date | Documents filters and backend query params | `GET /api/documents` |
| FR-03 | General access folders | Folder model with `generalAccess` flag | `Folder`, `GET /api/folders` |
| FR-04 | Multi-format upload | Upload form with file picker and Base64 transport | `POST /api/documents` |
| FR-05 | Role-based encryption/access | Session role checks and permission checks | Auth/session and document access guards |
| FR-06 | Version control | Document versions panel | `POST /api/documents/{id}/versions` |
| FR-07 | Invite employees for collaboration | Share access form | `POST /api/documents/{id}/share` |
| FR-08 | Leave comments on docs | Comment form and timeline | `POST /api/documents/{id}/comments` |
| FR-09 | Alerts on changes | Notification panel | `GET /api/notifications` |
| FR-10 | Alerts for approvals/rejections | Workflow approvals page and notifications | `POST /api/workflows/{id}/approve` and `/reject` |
| FR-11 | IT superuser user management | Users page | `GET /api/users`, `POST /api/users`, `PATCH /api/users/{id}` |
| FR-12 | Lock after 5 failed logins | Login flow and account state | `POST /api/auth/login` |
| FR-13 | Reset email process | Password reset workflow | `POST /api/auth/password-reset/start` and `/complete` |
| FR-14 | Recycle bin | Trash and restore actions | `POST /api/documents/{id}/trash` and `/restore` |
| FR-15 | Session timeout | Token session expiry | In-memory session handling in backend |
| FR-16 | Duplicate upload detection | Checksum validation | Document upload service |
| FR-17 | Citizen portal | Citizen application screen | `POST /api/citizen/applications` |
| NFR-01 | Scalability | Layered separation of UI, API, and storage | Architecture diagram |
| NFR-02 | Performance | Search/filter at API layer and lightweight storage | Document query and file persistence |
| NFR-03 | Disaster recovery | File storage separated from metadata | Local storage and structured state |
| NFR-04 | 24/7 runtime readiness | Stateless API process and decoupled frontend | Docker-ready structure via JS/Java services |

## Excluded Nice-to-Have Items

- Scanning citizen documents from a printer.
- ABIS national register integration.
- Biometric scanning.
