# Epic 1 Context: Fundación técnica — scaffold desplegable

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Stand up a deployable project skeleton — a .NET/ASP.NET Core backend built on Clean Architecture plus an Angular shell — running end-to-end on Azure, with CI/CD (GitHub Actions) and infrastructure-as-code (Terraform) working from the very first health-check endpoint. Every subsequent epic deploys incrementally onto this already-automated base, with no infrastructure debt to retrofit later.

## Stories

- Story 1.1: Esqueleto del repositorio (Clean Architecture + Angular)
- Story 1.2: Infraestructura Azure definida en Terraform
- Story 1.3: CI — build y test en cada push/PR
- Story 1.4: CD manual — desplegar el esqueleto a un ambiente de dev
- Story 1.5: Endpoint de health-check verificable de punta a punta
- Story 1.6: Destruir el ambiente de dev bajo demanda
- Story 1.7: Publicar el contrato OpenAPI
- Story 1.8: Monitoreo básico de disponibilidad

## Requirements & Constraints

- No functional requirements are covered directly by this epic; it exists to enable everything downstream.
- Production availability target is 99% uptime — the epic must wire up monitoring/alerting so this is measurable from day one, not discovered later via user reports.
- A PR must not be mergeable if the build/test pipeline fails.
- No `terraform apply`/`destroy` or deploy step is ever run by hand from a developer's local machine — every environment (dev, staging, prod) goes through the pipeline.
- Dev environments are ephemeral: created and destroyed only via manual pipeline trigger (`workflow_dispatch`), never automatically per PR/push, and the destroy workflow must never be able to target staging/production.
- Staging/production resource groups are longer-lived: created once, then redeployed (never re-created) on every push to main.
- The backend REST contract must be defined by a generated, published, versioned OpenAPI spec — this is what Angular (and later Flutter) build and validate against, not prose or hand-read code.

## Technical Decisions

- Backend follows Clean Architecture with dependencies pointing inward: `Domain` (no outward dependencies) ← `Application` (depends on Domain, defines interfaces) ← `Infrastructure` (implements Application's interfaces) ← `Api` (composition root, wires DI). Domain must never reference EF Core or other infrastructure types.
- Backend source tree: `src/Domain`, `src/Application`, `src/Infrastructure`, `src/Api`. The backoffice/admin UI (future epic) lives as a role-gated section of the same `Api`/Angular app, not a separate app — relevant to keep the scaffold from splitting into multiple deployables.
- Angular and Flutter are independent codebases with no shared UI code; only the REST API contract is shared between them. Flutter is out of scope for this epic but the OpenAPI-first approach set up here is what it will later depend on.
- Azure resources required (all Terraform-defined, `azurerm` provider): Container Apps (consumption/scale-to-zero plan, hosts the .NET API), Static Web Apps (Standard tier, hosts Angular), PostgreSQL Flexible Server (Burstable B1ms), Blob Storage, Communication Services (email-only OTP — not needed until Epic 5, but provisioned as part of the resource set), and the resource group itself.
- Stack versions: .NET 10 / ASP.NET Core 10 / EF Core 10, Angular (latest), PrimeNG (Aura preset, Community License), GitHub Actions (private repo, free plan), Application Insights for monitoring (provisioned via Terraform, bound to the 99% uptime NFR).
- CI runs build+test on every push and PR. CD is manual (`workflow_dispatch`) for both deploying and destroying the dev resource group. Main pushes redeploy staging/production in place.
- Error responses across the API should use the RFC 7807 Problem Details envelope (no per-endpoint bespoke error shapes) — worth establishing at scaffold time since it affects every future endpoint.
- JWT validation is centralized in ASP.NET Core middleware, not reimplemented per endpoint — no auth endpoints exist yet in this epic, but the middleware pattern is a scaffold-level decision.
- Exact Postgres schema, exact Azure region, and dev database seed/fixture mechanism are all explicitly undecided/deferred beyond this epic.

## Cross-Story Dependencies

- Story 1.2 (Terraform infra) must exist before Story 1.4 (CD) can deploy anything.
- Story 1.3 (CI) is a prerequisite for Story 1.4 (CD requires a green pipeline before manual deploy).
- Story 1.4 (dev deploy) is a prerequisite for Story 1.5 (health-check verification), Story 1.6 (dev destroy), and Story 1.8 (monitoring, which needs a deployed target).
- Story 1.7 (OpenAPI publishing) depends on Story 1.5's health endpoint existing as the first thing the spec describes, and is itself a prerequisite for all of Epic 6 (Flutter) and any Angular work that consumes the API contract.
- This entire epic is a prerequisite for Epics 2–6: every later epic's backend/frontend work deploys onto this scaffold rather than establishing its own CI/CD or infrastructure.
