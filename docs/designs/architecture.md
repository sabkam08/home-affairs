# Component-Based Architecture

## Overview

The system is organised into three layers:

1. Presentation layer: React SPA for staff and citizens.
2. Application layer: Java HTTP API handling authentication, documents, workflows, notifications, and citizen applications.
3. Data and file layer: in-memory operational state with file storage for uploaded document binaries.

## Component Diagram

```mermaid
flowchart LR
  subgraph Presentation[Presentation Layer]
    WebApp[React Web App]
    CitizenPortal[Citizen Portal Screens]
    AdminScreens[Admin & Review Screens]
  end

  subgraph Application[Application Layer]
    Api[Java HTTP API]
    Auth[Authentication & Session Service]
    Docs[Document Service]
    Workflow[Workflow Service]
    Users[User Administration Service]
    Notify[Notification Service]
    Citizen[Citizen Application Service]
    Trace[Traceability Endpoint]
  end

  subgraph Storage[Storage Layer]
    State[In-Memory Domain State]
    Files[Local File Storage]
  end

  WebApp --> Api
  CitizenPortal --> Api
  AdminScreens --> Api

  Api --> Auth
  Api --> Docs
  Api --> Workflow
  Api --> Users
  Api --> Notify
  Api --> Citizen
  Api --> Trace

  Auth --> State
  Docs --> State
  Workflow --> State
  Users --> State
  Notify --> State
  Citizen --> State
  Docs --> Files
  Citizen --> Files
```

## Key Design Decisions

- Authentication is token-based with a short demo session timeout to satisfy the inactivity requirement.
- Documents are versioned and protected with role-aware access checks and per-document permissions.
- Duplicate uploads are prevented through checksum comparison before storage.
- Citizen submissions are routed through the same workflow engine as internal approvals.
- File binaries are stored separately from metadata so versions can be tracked cleanly.
