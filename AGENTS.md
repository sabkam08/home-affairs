# Home Affairs AI Agent Context

## Project overview
This repository contains a full-stack Home Affairs document management system.

- Backend: ASP.NET Core app in `backend/`
- Frontend: Angular 22 app in `frontend/`
- Design and requirements docs: `docs/`

## Architecture
- Domain model and business rules live under `backend/Backend.Domain/`
- Application services and orchestration live under `backend/Backend.Application/`
- Infrastructure and persistence code live under `backend/Backend.Infrastructure/`
- API entry point is `backend/backend/Program.cs`
- UI code starts in `frontend/src/app/`

## Working conventions
- Keep changes small and consistent with the existing project structure.
- Prefer the repository’s design documents in `docs/designs/` when making architectural decisions.
- Do not invent new patterns or services unless the codebase already uses them.
- Preserve separation of concerns between domain, application, infrastructure, and UI layers.

## Validation commands
Run the smallest relevant command for the change:

- Backend build: `cd backend && dotnet build`
- Frontend build: `cd frontend && npm run build`
- Frontend dev server: `cd frontend && npm start`

## Key goals
- Build a secure, scalable document-management workflow.
- Keep backend and frontend maintainable, readable, and aligned with the project design documents.
