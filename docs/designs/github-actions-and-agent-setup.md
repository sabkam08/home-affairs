# GitHub Actions, Workflows, and AI Agent Setup

## 1. Introduction

GitHub has become much more than a place to host code. It is now a full platform for software delivery, automation, collaboration, and even AI-assisted development. In modern software engineering, the key concepts that matter most are:

- GitHub Actions
- workflow automation
- CI/CD pipelines
- GitHub automation triggers
- repository and environment configuration
- secrets and variables
- permissions and security
- AI coding assistance and agent orchestration
- context files for persistent project instructions

This document explains these concepts in detail and connects them to real-world project workflows, especially for a repository like this one, which contains both a backend and a frontend application.

---

## 2. What is GitHub Actions?

GitHub Actions is GitHub’s built-in automation platform. It lets you define automated tasks that run in response to events in a repository, such as:

- pushing code
- creating a pull request
- merging to a branch
- tagging a release
- scheduling a job
- manual triggering from the GitHub UI

At its core, GitHub Actions allows developers to automate build, test, deployment, linting, documentation generation, notifications, and even AI-driven routines.

### Why GitHub Actions matters

GitHub Actions is useful because it removes repetitive manual work. Instead of running build commands manually, the system can do it automatically every time code changes.

It is especially useful for:

- quality assurance
- consistency across teams
- deployment automation
- reducing errors from manual operations
- enabling faster development cycles

### Core idea

GitHub Actions runs jobs in ephemeral virtual environments called runners. Each workflow executes steps, and each step can run shell commands, scripts, or actions from the GitHub Marketplace.

---

## 3. Key Terminology

To understand GitHub Actions well, you need the vocabulary.

### Repository
A repository is the project’s GitHub home. It contains all code, branch history, pull requests, issues, and workflow files.

### Workflow
A workflow is a YAML definition file stored in the repository that tells GitHub when and how to run automation. The workflow file is usually kept under:

```yaml
.github/workflows/
```

### Event
An event is a trigger that starts a workflow. Common events include:

- push
- pull_request
- workflow_dispatch
- schedule
- release
- issue_comment

### Job
A job is a unit of work inside a workflow. Jobs can run in parallel or depend on each other.

### Step
A step is a single action or command within a job. Example:

- checkout repository
- install dependencies
- run tests
- publish build artifact

### Runner
A runner is a machine that executes the workflow. GitHub-hosted runners are provided by GitHub. Self-hosted runners can run on your own infrastructure.

### Action
An action is a reusable unit of automation. It can be a built-in GitHub action, a third-party action, or a custom, local action in your repo.

### Secrets
Secrets are encrypted values such as API keys, deploy tokens, or credentials that cannot be exposed in logs.

### Variables
Variables are non-secret values such as environment names, build flags, or paths used across workflows.

### Environment
An environment is a deployment target with optional protection rules, approvals, and environment-level secrets.

### Matrix
A matrix strategy allows one workflow to run against multiple versions or platforms in parallel, for example:

- Node.js 18 and 20
- Ubuntu and Windows
- .NET 8 and .NET 9

---

## 4. What is a GitHub Workflow?

A workflow is the automation recipe. It is written in YAML and defines:

- when it runs
- what jobs it contains
- what each job should do
- how dependencies flow
- what environment or permissions it needs

A very simple workflow file might look like this:

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Set up Node
        uses: actions/setup-node@v4
        with:
          node-version: 20
      - name: Install dependencies
        run: npm ci
      - name: Build app
        run: npm run build
```

This workflow checks out code, installs dependencies, and builds the app whenever code is pushed or a pull request is opened.

---

## 5. GitHub Actions Workflow Structure

A workflow file usually contains these sections:

### Name
Human-readable workflow title.

```yaml
name: Continuous Integration
```

### Trigger events
Defines when the workflow runs.

```yaml
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
```

### Jobs
Contains execution blocks.

```yaml
jobs:
  test:
```

### Runner
Specifies the environment.

```yaml
runs-on: ubuntu-latest
```

### Steps
The concrete commands or actions to execute.

```yaml
steps:
  - uses: actions/checkout@v4
  - run: dotnet build
