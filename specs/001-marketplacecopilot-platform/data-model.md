# Data Model — MarketplaceCopilot Platform Foundation

**Date**: 2026-09-24
**Feature**: [spec.md](./spec.md) · **Plan**: [plan.md](./plan.md)

This document describes the **Domain entities, value objects and events** introduced by this feature. It is technology-agnostic on purpose — EF Core configurations and Dapper read models are Infrastructure concerns and live outside this file.

Every aggregate root inherits `AggregateRoot<TId>` (already in `Domain/Common`) and therefore carries `TenantId` (Principle III).

Legend:

- 🟢 **New aggregate** (this feature)
- 🟡 **Modified aggregate** (existing, extended)
- 🔵 **Value object**
- ⚡ **Domain event**

---

## Aggregates

### 🟡 Product (Catalog)

Existing. Extended with the fields required by the Financial Engine, Bundle Maker, Auto-parts and SAC.

**Fields**:

- `Id` (Guid)
- `TenantId` (from base)
- `Sku` (string, unique per tenant)
- `Title` (string)
- `Description` (string)
- `Category` (`ProductCategory` VO — carries marketplace-specific category id references)
- `Cmv` (`Money`) — cost of merchandise sold
- `Weight` (`Weight` VO, grams)
- `Dimensions` (`Dimensions` VO — width/height/length in centimeters)
- `Images` (list of `ProductImage` VO — reference + processed reference)
- `SalePrices` (list of `MarketplacePrice` VO — per marketplace)
- `CurrentProfitability` (per marketplace projection — cached `ProfitabilityStatus`)
- `CompatibilityIds` (list of `CompatibilityRef` VO — auto-parts, references marketplace catalog id)
- `RepricerConfig` (`RepricerSettings` VO, optional)

**Invariants**:

- `Sku` MUST be non-empty and unique per tenant.
- `Cmv.Amount` MUST be > 0.
- `Weight` and `Dimensions` MUST be > 0.
- Attaching a `CompatibilityRef` requires a non-empty marketplace catalog id (FR-C2).

**Factory / operations**:

- `Product.Create(TenantId, sku, title, category, cmv, weight, dimensions) : ErrorOr<Product>`
- `product.SetSalePrice(marketplace, price, floorDecision) : ErrorOr<Success>` — requires an `Allowed` decision from `IProfitabilityFloorGuard` (Principle VIII).
- `product.AttachCompatibility(compatibilityRef) : ErrorOr<Success>`
- `product.UpdateCmv(newCmv) : ErrorOr<Success>` — raises ⚡ `ProductInputsChanged`.

---

### 🟢 Bundle (Catalog)

A virtual SKU composed of two or more concrete `Product` SKUs of the same tenant (nested bundles out of scope).

**Fields**:

- `Id` (Guid), `TenantId`
- `Sku` (string, unique per tenant)
- `Items` (list of `BundleItem` VO — `ProductId` + `Quantity`)
- `ComputedWeight` (`Weight` VO — Σ)
- `ComputedDimensions` (`Dimensions` VO — Max/Max/Sum rule, see FR-B3)
- `SuggestedSalePrices` (per marketplace, from `IdealPriceSimulator`)

**Invariants**:

- MUST reference ≥ 2 distinct concrete Products of the same tenant.
- No Bundle inside another Bundle.
- Re-created (materialization) when any child SKU's cost/weight/dimensions change.

**Factory**:

- `Bundle.Create(TenantId, sku, items) : ErrorOr<Bundle>`
- `bundle.RecomputeFromChildren(children) : ErrorOr<Success>` — raises ⚡ `BundleInputsChanged`.

---

### 🟢 Tenant (Tenants)

Existing conceptually (via `TenantId`), promoted to an aggregate to carry tenant-configured thresholds.

**Fields**:

- `Id` (`TenantId`)
- `Name` (string)
- `TaxRegimeSelection` (`TaxRegimeSelection` VO — regime + effective window)
- `Thresholds` (`ProfitabilityThresholds` VO — green %, yellow %, minimum-margin floor %)
- `RefreshSlas` (`RefreshSla` VO — traffic-light refresh SLA, search SLA, SAC drafting SLA)

**Invariants**:

- `green ≥ yellow ≥ floor ≥ 0`.
- Regime effective windows MUST NOT overlap.

**Factory**:

- `Tenant.Register(...)` (only from admin/registration flow).
- `tenant.UpdateThresholds(...)` — raises ⚡ `TenantThresholdsChanged`.
- `tenant.ChangeTaxRegime(...)` — raises ⚡ `TenantTaxRegimeChanged`.

---

