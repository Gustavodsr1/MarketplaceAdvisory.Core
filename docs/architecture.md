# Architecture — MarketplaceAdvisory.Core

> This document is the compact architectural overview of the Core service. It lives at the repository root under `docs/` and is the entry point for developers, reviewers and future SpecKit sessions.
>
> Deeper sources:
>
> - Principles → `.specify/memory/constitution.md`
> - Feature specs → `specs/<###>-<name>/` (`spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`, `tasks.md`)
> - Endpoint reference → `docs/endpoints.md`

## 1. Product identity

**MarketplaceCopilot** is a B2B SaaS platform for high-volume Brazilian marketplace sellers (Mercado Livre, Shopee, Amazon). It eliminates the "financial blindness" of professional sellers by:

- calculating real-time net profit and traffic-light status per SKU per marketplace (**Financial Engine**);
- protecting every automated write path from selling at a loss (**Profitability Guardian**, Constitution Principle VIII);
- generating SEO-safe listings and cleaning product images (**AI & Productivity Pipeline**);
- mapping manufacturer part codes to compatible vehicles for the auto-parts vertical (**Auto-parts Search**);
- centralizing the marketplace SAC inbox with one-click AI-drafted replies (**Smart SAC**);
- competing automatically for the Buy Box, but never below the floor (**Dynamic Repricer**).

## 2. Bounded contexts (three repositories)

```
┌────────────────────────┐   JWT   ┌────────────────────────┐
│ MarketplaceAdvisory.   │◀────────│ MarketplaceAdvisory.   │
│         BFF            │         │       Security         │
│  (Angular gateway)     │         │ (JWT issuer for User/  │
│                        │         │  Manager roles)        │
└─────────┬──────────────┘         └────────────────────────┘
          │ forwards Authorization
          ▼
┌────────────────────────┐
│ MarketplaceAdvisory.   │
│         Core           │  ◀── this repository
│ (business rules,       │
│  Financial Engine,     │
│  ACL, jobs)            │
└────────────────────────┘
          │
          │ ACL: IMarketplaceAdapter (Principle VII)
          ▼
   [Mercado Livre] [Shopee] [Amazon]  (deferred credentials)
```

- **Security**: standalone Identity Provider. Issues JWTs with `role` (User / Manager) and `tenant_id` claims. Symmetric key today; RSA/JWKS is the production plan (Constitution — Security Requirements).
- **BFF**: gateway tailored for the Angular front-end. Uses Refit to reach Core, forwards the bearer token.
- **Core**: everything else — this repository. Validates the JWT; enforces multi-tenancy; runs the business rules.

## 3. Layering (Clean Architecture + DDD)

Dependencies flow **inward only**. Nothing in `Domain` references `Infrastructure`, `Api`, EF Core, MediatR, ASP.NET, or any marketplace SDK.

```
┌──────────────────────────────────────────────────────────────┐
│  Api                                                         │
│  Controllers · Authentication · GlobalExceptionHandler ·     │
│  Swagger · JWT Bearer                                        │
└──────────────────────────┬───────────────────────────────────┘
                           │ uses
                           ▼
┌──────────────────────────────────────────────────────────────┐
│  Application                                                 │
│  Commands / Queries (MediatR) · Validation Behavior          │
│  Abstractions: IApplicationDbContext, IReadDbConnection,     │
│    ITenantContext, ICurrentUser, ICacheService,              │
│    IProfitabilityFloorGuard, IAiProvider, IImageProcessor,   │
│    IImageStorage, ICompatibilitySearchIndex                  │
└──────────────────────────┬───────────────────────────────────┘
                           │ uses
                           ▼
┌──────────────────────────────────────────────────────────────┐
│  Domain                                                      │
│  Aggregates (Product, Bundle, Tenant, MarketplaceFee,        │
│    ShippingTier, TaxProfile, ProfitabilityCalculation,       │
│    RepricerDecision, CompetitorObservation, AIJob,           │
│    ListingDraft, CustomerMessage, MessageReplyDraft,         │
│    ListingCompatibility)                                     │
│  Value Objects (Money, TenantId, Weight, Dimensions,         │
│    ProfitabilityThresholds, HumanOverrideToken, ...)         │
│  Domain Services (ProfitabilityCalculatorService,            │
│    IdealPriceSimulator, ITaxRegime + impls)                  │
│  Domain Events                                               │
└──────────────────────────────────────────────────────────────┘
                           ▲ implements
                           │
┌──────────────────────────┴───────────────────────────────────┐
│  Infrastructure                                              │
│  Persistence (EF Core writes · Dapper reads · Repositories)  │
│  Cache (Redis)                                               │
│  BackgroundJobs (Quartz.NET)                                 │
│  Integrations (IMarketplaceAdapter impls, Factory)           │
│  Content (AI provider adapter, Image processor, Storage)     │
│  Autoparts (Meilisearch adapter)                             │
│  Tenancy (NullTenantContext for non-HTTP paths)              │
└──────────────────────────────────────────────────────────────┘
```

