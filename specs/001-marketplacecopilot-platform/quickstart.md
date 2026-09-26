# Quickstart — MarketplaceCopilot Platform Foundation

**Date**: 2026-09-24
**Feature**: [spec.md](./spec.md) · **Plan**: [plan.md](./plan.md)

This guide describes how to validate the feature end-to-end after implementation. It intentionally references contracts and the data model instead of duplicating them.

## Prerequisites

- .NET 10 SDK.
- Docker Desktop (for PostgreSQL + Redis + Meilisearch + Seq via `docker compose`).
- The user has supplied at least a **PostgreSQL connection string** and a **Meilisearch endpoint** in `appsettings.Development.json` / User Secrets. Marketplace credentials may still be stubbed with the fake adapter.
- A JWT issued by `MarketplaceAdvisory.Security` for a test tenant, with role `Manager`.

## One-time setup

```powershell
# From the repository root
docker compose up -d                          # PostgreSQL + Redis + Meilisearch + Seq
dotnet build MarketplaceAdvisory.Core.sln
dotnet test  MarketplaceAdvisory.Core.sln     # green baseline
dotnet run --project tools/MarketplaceAdvisory.Migrator   # apply EF migrations
dotnet run --project MarketplaceAdvisory.Core/MarketplaceAdvisory.Core.Api
```

The API exposes Swagger at `https://localhost:<port>/swagger`. Use the **Bearer** button to paste your JWT.

## Test-First checkpoints (Constitution VI)

Each user story below MUST have its tests written and failing before the implementation lands. The order is the same as the priority list in the spec.

### P1 — Financial Engine ("Guardião da Lucratividade")

**Unit tests to add first**:

- `ProfitabilityCalculatorService_Should_ComputeNetProfit_For_SimplesNacional`
- `ProfitabilityCalculatorService_Should_ComputeNetProfit_For_LucroPresumido`
- `ProfitabilityCalculatorService_Should_ReturnYellow_When_MarginBetweenThresholds`
- `ProfitabilityCalculatorService_Should_ReturnRed_When_NegativeProfit`
- `IdealPriceSimulator_Should_ReverseSolve_For_TargetMargin_Within_HalfDecimalPoint`
- `IdealPriceSimulator_Should_HonorShippingTierBoundary_When_TierChangesAtSuggestedPrice`
- `ProfitabilityFloorGuard_Should_AllowWrite_When_PriceAboveFloor`
- `ProfitabilityFloorGuard_Should_HoldWrite_When_PriceBelowFloor`
- `ProfitabilityFloorGuard_Should_RequireOverride_When_HumanTokenProvided`

**End-to-end validation (Swagger / curl)**:

1. `POST /api/v1/financial/products/{sku}/costs` with CMV, category, weight, dimensions → expects `200 OK` and a `ProductProfitabilityDto` with the correct `NetProfit`, `NetMargin`, `TrafficLight`.
2. `POST /api/v1/financial/simulator` with `{ productId, targetNetMarginPercent: 15 }` → expects `SimulatorResponseDto` with `SuggestedSalePrice` such that recomputing yields 15% ± 0.05.
3. `GET /api/v1/financial/dashboard?status=Red` returns the list of SKUs currently red for the tenant.

### P2 — Dynamic Repricer & Competitor Radar

**Unit / integration tests**:

- `RunRepricerDecisionCommandHandler_Should_Lower_When_CompetitorAboveFloor`
- `RunRepricerDecisionCommandHandler_Should_Hold_When_CompetitorBelowFloor`
- `RunRepricerDecisionCommandHandler_Should_EmitProfitabilityFloorHit_When_Held`
- `CompetitorRadarJob_Should_Skip_Marketplace_When_AdapterUnavailable`
- Integration: run the Quartz job in-memory, feed a fake marketplace observation, assert the persisted `RepricerDecision`.

**End-to-end validation**:

1. `POST /api/v1/pricing/repricer/{productId}` with `{ marketplace: "MercadoLivre", enabled: true, underCutDelta: 0.01 }`.
2. Simulate a competitor observation (either via an admin seed endpoint or by inserting via the fake adapter).
3. `GET /api/v1/pricing/repricer/{productId}/history` should show a `Lowered` or `Held` entry with the correct floor.

### P3 — AI Listing Copier, Image Pipeline, Kit Maker

**Tests**:

