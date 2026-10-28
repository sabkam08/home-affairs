# HomeAffairs Architecture

## Architectural Style

Modular Monolith

## Principles

- Clean Architecture
- Domain Driven Design
- CQRS
- Separation of Concerns
- Security By Design
- Auditability First

## Core Aggregates

### Application

Root aggregate responsible for:

- Workflow
- Documents
- Status
- Assignment

### Citizen

Represents both Applicants and Subjects.

### Document

Version controlled document entity.

### Workflow Instance

Tracks progress through an application workflow.

## Patterns

- Repository Pattern
- Specification Pattern
- Strategy Pattern
- State Pattern
- Domain Events
- Outbox Pattern
- Optimistic Concurrency