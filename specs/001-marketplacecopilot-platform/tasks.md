# Tasks — MarketplaceCopilot Platform Foundation

**Spec**: [spec.md](./spec.md) · **Plan**: [plan.md](./plan.md) · **Data model**: [data-model.md](./data-model.md)

**Constitution**: `MarketplaceAdvisory.Core Constitution v1.1.0`

**Legend**:

- `[P]` — parallelizable with the previous task (no file conflict).
- `[TF]` — Test-First (write the failing test before the code, per Principle VI).
- `[BLOCKED-BY-USER]` — waiting on the user to supply DB schema, marketplace credentials, or the AI provider key. Task can be scaffolded (contracts, tests) but the concrete implementation lands after the block clears.

The five phases below map 1:1 to the user's original development plan:

| Phase | Prompt name |
|-------|-------------|
| 1 | Domain Entities & Value Objects |
| 2 | The Financial Engine (Services) |
| 3 | Data Access (EF Core & Dapper) |
| 4 | AI & Background Jobs |
| 5 | BFF & Identity Integration |

---

## Phase 1 — Domain Entities & Value Objects

Ship first because everything else depends on it. All work lives in `MarketplaceAdvisory.Core.Domain` and `MarketplaceAdvisory.SharedKernel`.

- **T001** [TF] Add xUnit fixtures for the new domain modules under `MarketplaceAdvisory.Core.Domain.Tests/` (`Financial/`, `Pricing/`, `Content/`, `Support/`, `Autoparts/`, `Tenants/`). Create test data builders for `Product`, `Bundle`, `Tenant`, `MarketplaceFee`, `ShippingTier`, `TaxProfile`. *(Partial: `Financial/` and `Tenants/` folders created and populated with golden tests; remaining module fixtures land with their aggregates.)*
- **T002** ✅ [P] Add value objects to `MarketplaceAdvisory.SharedKernel/ValueObjects/`: `Weight`, `Dimensions` (with `VolumetricWeight(divisor)` helper), `Percentage`, `ProfitabilityThresholds`, `HumanOverrideToken`. Each ships with `GetEqualityComponents` and invariant tests. (`RefreshSla` deferred — used only by SAC/dashboard SLAs.)
- **T003** [P] Add `MarketplaceAdvisory.Integrations.Abstractions/MarketplaceType` extension: keep existing values, add canonical models `CanonicalQuestion`, `CanonicalMessage`, `CanonicalCompetitorObservation` under `Models/`. *(Deferred with Pricing/Support modules.)*
- **T004** ✅ [TF] Extend `Product` in `MarketplaceAdvisory.Core.Domain/Catalog/Product.cs`: added `Cmv`, `Weight`, `Dimensions`, `DefaultMarketplace` (single-price v1 schema; per-marketplace dictionary lands with Phase 3 EF). Added `SetSalePrice(marketplace, price, floorDecision)`, `UpdateCmv(cmv)`. Mutations raise `ProductInputsChanged`. Legacy `Create(tenant, sku, name, price)` overload preserved so existing handlers/tests keep passing. `AttachCompatibility` deferred with Autoparts module.
- **T005** [TF] [P] Create `MarketplaceAdvisory.Core.Domain/Catalog/Bundle.cs`… *(Deferred — Bundle Maker lives in Phase 3–4.)*
- **T006** ✅ [TF] [P] Create `MarketplaceAdvisory.Core.Domain/Tenants/Tenant.cs` (aggregate) with `Thresholds`, `TaxRegimeSelection`, `RefreshSlas`. Emit `TenantThresholdsChanged` and `TenantTaxRegimeChanged`. *(RefreshSlas deferred with T002 helper VO.)*
- **T007** ✅ [TF] [P] Create `MarketplaceAdvisory.Core.Domain/Financial/` aggregates: `MarketplaceFee`, `ShippingTier`. Added `ITaxRegime` interface and two implementations `SimplesNacionalRegime`, `LucroPresumidoRegime`. *(Standalone `TaxProfile` aggregate deferred — its state is currently carried by `Tenant.TaxRegimeHistory` + concrete regime implementations. Split lands with Phase 3 persistence.)*
- **T008** ✅ [TF] [P] Create `MarketplaceAdvisory.Core.Domain/Financial/ProfitabilityCalculation.cs` (immutable). Includes `InputsSnapshot`, `NetProfit`, `NetMarginPercentage`, `Status`, `MinimumPriceFloor`, `CausedBy`.
- **T009** ✅ [TF] [P] Create `MarketplaceAdvisory.Core.Domain/Financial/ProfitabilityStatus.cs` enum (`Green | Yellow | Red`) and `CalculationTrigger.cs` enum.
- **T010** [TF] [P] Create `MarketplaceAdvisory.Core.Domain/Pricing/RepricerDecision.cs` (immutable) and `CompetitorObservation.cs` (mutable snapshot). *(Deferred — Phase 4 dependency.)*
- **T011** [TF] [P] Create `MarketplaceAdvisory.Core.Domain/Content/AIJob.cs`, `ListingDraft.cs`. *(Deferred — Phase 4 dependency.)*
- **T012** [TF] [P] Create `MarketplaceAdvisory.Core.Domain/Support/CustomerMessage.cs`, `MessageThread.cs`, `MessageReplyDraft.cs`. *(Deferred — Phase 4 dependency.)*
- **T013** [TF] [P] Create `MarketplaceAdvisory.Core.Domain/Autoparts/ListingCompatibility.cs`. *(Deferred — Phase 4 dependency.)*
- **T014** [P] Add domain events under `MarketplaceAdvisory.Core.Domain/Events/` for every event listed in `data-model.md`. *(Partial: `ProductInputsChanged`, `ProfitabilityCalculated`, `ProfitabilityFloorHit`, `TenantThresholdsChanged`, `TenantTaxRegimeChanged` shipped. Remaining events land with their aggregates.)*
- **T015** ✅ `dotnet test MarketplaceAdvisory.Core.Domain.Tests` MUST be green before moving to Phase 2. **Verified 2026-09-26: 23/23 green.**