- `RewriteListingFromCompetitorCommandHandler_Should_MaterializeAIJob_Then_EnqueueRewriteJob`
- `ListingRewriteJob_Should_RemoveCompetitorBrandNames_From_Description`
- `CleanProductImageCommandHandler_Should_EnqueueImageCleanupJob_And_ReturnJobId`
- `CreateBundleCommandHandler_Should_ComputeWeightAndDimensions_Per_FR_B3`
- `CreateBundleCommandHandler_Should_ReturnSuggestedPrice_From_IdealPriceSimulator`

**End-to-end validation**:

1. `POST /api/v1/listings/ai/rewrite` with `{ competitorUrl }` → returns `202 Accepted` with an `AIJobId`.
2. Poll `GET /api/v1/listings/ai/jobs/{id}` until `Succeeded`; assert the produced `ListingDraft` has ≤ 60-char title and no forbidden brand words.
3. `POST /api/v1/listings/bundles` with `{ items: [{sku:X}, {sku:Y}] }` → returns `BundleDto` with computed weight/dimensions and a suggested price per marketplace.
4. `POST /api/v1/listings/images/clean` with the image ref → returns `AIJobId`; on completion the processed image has the `#FFFFFF` background.

### P4 — Auto-parts Compatibility Search

**Tests**:

- `MeilisearchCompatibilitySearchIndex_Should_ReturnResults_Within_Sla_For_KnownPartCode` (integration, container).
- `AttachCompatibilityToProductCommandHandler_Should_Reject_When_CatalogIdIsEmpty`
- `AttachCompatibilityToProductCommandHandler_Should_Succeed_And_EmitEvent`

**End-to-end validation**:

1. Seed the Meilisearch index with a small compatibility dataset (fake CSV in test data).
2. `GET /api/v1/autoparts/search?partCode=BOSCH0986AN0510` → expects a `CompatibilityResultDto` with vehicles/years/engines and a marketplace catalog id per entry.
3. `POST /api/v1/autoparts/products/{productId}/compatibility` attaches the compatibility list.
4. Publish the listing via the marketplace adapter and verify the outbound payload carries the exact catalog ids used.

### P5 — Smart SAC (Inbox + AI Reply)

**Tests**:

- `DraftReplyCommandHandler_Should_InjectCompatibilityList_When_ProductHasAutoparts`
- `DraftReplyCommandHandler_Should_MaterializeAIJob_And_Return_DraftId`
- `ApproveReplyCommandHandler_Should_SendViaAdapter_And_MarkAnswered`
- `GetInboxQueryHandler_Should_ScopeByTenant_And_HonorPagination`

**End-to-end validation**:

1. Insert (via fake adapter or seed) an inbound question about a SKU with a compatibility list.
2. `GET /api/v1/sac/inbox` returns the message with a `DraftReplyDto` populated.
3. `POST /api/v1/sac/threads/{threadId}/reply/approve` sends the reply through the adapter and marks the thread `Answered`.

## Success signals (traceable to Success Criteria)

- **SC-001** — SDK/browser onboarding walkthrough completes in under 2 minutes with a fresh tenant.
- **SC-002** — Zero `RepricerDecision` rows with a `NewPrice` below the stored floor over a 30-day audit window (SQL: `SELECT COUNT(*) FROM pricing.repricer_history_v WHERE action = 'Lowered' AND new_price < minimum_price_floor_applied`).
- **SC-003** — 95th percentile of `/api/v1/autoparts/search` requests within the tenant SLA over a rolling 24h window (Serilog + OTel counter).
- **SC-004** — Manual sample audit of 100 AI-generated `ListingDraft` rows shows zero competitor brand names.
- **SC-005** — Bundle creation to `SuggestedSalePrice` returned within 30 seconds for a two-SKU bundle (per Otel span `Bundle.Create`).
- **SC-006** — Ratio of `Approved / Draft` on `MessageReplyDraft` ≥ 60% per tenant over 90 days.
- **SC-007** — Chaos test: kill one marketplace adapter → other adapters keep serving reads/writes.
- **SC-008** — Every `RepricerDecision` and `SetSalePrice` action has a linked `ProfitabilityCalculation` (verified via join test).

## Data seeding notes

- Test data lives under `MarketplaceAdvisory.Core.Infrastructure.Tests/Data/` as CSV files loaded by `Testcontainers` fixtures.
- Never commit real seller data.
- Marketplace fee/shipping/tier tables MUST be seeded to run any Financial Engine test end-to-end.
