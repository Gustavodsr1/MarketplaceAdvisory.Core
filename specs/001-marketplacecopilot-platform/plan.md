# Implementation Plan: MarketplaceCopilot Platform Foundation

**Branch**: `001-marketplacecopilot-platform` | **Date**: 2026-09-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-marketplacecopilot-platform/spec.md`

**Note**: This plan is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Deliver the foundational SaaS platform for high-volume Brazilian marketplace sellers, structured around five modules — Financial Engine (P1, MVP), Dynamic Repricer (P2), AI & Productivity Pipeline (P3), Auto-parts Search (P4) and Smart SAC (P5). The technical approach preserves the existing `MarketplaceAdvisory.Core` layering (Clean Architecture + DDD, CQRS-lite over EF Core + Dapper, MediatR handlers, multi-tenant global query filters, RBAC from Security-issued JWTs) and adds:

- A **deterministic `ProfitabilityCalculatorService`** as a pure Domain service, covered by exhaustive unit tests (Constitution I + VI + VIII).
- An **automated-writers guard** (`IProfitabilityFloorGuard`) that every price-changing handler MUST consult, enforcing Principle VIII (Profitability Guardian) at the Application layer.
- **Quartz.NET jobs + Redis-backed queues** for AI listing rewrite, image background removal, competitor observation and repricing.
- **Refit clients** for Security (already wired) and, in future infra iterations, for Mercado Livre / Shopee / Amazon behind `IMarketplaceAdapter` (Principle VII).
- A **canonical search abstraction** (`ICompatibilitySearchIndex`) for Auto-parts, defaulting to Meilisearch (see Phase 0 research).
- **Contracts DTOs** and **BFF endpoints** completing the vertical from Angular front-end down to Domain.

Database engine (PostgreSQL) and marketplace credentials are **explicitly deferred** — Domain/Application layers use `IApplicationDbContext`, `IReadDbConnection`, `IMarketplaceAdapter`, `IAiProvider`, `IImageProcessor` abstractions; concrete infra wiring lands after the user supplies credentials/schema.

## Technical Context

**Language/Version**: C# / .NET 10 (see `global.json` and existing `Directory.Build.props`).

**Primary Dependencies**:

- MediatR (behind `ICommand`/`IQuery` abstractions) with `ValidationBehavior` (FluentValidation) already wired.
- Entity Framework Core + Npgsql for command-side writes; Dapper for query-side reads.
- StackExchange.Redis (via `AddStackExchangeRedisCache`) for cache + queue backing.
- Quartz.NET for scheduled/background jobs (marketplace sync, repricer, AI drafting).
- Refit for typed HTTP clients (Security, marketplace adapters, AI provider, image processor).
- Mapster + FluentValidation + ErrorOr already registered in `Application.DependencyInjection`.
- Serilog + Aspire ServiceDefaults (OpenTelemetry, health checks) already in `Program.cs`.
- **Deferred**: Meilisearch .NET client (Auto-parts search), an AI SDK (OpenAI or Google Gemini), and any image-processing SDK — all behind swappable abstractions.

**Storage**:

- PostgreSQL (write model) via EF Core — connection string configured in `appsettings.json` (`ConnectionStrings:Postgres`) — schema and credentials deferred to user.
- PostgreSQL (read model) via Dapper — same connection string.
- Redis for distributed cache, rate limiting hints and background job queues.
- Meilisearch (or Elasticsearch, swappable) for Auto-parts compatibility index — index schema and credentials deferred.
- File/object storage for product images pre/post-processing — abstracted behind `IImageStorage`; concrete driver deferred (Azure Blob / S3 / local dev).

**Testing**:

- xUnit for all test projects (`.Domain.Tests`, `.Application.Tests`, `.Infrastructure.Tests`, `.Api.Tests`), already present.
- FluentAssertions expected for readability (add as package if not already registered in `Directory.Packages.props`).
- Api tests use `WebApplicationFactory<Program>` — the existing `public partial class Program {}` in `Core.Api/Program.cs` already exposes it.
- Testcontainers for PostgreSQL + Redis on Infrastructure tests (add to `Directory.Packages.props` when writing tests that require real backends).

**Target Platform**: Linux/Windows containers, `.NET 10` runtime, deployable to any Aspire-friendly hosting; local dev via Docker Compose (`docker compose up -d` → PostgreSQL + Redis + Seq).

**Project Type**: HTTP web service (Clean Architecture: `Domain`, `Application`, `Infrastructure`, `Api`) plus supporting `tools/MarketplaceAdvisory.Migrator` (out-of-process EF migrations).

**Performance Goals**:

- Financial Engine calculation: p95 < 20 ms per SKU on Domain-service call, p95 < 200 ms end-to-end HTTP.
- Traffic-light dashboard read (Dapper): p95 < 300 ms for a page of 100 SKUs per tenant.
- Auto-parts search: p95 < 500 ms per query.
- Repricer decision loop: process 10k SKUs / minute per tenant (target).

**Constraints**:

- Multi-tenant global query filters MUST be non-bypassable outside explicit admin operations (Principle III).
- No automated write path may violate Principle VIII (Profitability Guardian).
- Marketplace credentials and DB connection strings MUST come from configuration/secrets provider, never from code.

**Scale/Scope**:

- Target initial pilot: 20 tenants, 100k SKUs total, 3 marketplaces, 5 background job families.
- Domain complexity: 12 key entities (Tenant, Product, MarketplaceFee, ShippingTier, TaxProfile, ProfitabilityCalculation, RepricerDecision, Bundle, AIJob, ListingCompatibility, CustomerMessage, CompetitorObservation) plus 1 enum (`ProfitabilityStatus`).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Evaluated against `MarketplaceAdvisory.Core Constitution v1.1.0`.

| # | Principle | Verdict | Justification |
|---|-----------|---------|---------------|
| I | Clean Architecture + DDD (NON-NEGOTIABLE) | ✅ PASS | Feature reuses existing `Domain`/`Application`/`Infrastructure`/`Api` layout. `Product`, `Bundle`, `Tenant` aggregates carry invariants via factory methods returning `ErrorOr<T>`. `Money`, `TenantId`, plus new value objects (`Weight`, `Dimensions`, `TaxRate`, `Commission`) live in `Domain` / `SharedKernel`. |
| II | CQRS-lite | ✅ PASS | Every module exposes commands (writes via EF Core `IApplicationDbContext`) and queries (reads via Dapper `IReadDbConnection`) through MediatR. Controllers translate HTTP into `ISender.Send`; no business logic in controllers. |
| III | Multi-Tenancy by Default (NON-NEGOTIABLE) | ✅ PASS | Every new aggregate carries `TenantId`. Global query filter extended per entity in `ApplicationDbContext.OnModelCreating`. `ITenantContext` still sources tenant from the validated JWT `tenant_id` claim. |
| IV | RBAC via Security-Issued JWT | ✅ PASS | Existing `JwtBearer` + `RequireManager`/`RequireUser` policies extended: `POST /financial/simulator`, `POST /repricer/*`, `POST /listings/ai/*`, `POST /sac/reply` require `Manager`; read endpoints require `User`. |
| V | Errors as Values (ErrorOr) | ✅ PASS | New handlers return `ErrorOr<T>`. Domain failures (invalid CMV, negative weight, invalid tax rate, floor-breach) surface as `Error.Validation` / `Error.Conflict`. Controllers reuse the existing `ToProblem` mapping. |
| VI | Layered Testability with Domain Test-First | ✅ PASS | `Domain.Tests` will ship first for `ProfitabilityCalculatorService` (Rule A1) and `IdealPriceSimulator` (Rule A3), covering Green/Yellow/Red and floor computations. `Application.Tests` cover handlers with fakes for `IApplicationDbContext`, `IProfitabilityFloorGuard`, `IMarketplaceAdapter`, `IAiProvider`. `Api.Tests` cover auth + RBAC + versioning per endpoint. |
| VII | Anti-Corruption Layer for Marketplace Integrations | ✅ PASS | All marketplace calls (competitor scraping for repricer, listing publish, SAC message send, order sync) go through `IMarketplaceAdapter` / `IMarketplaceAdapterFactory`. Canonical models (`CanonicalOrder`, `CanonicalListing`) extended with `CanonicalQuestion`, `CanonicalMessage`, `CanonicalCompetitorObservation`. |
| VIII | Profitability Guardian (NON-NEGOTIABLE) | ✅ PASS | New `IProfitabilityFloorGuard` MUST be consulted by every price-changing handler (`SetSalePrice`, `RepricerDecideCommand`, `PublishBundleCommand`, `PublishAiListingCommand`). Guard returns `ErrorOr<FloorDecision>` with `Allowed | HeldFloorHit | RequiresOverride`. Overrides carry actor/reason/loss and emit `ProfitabilityFloorHit` events. |

**Post-Phase-1 re-check**: performed at the end of this plan (see [Post-Design Constitution Re-check](#post-design-constitution-re-check)).

## Project Structure

### Documentation (this feature)

```text
specs/001-marketplacecopilot-platform/
├── spec.md              # /speckit-specify output
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (public HTTP contracts)
│   ├── financial-engine.md
│   ├── repricer.md
│   ├── ai-productivity.md
│   ├── autoparts.md
│   └── sac.md
├── checklists/
│   └── requirements.md  # /speckit-specify quality checklist
└── tasks.md             # /speckit-tasks output (created in the next phase)
```

### Source Code (repository root)

```text
MarketplaceAdvisory.Core/
├── MarketplaceAdvisory.Core.Domain/
│   ├── Catalog/                     # Product, Bundle, ListingCompatibility
│   ├── Financial/                   # ProfitabilityCalculation, TaxProfile, MarketplaceFee, ShippingTier, ProfitabilityStatus, ProfitabilityCalculatorService
│   ├── Pricing/                     # RepricerDecision, CompetitorObservation, IProfitabilityFloorGuard (contract)
│   ├── Content/                     # AIJob, ListingDraft
│   ├── Support/                     # CustomerMessage, MessageThread
│   ├── Tenants/                     # Tenant (aggregate) + TenantThresholds
│   └── Common/                      # AggregateRoot, Entity, IDomainEvent, existing base classes
├── MarketplaceAdvisory.Core.Application/
│   ├── Financial/
│   │   ├── Commands/                # UpdateProductCosts, SetSalePrice, SetTenantThresholds
│   │   └── Queries/                 # GetProductProfitability, GetProfitabilityDashboard, RunIdealPriceSimulator
│   ├── Pricing/
│   │   ├── Commands/                # EnableRepricer, DisableRepricer, RunRepricerDecision (internal, called by job)
│   │   └── Queries/                 # GetRepricerHistory, GetCompetitorObservations
│   ├── Content/
│   │   ├── Commands/                # RewriteListingFromCompetitor, CleanProductImage, CreateBundle
│   │   └── Queries/                 # GetAiJobStatus, GetBundleProfitability
│   ├── Autoparts/
│   │   ├── Commands/                # AttachCompatibilityToProduct
│   │   └── Queries/                 # SearchCompatibilityByPartCode
│   ├── Support/
│   │   ├── Commands/                # DraftReply, ApproveReply, SendReply
│   │   └── Queries/                 # GetInbox, GetThread
│   └── Common/
│       ├── Abstractions/            # IProfitabilityFloorGuard, IAiProvider, IImageProcessor, IImageStorage, ICompatibilitySearchIndex, existing abstractions
│       └── Behaviors/               # ValidationBehavior (existing)
├── MarketplaceAdvisory.Core.Infrastructure/
│   ├── Persistence/                 # ApplicationDbContext extended, new EF configurations, Dapper read models
│   ├── Financial/                   # (nothing beyond DB; calculator is pure Domain)
│   ├── Pricing/                     # RepricerJob (Quartz), CompetitorRadarJob
│   ├── Content/                     # AiProvider adapter, ImageProcessor adapter, ListingRewriteJob, ImageCleanupJob
│   ├── Autoparts/                   # MeilisearchCompatibilitySearchIndex (behind ICompatibilitySearchIndex)
│   ├── Support/                     # SacInboxProjector (background), CustomerMessage repo
│   ├── Integrations/                # Existing MercadoLivre/Shopee/Amazon adapters extended with question/message/competitor endpoints
│   ├── Cache/                       # RedisCacheService (existing)
│   └── Tenancy/                     # NullTenantContext (existing)
└── MarketplaceAdvisory.Core.Api/
    ├── Controllers/
    │   ├── FinancialController.cs       # /api/v1/financial/*
    │   ├── PricingController.cs         # /api/v1/pricing/*
    │   ├── ListingsController.cs        # /api/v1/listings/*  (AI + Bundle)
    │   ├── AutopartsController.cs       # /api/v1/autoparts/*
    │   ├── SacController.cs             # /api/v1/sac/*
    │   ├── ProductsController.cs        # existing (retained)
    │   └── IdentityProxyController.cs   # existing (retained)
    ├── Authentication/                  # existing CurrentUser + TenantContext
    └── Program.cs                       # existing wiring + new services

MarketplaceAdvisory.Shared/
├── MarketplaceAdvisory.Contracts/
│   ├── Financial/                   # ProductProfitabilityDto, TrafficLightSnapshotDto, SimulatorRequestDto, SimulatorResponseDto
│   ├── Pricing/                     # RepricerConfigDto, RepricerDecisionDto, CompetitorObservationDto
│   ├── Content/                     # RewriteRequestDto, RewriteResultDto, BundleRequestDto, BundleDto, ImageJobDto
│   ├── Autoparts/                   # CompatibilitySearchRequestDto, CompatibilityResultDto
│   └── Support/                     # InboxMessageDto, DraftReplyDto, SendReplyRequestDto
└── MarketplaceAdvisory.Integrations.Abstractions/
    ├── IMarketplaceAdapter.cs       # extended with question/message/competitor operations
    └── Models/                      # CanonicalQuestion, CanonicalMessage, CanonicalCompetitorObservation added
```

**Structure Decision**: keep the existing four-layer layout of `MarketplaceAdvisory.Core` and organize new code by **module** inside each layer (`Catalog`, `Financial`, `Pricing`, `Content`, `Autoparts`, `Support`, `Tenants`) rather than by pattern. This co-locates rules per business capability while preserving the Clean Architecture direction of dependencies. `Contracts` grows a folder per module; `Integrations.Abstractions` grows canonical models per capability.

## Complexity Tracking

> No Constitution violations. The plan is a strict extension of the existing architecture; no new project, no new pattern beyond the ones already ratified by the Constitution.

## Post-Design Constitution Re-check

Re-evaluated after drafting `data-model.md`, `contracts/` and `quickstart.md`:

- **Principle I (Clean+DDD)** — unchanged; Domain services (`ProfitabilityCalculatorService`, `IdealPriceSimulator`) have zero framework references.
- **Principle II (CQRS-lite)** — every user story maps to explicit Commands/Queries; no shortcuts.
- **Principle III (Multi-Tenancy)** — every new aggregate carries `TenantId`; every new EF configuration adds the global query filter.
- **Principle IV (RBAC)** — every new endpoint declares `[Authorize]` policy in the contracts.
- **Principle V (ErrorOr)** — every handler signature in `contracts/` returns `ErrorOr<T>` or `ErrorOr<Success>`; controllers reuse `ToProblem`.
- **Principle VI (Testability)** — `quickstart.md` lists Test-First checkpoints per user story before implementation.
- **Principle VII (ACL)** — new marketplace operations (question/message/competitor) are added to `IMarketplaceAdapter`; no marketplace-native payload leaks past the adapter.
- **Principle VIII (Profitability Guardian)** — every command that changes a sale price (`SetSalePrice`, `RunRepricerDecision`, `CreateBundle`, `RewriteListingFromCompetitor` when it proposes a price) MUST call `IProfitabilityFloorGuard.Evaluate(...)` and honor the returned `FloorDecision`.

Verdict: **PASS**. Proceed to `/speckit-tasks`.