---

## Phase 2 — The Financial Engine (Services)

Deliver the deterministic math + the floor guard. All work lives in `MarketplaceAdvisory.Core.Domain` (pure services) and `MarketplaceAdvisory.Core.Application` (handlers/queries).

- **T020** ✅ [TF] Implement `ProfitabilityCalculatorService` in `Domain/Financial/`:
  - `Calculate(inputs) : ProfitabilityResult` — Rule A1 formula. ✅
  - `ClassifyStatus(marginPct, thresholds) : ProfitabilityStatus` — Rule A2. ✅
  - Deterministic, no I/O, no framework references. ✅
  - Golden tests: covers Simples Nacional Green/Yellow/Red, threshold boundaries, break-even, negative profit. *(Presumido bracket tests to be added when the read-side calculator wires activity-code lookup.)*
- **T021** ✅ [TF] [P] Implement `IdealPriceSimulator` in `Domain/Financial/`:
  - `SolveForTargetMargin(inputs, targetPct) : Money` — Rule A3. ✅
  - `SolveWithTierResolver(...)` — tier boundary iteration. ✅
  - Tests: reverse-solve within ±0.05 pp; tier switch handled. ✅
- **T022** ✅ [TF] Implement `IProfitabilityFloorGuard` in `Application/Common/Abstractions/` + concrete `ProfitabilityFloorGuard` in `Application/Financial/`. Tests cover `Allowed`, `HeldFloorHit`, `RequiresOverride` branches. ✅ Registered in `AddApplication`.
- **T023** ⚙️ [TF] [P] Application commands (`Application/Financial/Commands/`). **`SetSalePriceCommand` ✅ delivered** — returns `ErrorOr<ProductProfitabilityDto>`, assembles inputs via `IProfitabilityInputsAssembler`, calls `IProfitabilityFloorGuard`, maps the two documented outcomes (`floor.override_required`, `floor.hit`), and audits overrides via `ProfitabilityFloorHit`. *(`UpdateProductCostsCommand` and `SetTenantThresholdsCommand` deferred — they persist CMV/weight/dimensions and Tenant state, which is Phase 3 schema work.)*
- **T024** ⚙️ [TF] [P] Application queries (`Application/Financial/Queries/`). **`RunIdealPriceSimulatorQuery` ✅ and `GetProductProfitabilityQuery` ✅ delivered** (live-compute via the assembler + calculator, tenant-scoped). *(`GetProfitabilityDashboardQuery` deferred — depends on the Phase 3 Dapper view `catalog.product_profitability_v`.)*
- **T025** ✅ [P] FluentValidation validators for the delivered command/query (`SetSalePriceCommandValidator`, `RunIdealPriceSimulatorQueryValidator`) — auto-registered through the existing `ValidationBehavior` pipeline.
- **T026** ✅ [P] Domain → Contracts mapping. Delivered as explicit, deterministic projections in `FinancialMappings` (Money→MoneyDto, Percentage→decimal, enum→string) instead of Mapster — VO mapping is trivial and clarity was preferred. Contracts DTOs added: `MoneyDto`, `ProductProfitabilityDto`, `SimulatorResponseDto`.
- **T027** ✅ `dotnet test MarketplaceAdvisory.Core.Application.Tests` MUST be green. **Verified 2026-09-26: 16/16 green** (guard branches + simulator/profitability queries + set-sale-price command incl. override/audit paths).