## 4. CQRS-lite (writes vs reads)

- **Write path** — Controller → MediatR `ISender.Send(command)` → `ICommandHandler<TCommand>` → `IApplicationDbContext` (EF Core + Npgsql) → PostgreSQL.
- **Read path** — Controller → MediatR `ISender.Send(query)` → `IQueryHandler<TQuery>` → `IReadDbConnection` (Dapper) → PostgreSQL views listed in `data-model.md`.
- **FluentValidation** runs through `ValidationBehavior` in the MediatR pipeline; validators are scanned from the Application assembly.
- **Mapster** handles Domain-to-Contracts mapping; no Domain type leaves the API boundary.

## 5. Multi-tenancy (Principle III)

Every request MUST resolve its tenant from the JWT `tenant_id` claim via `ITenantContext`:

```
JWT (validated)
   │
   ▼
CurrentUser  ─┐
TenantContext ┴─► scoped per HTTP request
   │
   ▼
ApplicationDbContext.OnModelCreating
   HasQueryFilter(x => !tenantId.HasValue || x.TenantId == tenantId.Value)
Dapper queries
   WHERE tenant_id = @TenantId
```

Cross-tenant reads or writes are forbidden — Roslyn analyzer to enforce Dapper queries carry `@TenantId` is a follow-up task (`tasks.md` T034 note).

## 6. Profitability Guardian (Principle VIII)

The invariant that shapes the platform:

```
Every command that persists a sale price (SetSalePrice, RunRepricerDecision,
CreateBundle, ApproveDraft-with-price)
   │
   ▼
IProfitabilityFloorGuard.EvaluateAsync(...)
   │
   ├── Allowed(price)              → persist, emit ProfitabilityCalculated
   ├── HeldFloorHit(floor)         → hold, emit ProfitabilityFloorHit, notify seller
   └── RequiresOverride(diff)      → return 409, retry with HumanOverrideToken
```

The guard reuses `ProfitabilityCalculatorService`, so the math is the same everywhere. Every automated decision is materialized as an immutable `ProfitabilityCalculation` (auditability, SC-008).

## 7. Anti-Corruption Layer (Principle VII)

All marketplace I/O flows through `IMarketplaceAdapter` (in `MarketplaceAdvisory.Integrations.Abstractions`) and `IMarketplaceAdapterFactory` (in Infrastructure). Adapters translate between marketplace-native payloads and the **canonical models**:

- `CanonicalOrder`, `CanonicalListing` (existing)
- `CanonicalQuestion`, `CanonicalMessage`, `CanonicalCompetitorObservation` (added in feature 001)

A single marketplace outage never blocks the others — jobs and controllers depend on the adapter interface only.

## 8. Background jobs (Quartz.NET)

| Job | Cadence (default) | Purpose |
|-----|-------------------|---------|
| `RepricerJob` | 5 min per marketplace | Compare competitor prices vs floor; lower or hold. |
| `CompetitorRadarJob` | 5 min per marketplace | Refresh `CompetitorObservation` snapshots. |
| `ListingRewriteJob` | queue-driven | Pop `AIJob(kind=ListingRewrite)` from Redis and call `IAiProvider`. |
| `ImageCleanupJob` | queue-driven | Pop `AIJob(kind=ImageCleanup)` and call `IImageProcessor`. |
| `SacDraftJob` | queue-driven | Draft AI replies for new inbound messages. |
| `MarketplaceSyncJob` | hourly (placeholder) | Orders / listings sync (already scaffolded). |