```

---

## 6. Common Triggers (Events)

### push
Runs when code is pushed to a branch.

```yaml
on:
  push:
    branches:
      - main
      - develop
```

### pull_request
Runs when a PR is created or updated.

```yaml
on:
  pull_request:
    types: [opened, synchronize, reopened]
```

### workflow_dispatch
Lets you trigger a workflow manually from the GitHub interface.

```yaml
on:
  workflow_dispatch:
```

### schedule
Runs jobs on a cron schedule.

```yaml
on:
  schedule:
    - cron: '0 0 * * *'
```

### release
Runs when a release is published.

```yaml
on:
  release:
    types: [published]
```

### workflow_run
Runs after another workflow finishes.

```yaml
on:
  workflow_run:
    workflows: ["CI"]
    types: [completed]
```

---

## 7. Jobs and Dependencies

Jobs can be independent or dependent.

### Independent jobs
Two jobs can run in parallel.

```yaml
jobs:
  build:
    runs-on: ubuntu-latest

  test:
    runs-on: ubuntu-latest
```

### Dependent jobs
A job can wait for another job to complete.

```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - run: echo "Build done"

  deploy:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - run: echo "Deploying"
```

The `needs` keyword creates a dependency chain.

---

## 8. Runners

A runner is where your job actually executes.

### GitHub-hosted runners
GitHub manages the VM for you. Examples:

- ubuntu-latest
- windows-latest
- macos-latest

### Self-hosted runners
You run your own machine or server in your environment. Common when you need:

- private networking
- custom tools installed
- access to internal infrastructure
- lower cost for large workloads

### Runner security
A runner is effectively code execution. Treat it like a server. Ensure you:

- use least privilege
- avoid exposing secrets
- restrict access to protected branches
- use self-hosted runners only when necessary

---

## 9. Actions and Reusable Automation

Actions are reusable building blocks.

### Official actions
GitHub-maintained actions are common and trusted.

```yaml
- uses: actions/checkout@v4
- uses: actions/setup-node@v4
- uses: docker/login-action@v3
```

### Third-party actions
These are sometimes used from the marketplace.

```yaml
- uses: some-org/some-action@v1
```

### Local actions
You can create custom actions in your repo for organization-specific behavior.

Example folder:

```text
.github/actions/my-action/
```

Local action may contain:

- action.yml
- entrypoint script
- metadata

---

## 10. YAML Format and Workflow Syntax

GitHub Actions workflows are YAML files. YAML is easy to read but strict about indentation.

Example:

```yaml
name: Example Workflow

on:
  push:
    branches:
      - main

jobs:
  example:
    runs-on: ubuntu-latest
    steps:
      - name: Print message
        run: echo "Hello from GitHub Actions"
```

### Important YAML rules

- indentation matters
- string values need quotes when necessary
- lists use `-`
- maps use `key: value`
- correct spacing prevents syntax errors

---

## 11. GitHub Actions Variables and Secrets

### Secrets
Secrets are encrypted values stored in GitHub repository or environment settings.

Example:

```yaml
env:
  API_TOKEN: ${{ secrets.API_TOKEN }}
```

Use secrets for:

- cloud provider credentials
- deployment tokens
- API keys
- database passwords

### Variables
Variables are not encrypted and can be shared across workflows.

```yaml
env:
  NODE_ENV: production
```

Or from repo variables:

```yaml
env:
  APP_NAME: ${{ vars.APP_NAME }}
```

### Best practices

- never hardcode secrets
- avoid printing secrets to logs
- rotate secrets regularly
- use environment-level secrets for deployments

---

## 12. Environments, Protection Rules, and Deployments

Environments are useful for deployment workflows.

Example:

```yaml
jobs:
  deploy:
    environment: production
    runs-on: ubuntu-latest
```

Environments can have:

- required reviewers
- wait timers
- deployment branches
- environment-specific secrets

This helps control who can deploy and when.

---

## 13. Permissions and Security

Workflow permissions are important. Without careful configuration, automation may have too much access.

Example:

```yaml
permissions:
  contents: read
  packages: write
  id-token: write