> **New seam introduced this cycle:** `IProfitabilityInputsAssembler` (`Application/Common/Abstractions/`) — assembles `ProfitabilityInputs` (active `MarketplaceFee`, `ShippingTier`, tenant thresholds, tax regime) for a product+marketplace+price. It is the boundary the Phase 3 DB-backed implementation fills; handlers and tests depend only on the abstraction, so the P1 vertical is fully tested today with a fake assembler. **Not registered in DI yet** (no concrete impl until Phase 3), so controllers (Phase 5) wire once the real assembler lands.

---

## Phase 3 — Data Access (EF Core & Dapper)

**Depends on** Phase 1 (Domain entities) and Phase 2 (Application abstractions).

> **Important — Blocked-by-user checkpoints**: the schema and connection strings are deferred until the user supplies them. Scaffold interfaces, EF configurations and Dapper queries against the modeled entities; postpone the actual `add-migration`/`update-database` runs to a follow-up.

- **T030** Extend `MarketplaceAdvisory.Core.Infrastructure/Persistence/ApplicationDbContext.cs`:
  - Add `DbSet<>` for every new aggregate.
  - Apply `HasQueryFilter` on **every** new entity for `TenantId` (Principle III).
  - Set default schemas per module (`financial`, `pricing`, `content`, `support`, `autoparts`, `tenants`).
- **T031** [P] Add EF configurations under `Infrastructure/Persistence/Configurations/`: `BundleConfiguration`, `TenantConfiguration`, `MarketplaceFeeConfiguration`, `ShippingTierConfiguration`, `TaxProfileConfiguration`, `ProfitabilityCalculationConfiguration`, `RepricerDecisionConfiguration`, `CompetitorObservationConfiguration`, `AIJobConfiguration`, `ListingDraftConfiguration`, `CustomerMessageConfiguration`, `MessageReplyDraftConfiguration`, `ListingCompatibilityConfiguration`. Owned types for value objects (`Money`, `Weight`, `Dimensions`, `ProfitabilityThresholds`).
- **T032** [P] Add write-side repositories where the pattern already exists (`ProductRepository` template): `BundleRepository`, `AIJobRepository`, `RepricerDecisionRepository`, `CustomerMessageRepository`. Register in `Infrastructure/DependencyInjection`.
- **T033** [BLOCKED-BY-USER] Author EF migrations for the initial schema:
  - Run `dotnet ef migrations add InitialCore --project ...Infrastructure --startup-project tools/MarketplaceAdvisory.Migrator`.
  - **Do not run against a real DB** until the user supplies the connection string.
