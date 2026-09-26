# Phase 0 Research — MarketplaceCopilot Platform Foundation

**Date**: 2026-09-24
**Feature**: [spec.md](./spec.md) · **Plan**: [plan.md](./plan.md)

This document consolidates decisions taken to resolve open technical questions before design (Phase 1). Each decision is written as **Decision / Rationale / Alternatives considered**, per SpecKit convention.

---

## R1. Where does the Profitability Calculation live?

**Decision**: `ProfitabilityCalculatorService` is a **pure Domain service** with no framework dependencies. It takes a snapshot record (`ProfitabilityInputs`) and returns a `ProfitabilityCalculation` value object. The Application layer wraps it into commands/queries; Infrastructure only supplies the persisted inputs.

**Rationale**:

- Principle I: Domain must be framework-independent, so the math is trivially unit-testable and reproducible for audits (SC-008 requires reproducing historical decisions).
- Principle VI: the calculator is where the Test-First rule bites hardest — every Rule A1 / A2 / A3 branch must ship with tests.
- Principle VIII: the calculator produces the minimum-price floor value that guards every automated write path.

**Alternatives considered**:

- Application-layer service — rejected because it would couple math to MediatR and be harder to reuse from jobs and the guard.
- SQL-based calculation — rejected: not portable, hard to test, breaks Principle I, and the tax regime rules are conditional (Simples Nacional vs. Lucro Presumido) which are painful in SQL.

---

## R2. How is the Profitability Floor enforced across write paths?

**Decision**: introduce `IProfitabilityFloorGuard` in `Application/Common/Abstractions` with the signature:

```csharp
Task<ErrorOr<FloorDecision>> EvaluateAsync(FloorEvaluationRequest request, CancellationToken ct);
// FloorDecision is a discriminated union: Allowed(price) | HeldFloorHit(floor) | RequiresOverride(diff)
```

Every command that touches a sale price MUST call the guard before persistence. Overrides require a `HumanOverrideToken` argument that carries actor, reason and accepted loss; the guard verifies the token, logs it and emits `ProfitabilityFloorHit`.

**Rationale**:

- Principle VIII is NON-NEGOTIABLE — moving the check into a single abstraction makes it impossible to forget in future features (a new command that skips the guard fails a Constitution Check).
- ErrorOr keeps the "held" case as an explicit expected error, not an exception (Principle V).

**Alternatives considered**:

- MediatR pipeline behavior that intercepts commands — rejected: too magical, hides the invariant from the handler author and can't distinguish which commands actually change prices.
- Database check constraint — rejected: not expressive enough (thresholds vary per tenant/day) and would only fire at persist time, hiding intent from the domain.

---

## R3. Which search engine for Auto-parts (Rule C1)?

**Decision**: **Meilisearch** as the default concrete implementation of `ICompatibilitySearchIndex`, with Elasticsearch reserved as an alternative for tenants that already run one.

**Rationale**:

- Meilisearch has the lowest operational overhead for the pilot scale (SC-003 asks for < 500 ms p95 on the tenant SLA), ships typo-tolerance and prefix search out of the box (part codes have hyphens/spaces variability), and is easy to run in Docker Compose next to Postgres/Redis.
- The interface `ICompatibilitySearchIndex` isolates the choice — swapping to Elasticsearch is a Infrastructure-only change.

**Alternatives considered**:

- Elasticsearch — heavier ops, better for very large catalogs (>1M entries) or complex analytics. Kept as alternative.
- PostgreSQL full-text search — rejected for now: acceptable for a small MVP catalog but does not meet the typo-tolerance and prefix-search UX we want; can be added later behind the same interface.

---

## R4. AI provider abstraction

**Decision**: introduce `IAiProvider` with methods `RewriteListingAsync(RewriteRequest)`, `DraftReplyAsync(DraftRequest)`. Concrete adapters (OpenAI, Google Gemini, local) live in `Infrastructure/Content`. Every call is materialized as an `AIJob` aggregate before the outbound HTTP so failures never leave the write path in an inconsistent state.

**Rationale**:

- The user's business rules say "call a Generative AI API (e.g., OpenAI/Gemini)" — the choice is explicitly swappable.
- Materializing the job first satisfies FR-B4 (audit) and FR-B5 (fail closed).

**Alternatives considered**:

- Direct SDK usage in Application layer — rejected: couples Application to a specific vendor (Principle I).
- Sync AI calls in the request path — rejected: latency; blocks the seller UX. Jobs run via Quartz.

---

## R5. Background job runtime — Quartz vs. Hangfire?

**Decision**: **Quartz.NET**, already registered in the existing `Core.Infrastructure.DependencyInjection` (`AddQuartz` + `AddQuartzHostedService`). Extend with new job classes per family: `RepricerJob`, `CompetitorRadarJob`, `ListingRewriteJob`, `ImageCleanupJob`, `SacDraftJob`.