```

### Good security practices

- use minimum required permissions
- avoid broad `write-all` tokens
- limit self-hosted runners
- verify third-party actions before use
- pin actions to a specific commit or version

A safer pattern is:

```yaml
- uses: actions/checkout@v4
```

not just a floating tag when possible.

---

## 14. Workflow Examples

### Example 1: .NET backend build validation

This is useful for a project that contains an ASP.NET Core API.

```yaml
name: Backend CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: cd backend && dotnet restore

      - name: Build solution
        run: cd backend && dotnet build --no-restore
```

### Example 2: Angular frontend build validation

```yaml
name: Frontend CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: 22

      - name: Install dependencies
        run: cd frontend && npm install

      - name: Build Angular app
        run: cd frontend && npm run build
```

### Example 3: Combined backend + frontend workflow

```yaml
name: Full Stack CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

jobs:
  backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: cd backend && dotnet restore
      - run: cd backend && dotnet build --no-restore

  frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 22
      - run: cd frontend && npm install
      - run: cd frontend && npm run build
```

### Example 4: Deployment workflow

```yaml
name: Deploy to Azure

on:
  workflow_dispatch:

jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production
    steps:
      - uses: actions/checkout@v4
      - name: Deploy application
        run: echo "Deploying to production environment"
```

---

## 15. CI vs CD

### Continuous Integration (CI)
CI is about automatically validating the codebase. Typical tasks:

- restore dependencies
- compile
- run unit tests
- lint code
- build artifacts

### Continuous Delivery / Deployment (CD)
CD takes validated code and ships it to a target environment.

- publish build output
- deploy to staging
- deploy to production
- run smoke tests

A workflow often contains both CI and CD stages.

---

## 16. Workflow Best Practices

### Keep workflows small and focused
Each workflow should do one primary job. Avoid mixing too many concerns into a single workflow.

### Use reusable workflows
If many repos or projects share a common deployment pattern, create reusable workflows.

### Cache dependencies
This speeds up builds significantly.

```yaml
- uses: actions/cache@v4
  with:
    path: ~/.npm
    key: ${{ runner.os }}-node-${{ hashFiles('**/package-lock.json') }}
```

### Fail fast
Use early validation so broken code is caught quickly.

### Keep logs readable
Use clear step names and concise commands.

### Validate before merge
Use pull request workflows to prevent bad code from reaching main.

---

## 17. GitHub Actions Marketplace

The GitHub Marketplace contains hundreds of actions that can help you with:

- linting
- test reporting
- code coverage
- deployment to cloud providers
- Docker builds
- notifications
- security scanning

Examples:

- actions/checkout
- actions/setup-node
- actions/setup-dotnet
- docker/login-action
- codecov/codecov-action

Reusing actions reduces implementation time but you still need to review the action’s source and trust level.

---

## 18. GitHub Actions for this Project

For a project like this repository, GitHub Actions could be used to validate both backend and frontend code on every push or pull request.

### Example workflow goals for this repo

- check that backend builds successfully
- check that frontend builds successfully
- run tests if they exist
- keep main deployable
- optionally run a lint or formatting check

### Suggested project workflow plan

1. Run backend build on PRs to main
2. Run frontend build on PRs to main
3. Run tests after build succeeds
4. Use branch protection to block merges when checks fail
5. Add deployment workflow only after staging or production readiness

---

## 19. What Are AI Agents?

An AI agent is an automated software system that can:

- interpret a task
- inspect files or code
- make decisions
- call tools or commands
- iteratively complete tasks
- ask for clarification when needed

In modern software development, an AI agent can act like a coding assistant that works with your repository context.

### Simple definition
An AI agent is not just a chatbot. It is a system that can take a task, use tools, and perform work in context.

### Common agent capabilities

- read files and folder structure
- search for code patterns
- understand project architecture
- edit files
- run commands
- check build/test results
- summarize work

---

## 20. Difference Between Chatbots and Agents

### Chatbot
A chatbot usually responds to prompts with text. It may provide code examples but does not necessarily act on the repository.

### Agent
An agent can work inside your project, read files, modify code, run commands, and build the solution. It is task-oriented and often tool-using.

Example:

- Chatbot: “Write a login form in Angular.”
- Agent: “Inspect the app structure, locate the auth module, create the component, update routing, and verify with the Angular build.”

---

## 21. Context Files for AI Agents

One of the most important patterns in AI-assisted development is giving the agent persistent context.

Instead of telling the model the same information every single session, you create a file such as:

```text
AGENTS.md
```

or

```text
.github/copilot-instructions.md
```

This file contains project-specific instructions that the agent reads automatically.

### What should go into context files?

- project overview
- stack used (ASP.NET Core, Angular, etc.)
- folder structure
- architecture boundaries
- verification commands
- naming conventions
- rules for change scope
- docs references
- security choices

### Why context files matter

Without context, the AI may:

- use the wrong technology stack
- propose broad refactors
- ignore the architecture
- break project conventions
- make assumptions about requirements

With context files, the agent starts each session with the project memory already loaded.

---

## 22. AGENTS.md and Copilot Instructions

### AGENTS.md
This is a common repo-level file used by AI agents to understand a project.

Example structure:

```md
# Project Context