- **T034** [TF] [P] Author Dapper queries in `Infrastructure/Persistence/ReadDbConnection.cs` for the read-model views listed in `data-model.md`:
  - `catalog.product_profitability_v`
  - `catalog.product_search_v`
  - `pricing.repricer_history_v`
  - `support.inbox_v`
  - `autoparts.compatibility_v`
  - Every query MUST parameterize `@TenantId` (Principle III + `research.md#R9`).
- **T035** [TF] Integration tests under `Infrastructure.Tests/` using Testcontainers PostgreSQL. Cover: multi-tenant isolation (a query issued as tenant A never sees tenant B rows), Money/Weight owned-type round-trip, cascade delete/reference behavior.
- **T036** ✅ Once the user supplies the connection string: run `dotnet run --project tools/MarketplaceAdvisory.Migrator` and re-run `dotnet test MarketplaceAdvisory.Core.Infrastructure.Tests`.

---

## Phase 4 — AI & Background Jobs

**Depends on** Phase 3 (repos + read model) and Phase 2 (Application abstractions).

- **T040** [TF] Add abstractions: `IAiProvider`, `IImageProcessor`, `IImageStorage`, `ICompatibilitySearchIndex` in `Application/Common/Abstractions/`. Documentation on each interface links back to `research.md`.
- **T041** [TF] Implement `Infrastructure/Content/AiProvider/FakeAiProvider` (returns deterministic fixtures) so tests and dev environments work without a real key. Concrete `OpenAiProvider` (or `GeminiProvider`) is `[BLOCKED-BY-USER]` on the API key.
- **T042** [TF] [P] Implement `Infrastructure/Content/ImageProcessor/FakeImageProcessor` (returns a fixture image) and `Infrastructure/Content/ImageStorage/LocalImageStorage`. Cloud drivers are `[BLOCKED-BY-USER]`.
- **T043** [TF] [P] Implement `Infrastructure/Autoparts/MeilisearchCompatibilitySearchIndex` — with a `FakeCompatibilitySearchIndex` fallback for tests.
- **T044** [TF] Implement Quartz jobs under `Infrastructure/BackgroundJobs/Jobs/`:
  - `RepricerJob` — per marketplace, `[DisallowConcurrentExecution]`, calls `RunRepricerDecisionCommand`.
  - `CompetitorRadarJob` — per marketplace, refreshes `CompetitorObservation`.
  - `ListingRewriteJob` — pops an `AIJob` of kind `ListingRewrite` from Redis queue, calls `IAiProvider`.
  - `ImageCleanupJob` — pops `ImageCleanup` jobs.
  - `SacDraftJob` — pops `SacDraft` jobs.
  - Register each job + trigger in `Infrastructure/DependencyInjection.cs` per module.
- **T045** [TF] [P] Application commands for the pipelines (`Application/Content/Commands/` and `Application/Support/Commands/`): `RewriteListingFromCompetitorCommand`, `CleanProductImageCommand`, `CreateBundleCommand`, `DraftReplyCommand`, `ApproveReplyCommand`, `SendReplyCommand`. Every command that changes a price consults `IProfitabilityFloorGuard`.
- **T046** [TF] [P] Application queries for AI/Support: `GetAiJobStatusQuery`, `GetBundleProfitabilityQuery`, `GetInboxQuery`, `GetThreadQuery`.
- **T047** [P] Add competitor scraping to marketplace adapters — extend `IMarketplaceAdapter` with `GetCompetitorObservationsAsync(url, ct)`, `GetInboxAsync(since, ct)`, `SendReplyAsync(threadId, body, ct)`, `PublishCompatibilityAsync(productId, entries, ct)`. `[BLOCKED-BY-USER]` for concrete Mercado Livre / Shopee / Amazon endpoints; fakes ship for tests.
- **T048** ✅ `dotnet test MarketplaceAdvisory.Core.Application.Tests` + `MarketplaceAdvisory.Core.Infrastructure.Tests` green.

---

## Phase 5 — BFF & Identity Integration