### 🟢 ProfitabilityCalculation (Financial) — immutable projection / audit

Immutable record of a full calculation run. Persisted so audits can reproduce a decision (SC-008).

**Fields** (all immutable):

- `Id`, `TenantId`, `ProductId`, `Marketplace`, `ComputedAt`
- `InputsSnapshot` (embedded object: cmv, commission%, fixed fee, subsidized shipping, tax rate, sale price, regime code, thresholds)
- `NetProfit` (`Money`)
- `NetMarginPercentage` (decimal)
- `Status` (`ProfitabilityStatus` enum)
- `MinimumPriceFloor` (`Money`) — the price at which margin equals the tenant's minimum
- `CausedBy` (`CalculationTrigger` enum — `SalePriceChange | CmvChange | FeeChange | ShippingChange | TaxChange | ThresholdChange | SimulatorRun`)

Never mutates. New inputs → new record.

---

### 🟢 MarketplaceFee (Financial)

Rate table for commission and fixed fees.

**Fields**:

- `Id`, `TenantId?` (null = platform default; non-null = tenant override)
- `Marketplace` (`MarketplaceType`)
- `CategoryRef` (`ProductCategory` VO / catalog id)
- `CommissionPercentage` (decimal)
- `FixedUnitFee` (`Money`)
- `EffectiveFrom`, `EffectiveUntil?` (window)

**Invariants**:

- `0 ≤ commission% ≤ 100`
- Windows per (Tenant, Marketplace, Category) MUST NOT overlap.

---

### 🟢 ShippingTier (Financial)

Subsidized shipping cost per marketplace and volumetric-weight tier.

**Fields**:

- `Id`, `Marketplace`, `MinVolumetricWeightGrams`, `MaxVolumetricWeightGrams`
- `SubsidizedCost` (`Money`)
- `EffectiveFrom`, `EffectiveUntil?`

**Invariants**:

- Tiers MUST cover the domain of volumetric weights without gaps or overlaps within an effective window.

---

### 🟢 TaxProfile (Financial)

Selection + parameters per regime.

**Fields**:

- `Id`, `TenantId`
- `Regime` (enum: `SimplesNacional | LucroPresumido`)
- `Parameters` (VO — regime-specific: flat rate for Simples; activity code + brackets for Presumido)
- `EffectiveFrom`, `EffectiveUntil?`

---

### 🟢 RepricerDecision (Pricing) — immutable

Audit record for every automated repricing evaluation (FR-E4).

**Fields**:

- `Id`, `TenantId`, `ProductId`, `Marketplace`, `EvaluatedAt`
- `CompetitorPriceObserved` (`Money`)
- `MinimumPriceFloorApplied` (`Money`)
- `Action` (`RepricerAction` enum: `Lowered | Held | NoChange`)
- `NewPrice` (`Money?`)
- `TriggeredBy` (system actor id)

---

### 🟢 CompetitorObservation (Pricing) — mutable snapshot

Latest observed data for a monitored competitor listing per SKU.

**Fields**:

- `Id`, `TenantId`, `ProductId`, `Marketplace`, `CompetitorUrl`, `LastSeenAt`
- `Price` (`Money`), `Available` (bool), `BuyBoxPositionHint` (enum: `Winning | Losing | Unknown`)

---

### 🟢 AIJob (Content) — immutable audit + status

Every AI call is materialized as a job (FR-B4).

**Fields**:

- `Id`, `TenantId`, `Kind` (enum: `ListingRewrite | ImageCleanup | SacDraft`), `Status` (`Pending | Running | Succeeded | Failed`)
- `PromptPayload`, `ResponsePayload` (opaque JSON — provider-agnostic)
- `Provider` (string label), `Model` (string), `CostUsd` (decimal, nullable)
- `LinkedRef` (`AiJobLink` VO — either `ProductId + Marketplace` or `MessageId`)
- `CreatedAt`, `CompletedAt?`, `Error?`

---

### 🟢 ListingDraft (Content) — output of AI rewrite

**Fields**:

- `Id`, `TenantId`, `SourceUrl`, `RewrittenTitle`, `RewrittenDescription`
- `Status` (`Pending | Approved | Rejected`), `LinkedAiJobId`, `LinkedProductId?`

**Invariants**:

- `RewrittenTitle` MUST be ≤ 60 characters (FR-B1 acceptance).
- MUST NOT contain competitor brand names — verified by the sanitizer step in the pipeline.

---

### 🟢 CustomerMessage (Support)

Single row of the unified inbox.

**Fields**:

- `Id`, `TenantId`, `ThreadId`, `Marketplace`
- `Direction` (`Inbound | Outbound`), `Body` (string), `ArrivedAt` / `SentAt`
- `LinkedProductId?`, `LinkedOrderId?`
- `DraftReplyId?` (link to a `MessageReplyDraft`)
- `Status` (`Unread | Read | Answered`)

---

### 🟢 MessageReplyDraft (Support)

AI-drafted response awaiting operator approval.

**Fields**:

- `Id`, `TenantId`, `MessageId`, `Body`, `AiJobId`, `Status` (`Draft | Approved | Rejected`)
- `ApprovedBy?`, `ApprovedAt?`

---

### 🟢 ListingCompatibility (Autoparts)

Association between a `Product` and a marketplace's catalog compatibility id.

**Fields**:

- `Id`, `TenantId`, `ProductId`, `Marketplace`
- `CatalogRef` (`CompatibilityRef` VO — required non-empty catalog id, human-readable label optional)

**Invariants**:

- `CatalogRef.MarketplaceCatalogId` MUST be non-empty (FR-C2).

---

## Value Objects (🔵)

- `Money(Amount, Currency)` — existing.
- `TenantId(Guid)` — existing.
- `Weight(Grams)` — new.
- `Dimensions(WidthCm, HeightCm, LengthCm)` — new; helper `VolumetricWeight(divisor)`.
- `ProductCategory(Marketplace, CategoryId, Label?)` — new.
- `MarketplacePrice(Marketplace, Price)` — new.
- `ProfitabilityThresholds(GreenPct, YellowPct, MinimumFloorPct)` — new; invariant `Green ≥ Yellow ≥ Floor ≥ 0`.
- `RefreshSla(TrafficLightSla, SearchSla, SacDraftSla)` — new.
- `CompatibilityRef(Marketplace, CatalogId, Label?)` — new.
- `RepricerSettings(Enabled, UnderCutDelta, MinCadenceMinutes)` — new.
- `AiJobLink(ProductId?, MessageId?)` — new.
- `TaxRegimeSelection(Regime, EffectiveFrom, EffectiveUntil?)` — new.
- `HumanOverrideToken(ActorId, ReasonCode, AcceptedLoss)` — new; passed to `IProfitabilityFloorGuard` on override.
- `FloorDecision.Allowed(price) | .HeldFloorHit(floor) | .RequiresOverride(diff)` — new discriminated union.

---

## Domain Events (⚡)

- `ProductInputsChanged(ProductId, ChangedFields)` — triggers recomputation of Profitability.
- `BundleInputsChanged(BundleId)` — triggers recomputation of Bundle profitability.
- `ProfitabilityCalculated(ProductId, Marketplace, Status)` — updates dashboard read model.
- `ProfitabilityFloorHit(TenantId, ProductId, Marketplace, Actor, ObservedPrice, Floor)` — Principle VIII notification.
- `TenantThresholdsChanged(TenantId)` — triggers recomputation of all Profitability projections.
- `TenantTaxRegimeChanged(TenantId, NewRegimeCode, EffectiveFrom)` — same.
- `CompetitorPriceObserved(ProductId, Marketplace, Price)` — repricer input.
- `RepricerDecided(ProductId, Marketplace, Action, NewPrice?)` — audit stream.
- `ListingDraftReady(ListingDraftId)` — front-end poll.
- `ImageCleaned(ProductId, ImageRef)` — front-end poll.
- `SacDraftReady(MessageId)` — front-end poll.

---

## Read Models (Dapper projections)

- `catalog.product_profitability_v` — SKU + marketplace + status + margin + floor (for traffic-light dashboard).
- `catalog.product_search_v` — SKU + tenant + title + status (basic listing view).
- `pricing.repricer_history_v` — decisions per SKU per marketplace ordered by time.
- `support.inbox_v` — one row per thread, unread count, last message snippet.
- `autoparts.compatibility_v` — projection of `ListingCompatibility` for admin views (search itself is served by the Meilisearch index, not by this view).

Each view MUST include `tenant_id` and MUST be queried with `@TenantId` in the WHERE clause (Principle III, see R9 in `research.md`).

---

## Relationships (summary)

- `Tenant 1 — * Product`
- `Product 1 — * MarketplacePrice` (per marketplace)
- `Product 1 — * ListingCompatibility`
- `Bundle * — * Product` via `BundleItem`
- `Product 1 — * ProfitabilityCalculation` (audit trail)
- `Product 1 — * RepricerDecision`
- `Product 1 — * CompetitorObservation`
- `MessageThread 1 — * CustomerMessage`
- `CustomerMessage 1 — 0..1 MessageReplyDraft`
- `AIJob 1 — 0..1 (ListingDraft | ProcessedImage | MessageReplyDraft)`