## Stack
- ASP.NET Core backend
- Angular frontend

## Architecture
- Domain logic in Backend.Domain
- App services in Backend.Application
- Infrastructure in Backend.Infrastructure

## Validation
- cd backend && dotnet build
- cd frontend && npm run build
```

This gives the agent consistent instruction.

### .github/copilot-instructions.md
This is GitHub Copilot-specific context. It is especially useful when using Copilot inside GitHub and VS Code.

Example:

```md
# Copilot instructions for Home Affairs project

## Project stack
- ASP.NET Core backend in backend/
- Angular 22 frontend in frontend/

## Coding guidance
- Prefer small, surgical changes
- Respect layered architecture
- Check design docs before large restructuring
```

These files help reduce repeated prompting and improve consistency.

---

## 23. How AI Agents Use Context

Agents work best when they have reliable project context. They can use:

- repo structure
- design docs
- coding conventions
- validation commands
- security practices

As a result, they become more effective at:

- understanding the codebase
- making consistent changes
- following project architecture
- avoiding destructive broad changes

This is especially important for multi-layer apps with backend and frontend logic.

---

## 24. Agent Setup Workflow for a Project

To set up an AI agent for a repo, you typically do the following:

### Step 1: Define the project stack
Document the technologies and folder roles.

### Step 2: Add repo context files
Create files like:

- AGENTS.md
- .github/copilot-instructions.md

### Step 3: Include validation commands
Tell the agent how to build and test the project.

### Step 4: Document architecture boundaries
Say where domain, application, infrastructure, and UI code lives.

### Step 5: Define project rules
For example:

- do not change public API without review
- do not create unnecessary libraries
- prefer small changes
- check design docs before major architectural decisions

### Step 6: Add practical examples
Include examples of what good work looks like.

---

## 25. Practical Example: Agent Setup for This Repository

For this project, your agent context should explain that the repo is made up of:

- ASP.NET Core backend in `backend/`
- Angular frontend in `frontend/`
- design docs in `docs/designs/`
- deliverables in `docs/deliverables/`

A practical example context file could be:

```md
# Home Affairs AI Agent Context

## Project overview
This repository contains a full-stack Home Affairs document management system.

- Backend: ASP.NET Core app in backend/
- Frontend: Angular 22 app in frontend/
- Design and requirements docs: docs/

## Architecture
- Domain layer: backend/Backend.Domain/
- Application layer: backend/Backend.Application/
- Infrastructure layer: backend/Backend.Infrastructure/
- API host: backend/backend/Program.cs

## Working conventions
- Keep changes small and consistent with the project structure.
- Check design docs before proposing large architecture changes.
- Do not invent new services or patterns unless already used.