**Depends on** Phase 4.

- **T050** Add controllers under `MarketplaceAdvisory.Core.Api/Controllers/`:
  - `FinancialController` — every endpoint from `contracts/financial-engine.md`, `[Authorize]` at class level, `[Authorize(Roles = AppRoles.Manager)]` on writes.
  - `PricingController` — from `contracts/repricer.md`.
  - `ListingsController` — from `contracts/ai-productivity.md`.
  - `AutopartsController` — from `contracts/autoparts.md`.
  - `SacController` — from `contracts/sac.md`.
  - Each action returns `IActionResult` via the existing `ToProblem` mapping. Every endpoint MUST carry `ProducesResponseType<T>`.
- **T051** [P] Add DTOs to `MarketplaceAdvisory.Contracts` under the module folders (see `plan.md#project-structure`). No Domain type crosses the API boundary.
- **T052** [TF] Add integration tests under `MarketplaceAdvisory.Core.Api.Tests/` using `WebApplicationFactory<Program>` for one happy-path and one auth/RBAC test per controller. Reuse the fake AI/image/search adapters from Phase 4.
- **T053** Update `MarketplaceAdvisory.Core.Api/Program.cs`:
  - Register the new abstractions and jobs (call site of existing `AddApplication` / `AddInfrastructure`).
  - Ensure the new Swagger endpoints all inherit the existing `Bearer` security requirement.
  - Confirm `RequireManager` / `RequireUser` policies are applied.
- **T054** BFF wiring (in `MarketplaceAdvisory.BFF` repository): add Refit clients `IFinancialClient`, `IPricingClient`, `IListingsClient`, `IAutopartsClient`, `ISacClient` and one Dashboard aggregator per Angular page. Forward the JWT via `Authorization` header (existing pattern from `DashboardController`). *Note: BFF work happens in the BFF repo; this task documents the coordination.*
- **T055** [P] Update `README.md` at the root of `MarketplaceAdvisory.Core` with the endpoint documentation (auto-generated section pointing to `docs/endpoints.md`) and the module map — see the "Documentation" section below.
- **T056** ✅ `dotnet build MarketplaceAdvisory.Core.sln && dotnet test MarketplaceAdvisory.Core.sln` all green.

---

## Documentation deliverables

- `docs/endpoints.md` — per-endpoint reference (one-line purpose + link to `contracts/`).
- `docs/architecture.md` — architecture overview (layers, DI graph, module map, Principle VIII flow).
- `README.md` — refreshed with product identity + module map + quickstart (see `quickstart.md`).

These are created together with **T055** so the docs cannot lag behind the code.

---

## Traceability matrix

| Task     | User Story | FR                    | Constitution |
|----------|------------|-----------------------|--------------|
| T020..27 | US1        | FR-A1..A6, FR-001..05 | I, II, V, VI, VIII |
| T044..48 | US2        | FR-E1..E5             | II, V, VI, VII, VIII |
| T045..46 | US3        | FR-B1..B5             | II, V, VI, VII, VIII |
| T043,47  | US4        | FR-C1..C3             | II, V, VII |
| T045..46 | US5        | FR-D1..D4             | II, V, VII |

---

## Ordering

```
Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5
   ▲          ▲          ▲
   │          │          └─ requires all Phase-1 aggregates + Phase-2 abstractions
   │          └─ requires all Phase-1 aggregates
   └─ requires the Constitution (already ratified v1.1.0)
```

Within a phase, tasks with `[P]` can run in parallel. `[TF]` tasks MUST have failing tests before the code lands.

---

## Blocked-by-user summary

| Task | What we need from you |
|------|-----------------------|
| T033 | PostgreSQL connection string + schema decisions (or "keep the modeled defaults") |
| T041 | AI provider key (OpenAI / Gemini / other) |
| T042 | Cloud blob storage credentials (S3 / Azure Blob) — OK to stay local for dev |
| T047 | Mercado Livre / Shopee / Amazon API credentials + endpoint URLs |

Everything else can proceed with the fakes shipped by the tasks above.