Every job is `[DisallowConcurrentExecution]` when it touches shared tenant state and logs start/end with correlation IDs.

## 9. Configuration & secrets

- Connection strings, JWT settings, marketplace API keys, AI provider keys → configuration providers (User Secrets in dev, Env / Vault in prod).
- Nothing sensitive lives in source. Local overrides use `appsettings.Development.local.json` (gitignored) or User Secrets.
- Health checks probe PostgreSQL + Redis; response payloads never expose config values.

## 10. Observability

`MarketplaceAdvisory.Aspire.ServiceDefaults`:

- OpenTelemetry traces + metrics + logs.
- Serilog structured logs (never logs raw tokens/PII).
- Health check endpoints (`/health`, `/alive`).
- Correlation IDs propagated across BFF → Core → Adapters.

## 11. Testing strategy (Principle VI)

| Layer | Test project | What it covers |
|-------|--------------|----------------|
| Domain | `MarketplaceAdvisory.Core.Domain.Tests` | Aggregates, VOs, factories, `ProfitabilityCalculatorService`, `IdealPriceSimulator`. Test-First. |
| Application | `MarketplaceAdvisory.Core.Application.Tests` | MediatR handlers with faked `IApplicationDbContext`, `ITenantContext`, `IProfitabilityFloorGuard`, `IAiProvider`, etc. |
| Infrastructure | `MarketplaceAdvisory.Core.Infrastructure.Tests` | Testcontainers PostgreSQL + Redis. Multi-tenant isolation, EF configurations, Dapper views. |
| Api | `MarketplaceAdvisory.Core.Api.Tests` | `WebApplicationFactory<Program>` for auth, RBAC, versioning, ProblemDetails. |

Fakes: `FakeAiProvider`, `FakeImageProcessor`, `LocalImageStorage`, `FakeCompatibilitySearchIndex`, per-marketplace `FakeMarketplaceAdapter` — all in test assemblies so the app never depends on them.

## 12. Repository layout (delta after feature 001)

Refer to `specs/001-marketplacecopilot-platform/plan.md#project-structure` for the full layout. Highlights:

- `Domain/` gets `Financial/`, `Pricing/`, `Content/`, `Support/`, `Autoparts/`, `Tenants/` subfolders.
- `Application/` mirrors the same modules with `Commands/` and `Queries/`.
- `Infrastructure/` mirrors modules for adapter homes.
- `Api/Controllers/` grows one controller per module.
- `Shared/Contracts/` grows one DTO folder per module.

## 13. Deferred items (waiting on the user)

Not blockers for the architectural work, but real blockers for full end-to-end:

- PostgreSQL schema + connection strings (`tasks.md` T033).
- Marketplace API credentials for Mercado Livre / Shopee / Amazon (`tasks.md` T047).
- AI provider key (`tasks.md` T041).
- Blob storage driver for images (`tasks.md` T042).

Concrete adapters ship as fakes today so the platform can be built, tested and demoed without any external dependency.

## 14. Constitution mapping (quick check)

| Principle | Where it lives |
|-----------|----------------|
| I. Clean Architecture + DDD | Layer folders; Domain has zero framework references |
| II. CQRS-lite | Command/Query separation via MediatR + Dapper |
| III. Multi-Tenancy | `ITenantContext`, EF global filters, Dapper `@TenantId` |
| IV. RBAC via JWT | `Program.cs` JWT Bearer + `RequireManager` / `RequireUser` policies |
| V. Errors as Values | `ErrorOr<T>` in every handler; `ToProblem` mapping in controllers |
| VI. Layered Testability | Four `.Tests` projects; Test-First for Domain |
| VII. Anti-Corruption Layer | `IMarketplaceAdapter` + `MarketplaceAdapterFactory` |
| VIII. Profitability Guardian | `IProfitabilityFloorGuard` on every price-changing command |
