<!--
Sync Impact Report
==================
Version change: 1.0.0 → 1.1.0 (MINOR — added Business Domain section and Principle VIII)
Modified principles:
  - I. Clean Architecture + DDD (NON-NEGOTIABLE) — unchanged
  - II. CQRS-lite with Command/Query Separation — unchanged
  - III. Multi-Tenancy by Default (NON-NEGOTIABLE) — unchanged
  - IV. RBAC via Security-Issued JWT — unchanged
  - V. Errors as Values (ErrorOr) — unchanged
  - VI. Layered Testability with Domain Test-First — unchanged
  - VII. Anti-Corruption Layer for Marketplace Integrations — unchanged
Added principles:
  - VIII. Profitability Guardian (NON-NEGOTIABLE) — no automated write path may sell below the
    Financial Engine's minimum floor; overrides require explicit human confirmation and audit.
Added sections:
  - Business Domain (product identity, target user, module map, cross-cutting invariant)
Removed sections:
  - None
Templates requiring review:
  - .specify/templates/plan-template.md — verify Constitution Check gate includes Principle VIII
  - .specify/templates/spec-template.md — no change required
  - .specify/templates/tasks-template.md — no change required
Deferred items / TODOs:
  - TODO(DB_AND_ML_APIS): PostgreSQL schema/connection strings and marketplace API credentials
    (Mercado Livre, Shopee, Amazon) intentionally deferred — user will supply during
    Infrastructure implementation. Interfaces (`IApplicationDbContext`, `IReadDbConnection`,
    `IMarketplaceAdapter`) MUST be defined without leaking concrete secrets or SDKs.
-->

# MarketplaceAdvisory.Core Constitution

## Business Domain

**Product identity**: MarketplaceCopilot — a B2B SaaS platform for high-volume Brazilian
marketplace sellers (Mercado Livre, Shopee, Amazon). Its promise is to eliminate
"financial blindness": every sale, every listing, every automated action MUST make the
seller's real net profit visible and, whenever an automated write path affects price,
MUST protect it.

**Target user**: professional sellers ("PJ") with dozens to thousands of SKUs across
multiple marketplaces, with special support for the **auto-parts vertical** (part-code →
compatible-vehicle mapping).

**Module map (owned by this repository, `MarketplaceAdvisory.Core`)**:

- **A — Financial Engine** ("Guardião da Lucratividade"): net-profit calculation, traffic-
  light status, ideal-price reverse simulator. Owner of the profitability floor.
- **B — AI & Productivity Pipeline**: competitor scraping + AI title/description rewrite,
  background image processing (white `#FFFFFF` background), Kit/Bundle Maker.
- **C — Auto-parts Vertical**: fast search index of part codes → compatible
  vehicles/years/engines, strictly aligned with each marketplace's official
  compatibility catalog.
- **D — Smart SAC (Customer Service)**: unified inbox for pre/post-sale messages,
  context-aware AI reply drafts approved with one click.
- **E — Dynamic Repricer & Competitor Radar**: competitor price monitoring and automated
  repricing, bound by Principle VIII.

**Cross-cutting invariant**: every price-changing write MUST consult the Financial Engine
BEFORE persisting. This is enforced by Principle VIII.

## Core Principles

### I. Clean Architecture + DDD (NON-NEGOTIABLE)

## Core Principles

### I. Clean Architecture + DDD (NON-NEGOTIABLE)

Source code MUST be organized in four inward-pointing layers — `Domain`, `Application`,
`Infrastructure`, `Api` — with dependencies only flowing inward. The `Domain` layer MUST
NOT reference `Infrastructure`, `Api`, EF Core, MediatR, ASP.NET Core, or any third-party
adapter. Business invariants MUST live inside aggregates (protected constructors, factory
methods returning `ErrorOr<T>`) and be expressed through Value Objects (e.g., `Money`,
`TenantId`) rather than primitives. `Application` MUST depend only on `Domain` and on
abstractions it owns (`IApplicationDbContext`, `IReadDbConnection`, `ITenantContext`,
`ICurrentUser`, `ICacheService`). Rationale: keeps the business rules independent of
frameworks, enables fast unit testing, and makes marketplace/tenant/infra swaps safe.

### II. CQRS-lite with Command/Query Separation