**Rationale**:

- Keeps the count of infra choices low (Principle: complexity must be justified).
- Quartz already has the `MarketplaceSyncJob` placeholder and `[DisallowConcurrentExecution]` pattern that we can copy.

**Alternatives considered**:

- Hangfire — nicer dashboard but adds a second scheduler, another table set and a licensing consideration for pro features; rejected on YAGNI grounds.
- Native `IHostedService` per family — rejected: reimplements retry/backoff/cron that Quartz already provides.

---

## R6. Image storage & processing back-end

**Decision**: two abstractions — `IImageStorage` (put/get) and `IImageProcessor` (background removal + white-fill). Local dev uses the file system; production picks between Azure Blob and S3 (deferred until deployment).

**Rationale**:

- Domain doesn't care where bytes live — Principle I.
- Deferring the concrete driver matches the user's directive ("a parte do banco de dados e conexão com as APIs do mercado livre deixe que vou adicionar assim que tiver"): same policy applies to blob store credentials.

**Alternatives considered**:

- Embedding the processor in the Api process — rejected: CPU/GPU-heavy, breaks the API SLO.
- A single `IImageService` combining storage + processing — rejected: violates single responsibility.

---

## R7. Tax regime pluggability

**Decision**: `ITaxRegime` interface in the Domain with two initial implementations: `SimplesNacionalRegime` (flat rate per tenant) and `LucroPresumidoRegime` (rate schedule). The regime is selected per tenant per effective-date window (see `TaxProfile` in the data model). The calculator receives the resolved regime as input.

**Rationale**:

- FR-A5 requires additional regimes to be pluggable without changing calling code.
- Regime is a strategy, not data — belongs in Domain.

**Alternatives considered**:

- Enum + switch inside the calculator — rejected: violates open/closed; every new regime forces edits to the calculator.
- Table-driven rules only — rejected: Lucro Presumido rate schedule is non-trivial (per-activity brackets), a strategy composes rules better.

---

## R8. Contracts DTOs vs. Domain models

**Decision**: continue the existing pattern — new DTOs live in `MarketplaceAdvisory.Contracts` and are the only shape exchanged with BFF/Security/Angular. Domain aggregates never leak through the API boundary.

**Rationale**:

- Principle IV/VII: the outside world speaks Contracts, not Domain.
- Enables independent versioning of the API vs. the Domain.

**Alternatives considered**:

- Auto-generated DTOs via Mapster only — rejected: while Mapster is used for mapping, we still hand-author DTOs for stability.

---

## R9. Multi-tenant isolation in read model (Dapper)

**Decision**: `IReadDbConnection.CreateConnection()` returns an opened connection scoped by tenant. All Dapper queries filter by the `@TenantId` parameter injected from `ITenantContext`. Guard rails: a Roslyn analyzer rule (added to `Directory.Build.props`) will fail the build if a Dapper `Query*` call is executed without a `@TenantId` parameter — deferred to a follow-up ticket in `tasks.md`.

**Rationale**:

- EF global query filters only cover the write model; Dapper reads must be enforced separately (Principle III is NON-NEGOTIABLE).

**Alternatives considered**:

- Per-tenant DB schema/connection pool — future option for very large tenants; overkill for the pilot.
- Row-level security in PostgreSQL — good defense-in-depth, revisit once schema is defined (deferred with DB config).

---

## R10. Repricer decision cadence

**Decision**: default cadence is every 5 minutes per marketplace per tenant. Configurable per tenant. Uses Quartz cron trigger, per-marketplace to satisfy FR-E5 (independent failure domains).

**Rationale**:

- Balances competitor-radar responsiveness with API rate limits from each marketplace.
- Independent per-marketplace triggers keep failure domains isolated.

**Alternatives considered**:

- Every minute — risks rate-limit bans; expensive.
- Reactive/webhook-based — marketplaces don't offer this uniformly; kept as future enhancement per adapter.

---

## Deferred / TODOs

- **DB_SCHEMA**: PostgreSQL schema, EF migrations content and connection strings deferred to Infrastructure implementation (user will supply).
- **ML_CREDENTIALS**: Mercado Livre / Shopee / Amazon API credentials and endpoints deferred; adapters remain contract-only until then.
- **AI_PROVIDER_CHOICE**: pilot provider (OpenAI vs. Gemini) not chosen; adapter is behind `IAiProvider` and can be switched with no Domain change.
- **BLOB_STORE**: image storage driver deferred until deployment; abstraction exists.
- **ROSLYN_ANALYZER**: build-time enforcement of Principle III in Dapper reads (R9) tracked as a task, not a blocker.

All items above are captured as work in `tasks.md` and gated on the user supplying the missing configuration.