## Validation commands
- cd backend && dotnet build
- cd frontend && npm run build
```

This is enough for the agent to work with the project without repeated explanations.

---

## 26. GitHub Actions and AI Agents Together

GitHub Actions and AI agents are complementary.

### GitHub Actions handles automation
- builds
- tests
- deployments
- validation checks
- scheduled jobs

### AI agents handle development assistance
- code generation
- refactoring
- issue analysis
- documentation updates
- repository understanding

Together, they create a strong developer workflow:

1. Developer opens a PR.
2. GitHub Action runs CI checks.
3. AI agent reviews code context.
4. Agent suggests code improvements or identifies issues.
5. Workflow completes only when checks pass.

This gives a robust engineering loop.

---

## 27. Example End-to-End Setup

### Example scenario
You have a repository with a .NET backend and Angular frontend.

### Setup steps

#### Step 1: Create workflow files
Create a folder:

```text
.github/workflows/
```

Then add a file like:

```text
.github/workflows/fullstack-ci.yml
```

#### Step 2: Add automation rule
Example:

```yaml
name: Fullstack CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: cd backend && dotnet restore
      - run: cd backend && dotnet build --no-restore

  frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 22
      - run: cd frontend && npm install
      - run: cd frontend && npm run build
```

#### Step 3: Add project context for AI
Create:

```text
AGENTS.md
```

with repo instructions.

#### Step 4: Commit and push
After pushing to GitHub, the workflow starts automatically.

#### Step 5: Review results
Go to the GitHub Actions tab and inspect logs.

---

## 28. Branch Protection and Quality Gates

GitHub Actions becomes even more powerful when paired with branch protection rules.

Examples of branch protection:

- require PR before merge
- require status checks to pass
- restrict direct pushes to main
- require code review approval

This ensures the workflow acts as a quality gate.

---

## 29. Deployment Safety Patterns

When deploying, always use a staged approach.

Recommended model:

- develop branch runs CI checks
- main branch runs production validation
- deployment only after successful workflow completion
- production environment has reviewers and secrets

This improves overall reliability.

---

## 30. Troubleshooting Common Issues

### Workflow fails due to missing dependency
Cause: required tool not installed.

Fix: add setup action such as:

```yaml
- uses: actions/setup-node@v4
```

or

```yaml
- uses: actions/setup-dotnet@v4
```

### YAML indentation issue
Cause: wrong whitespace or YAML structure.

Fix: ensure consistent indentation and valid keys.

### Secret not available
Cause: secret not defined in repository or environment.

Fix: create the secret in GitHub settings.

### Action version mismatch
Cause: action changed behavior or API expectations.

Fix: pin to a known stable version and review release notes.

---

## 31. Recommended Setup Pattern for Team Projects

For a real team project, a good structure is:

- store workflow files under `.github/workflows/`
- keep project instructions in `AGENTS.md`
- keep Copilot-specific file in `.github/copilot-instructions.md`
- use branch protection
- use environment approvals for deployment
- store secrets in GitHub, not in source code
- keep CI fast and focused

This creates a reliable team workflow.

---

## 32. Summary

GitHub Actions and workflow automation are foundational parts of modern software engineering. They allow your repository to automatically validate code, build artifacts, run tests, and deploy software with controlled rules and security.

AI agent setup complements this by giving the assistant the right project context, so it can understand your repository, follow conventions, and perform work consistently.

The two together enable a powerful engineering loop:

- code changes happen
- workflows validate them
- agents assist with implementation and understanding
- quality gates prevent broken code from merging

This is the modern standard for productive, scalable, maintainable development.

---

## 33. Checklist for Implementing This in a Repository

Use this as a quick setup checklist:

- [ ] Create `.github/workflows/` folder
- [ ] Add a CI workflow for build/test validation
- [ ] Add a deployment workflow only when needed
- [ ] Store secrets in GitHub secrets
- [ ] Configure environment protections
- [ ] Add `AGENTS.md` for repo context
- [ ] Add `.github/copilot-instructions.md` for Copilot behavior
- [ ] Document build commands in context files
- [ ] Ensure decisions align with project architecture
- [ ] Review workflow logs regularly

---

## 34. Final Practical Advice

If you are implementing this in your own repo, start simple. Do not begin with complex deployments or elaborate AI orchestration. Start with:

1. a CI workflow for build validation
2. a clear `AGENTS.md` file
3. a small set of coding rules
4. one or two parseable validation commands

Once the repo is stable, you can expand to deployment, testing, security checks, and richer agent workflows.

This approach keeps the system maintainable and reduces confusion for both humans and AI tools.