Write and read paths MUST be physically separated. Commands MUST go through MediatR
handlers (`ICommand`/`ICommandHandler`) and persist via EF Core + Npgsql over
`IApplicationDbContext`. Queries MUST go through MediatR handlers (`IQuery`/`IQueryHandler`)
and read via Dapper over `IReadDbConnection`, bypassing EF Core for performance.
`FluentValidation` validators MUST run through the `ValidationBehavior` pipeline before any
handler executes. Controllers MUST NOT contain business logic — they translate HTTP into
`ISender.Send` calls and map `ErrorOr` results into `ProblemDetails`. Rationale: optimizes
each side independently and enforces a single, uniform entry point into the application.

### III. Multi-Tenancy by Default (NON-NEGOTIABLE)

Every `AggregateRoot<TId>` MUST carry a `TenantId`. The active tenant MUST be resolved
exclusively from the validated JWT `tenant_id` claim through `ITenantContext`; raw HTTP
headers, query strings, or route values MUST NOT be trusted as tenant sources. EF Core
global query filters MUST enforce `TenantId` isolation on every entity, and write
operations MUST assign the current tenant before saving. Cross-tenant reads or writes are
forbidden except through an explicit, documented administrative operation reviewed against
this constitution. Rationale: prevents data leakage across customers as the platform
grows.

### IV. RBAC via Security-Issued JWT

The `Core` MUST NOT issue tokens. Authentication MUST use JWT Bearer with strict
validation (`Issuer`, `Audience`, `IssuerSigningKey`, `Lifetime`), configured from the
shared `JwtSettings` section. Authorization MUST be declarative: endpoints requiring
elevated access MUST use `[Authorize(Roles = AppRoles.Manager)]` or a named policy
(`RequireManager`, `RequireUser`). Role and claim constants MUST come from
`MarketplaceAdvisory.SharedKernel.Authentication` (`AppRoles`, `AppClaimTypes`) — no magic
strings. Talking to the Security service (e.g., `IdentityProxyController`) MUST go through
a typed Refit client (`IIdentityAuthApi`). Rationale: keeps identity centralized and
avoids duplicating auth logic across services.

### V. Errors as Values (ErrorOr)

Application handlers MUST return `ErrorOr<T>` and MUST NOT throw exceptions for expected
business outcomes (validation failure, not-found, conflict, unauthorized, forbidden).
Domain factories (e.g., `Product.Create`) MUST return `ErrorOr<T>` when invariants can
fail. Controllers MUST translate `ErrorOr` errors into the correct HTTP status via the
existing `ToProblem` mapping (`Validation → 400`, `NotFound → 404`, `Conflict → 409`,
`Unauthorized → 401`, `Forbidden → 403`, else `500`). Truly unexpected failures MUST be
handled by `GlobalExceptionHandler` returning RFC 7807 `ProblemDetails`. Rationale: makes
expected error paths explicit, testable, and cheap.

### VI. Layered Testability with Domain Test-First

Every layer MUST have its own test project (`.Domain.Tests`, `.Application.Tests`,
`.Infrastructure.Tests`, `.Api.Tests`). New or changed **domain invariants** MUST ship
with tests that cover both the happy path and the error path (Test-First for domain).
`Application` handlers MUST be tested against in-memory or faked implementations of the
abstractions they consume. `Api` MUST be tested via `WebApplicationFactory<Program>` for
integration flows (auth, RBAC, versioning, ProblemDetails). Pull requests that reduce
coverage of domain invariants or remove existing tests without justification MUST be
rejected. Rationale: guarantees that the core rules stay correct as the platform grows.

### VII. Anti-Corruption Layer for Marketplace Integrations

Any interaction with Amazon, Mercado Livre, Shopee, or any future marketplace MUST go
through `IMarketplaceAdapter`, resolved by `IMarketplaceAdapterFactory`. The canonical
models (`CanonicalOrder`, `CanonicalListing`, `MarketplaceType`) in
`MarketplaceAdvisory.Integrations.Abstractions` MUST be the ONLY shape used by the
Application/Domain layers — marketplace-specific payloads MUST NOT leak beyond the
adapter. Each adapter MUST fail independently: a single marketplace outage MUST NOT
cascade into others. Rationale: isolates marketplace churn and quirks from business
logic, keeping the domain stable.

### VIII. Profitability Guardian (NON-NEGOTIABLE)

No automated write path — repricer, bulk update, bundle price suggestion, competitor-match
job, AI-generated listing — MAY persist a sale price that violates the Financial Engine's
computed **absolute minimum floor** for that SKU/tenant. The floor is the price at which
the deterministic Net Profit calculation (Rule A1: Sale Price − CMV − commission − fixed
unit fee − subsidized shipping − tax) equals the tenant-configured minimum margin (defaults
to 0%, meaning "never sell at a loss"). Every automated write MUST:

1. Call the Financial Engine BEFORE persisting a price change and store the calculated
   floor alongside the decision (auditability).
2. HOLD the price and emit a `ProfitabilityFloorHit` domain event when the floor would be
   violated — the operation MUST fail closed, not open.
3. Notify the seller via the SAC/dashboard channel and require **explicit human
   confirmation** to override; the override MUST be logged with actor, reason, and the
   exact loss accepted.

The traffic-light status (Green / Yellow / Red per Rule A2) MUST be recomputed on every
input change (cost, fee, shipping, tax, price) and MUST be reflected on the dashboard read
model without stale reads longer than the tenant-configured refresh SLA. Rationale: the
product's core promise is protecting the seller from selling at a loss; automation without
this guard is a product-defining bug, not a technical one.

## Security Requirements

- JWT Bearer validation MUST enable `ValidateIssuer`, `ValidateAudience`,
  `ValidateIssuerSigningKey`, and `ValidateLifetime`. `ClockSkew` MUST be at most one
  minute.
- The symmetric `SigningKey` used today is acceptable for local/dev; before production the
  Security service MUST move to asymmetric keys (RSA/JWKS) and the Core MUST validate
  against the published JWKS endpoint. This migration is a MAJOR amendment.
- Secrets (connection strings, signing keys, marketplace credentials) MUST NOT be
  committed. Local development MUST use dotnet User Secrets or `appsettings.*.local.json`
  files that are already gitignored; production MUST use environment variables or a
  secrets vault.
- All PII and financial data MUST be scoped by `TenantId` (Principle III). Cross-tenant
  aggregation is forbidden outside explicitly reviewed administrative operations.
- Health checks (Postgres, Redis) MUST NOT expose sensitive configuration in their
  response payloads.
- Structured logging (Serilog) MUST NOT log raw tokens, passwords, or full card/PII
  payloads. Correlation IDs from Aspire ServiceDefaults MUST be preserved.

## Development Workflow & Quality Gates

- Every change MUST land through a pull request reviewed by at least one maintainer;
  self-merges without review are forbidden.
- `dotnet build MarketplaceAdvisory.Core.sln` and `dotnet test MarketplaceAdvisory.Core.sln`
  MUST succeed before merge. CI MUST enforce this gate.
- Database schema changes MUST be delivered as EF Core migrations and applied via the
  dedicated out-of-process runner (`tools/MarketplaceAdvisory.Migrator`). The API process
  MUST NOT run migrations at startup in production.
- Public HTTP endpoints MUST be versioned via `/api/v{apiVersion}/...` using
  `Asp.Versioning`. Breaking changes MUST bump the major API version, not mutate an
  existing one.
- Endpoints MUST be documented via `ProducesResponseType<T>` and Swagger security
  definitions; new endpoints without OpenAPI annotation MUST be rejected in review.
- Public DTOs exchanged with the BFF or the Security service MUST live in
  `MarketplaceAdvisory.Contracts`. Any change to Contracts requires coordinated review
  with BFF and Security owners.
- Observability MUST rely on `MarketplaceAdvisory.Aspire.ServiceDefaults` (OpenTelemetry
  traces/metrics/logs, health checks). Custom telemetry MUST reuse the shared conventions.
- Background jobs MUST run under Quartz.NET with `[DisallowConcurrentExecution]` when
  they touch shared tenant state, and MUST log start/end with correlation.

## Governance

This constitution supersedes any other coding practice, style guide, or convention
whenever a conflict arises. Amendments MUST be delivered as a dedicated pull request that
(1) states the rationale, (2) bumps `CONSTITUTION_VERSION` per the rules below,
(3) updates `LAST_AMENDED_DATE`, and (4) includes a migration plan for code already
merged into `main` when the change is not backward compatible.

Semantic versioning of the constitution:

- **MAJOR** — a backward-incompatible removal or redefinition of a principle, of the
  Security Requirements section, or of the Governance rules (e.g., dropping Multi-Tenancy,
  switching signing algorithm).
- **MINOR** — a new principle, a new section, or a materially expanded rule.
- **PATCH** — clarifications, wording, typo fixes, or non-semantic refinements.

Reviewers MUST verify that each PR complies with every principle in this document and
MUST cite the specific principle when requesting changes. Complexity added to satisfy a
principle (extra abstraction, new library, new pattern) MUST be justified in the PR
description. Deviations MUST be documented as ADRs under `docs/adr/` (create the folder
if missing) and linked to the amendment PR.

**Version**: 1.1.0 | **Ratified**: 2026-09-24 | **Last Amended**: 2026-09-24
