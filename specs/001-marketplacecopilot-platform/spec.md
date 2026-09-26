# Feature Specification: MarketplaceCopilot Platform Foundation

**Feature Branch**: `001-marketplacecopilot-platform`

**Created**: 2026-09-24

**Status**: Draft

**Input**: User description: "MarketplaceCopilot — a B2B SaaS platform for high-volume Brazilian marketplace sellers (Mercado Livre, Shopee, Amazon) that eliminates financial blindness by calculating real-time profitability, automating listing creation with AI, mapping auto-parts compatibility, powering a smart customer-service inbox and running a profit-safe dynamic repricer."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Financial Engine ("Guardião da Lucratividade") (Priority: P1)

As a professional seller managing hundreds of SKUs across marketplaces, I want to see, for every product, the exact net profit I will earn on each sale AND be blocked from ever pricing a SKU below the point where I would lose money, so that I can trust automation instead of manually checking spreadsheets.

**Why this priority**: This is the product's core promise. Without it, none of the other modules deliver value — an AI-generated listing at a loss, an auto-parts sale at a loss, or a repricer that steals margin are all worse than doing nothing. This module is the MVP: the platform is shippable if only P1 exists.

**Independent Test**: Can be validated by (1) registering a product with cost, category, weight, tax regime and sale price; (2) confirming the platform returns the correct Net Profit, Net Margin %, and traffic-light status (Green / Yellow / Red); (3) using the Ideal-Price Simulator with a target margin and confirming the returned sale price, when fed back into the calculator, produces exactly the requested margin.

**Acceptance Scenarios**:

1. **Given** a product with CMV=R$50, marketplace commission=12%, fixed unit fee=R$5, subsidized shipping=R$18, tax rate=6% (Simples Nacional) and sale price=R$100, **When** the seller opens the product view, **Then** the platform displays Net Profit=R$9.00, Net Margin=9.0%, and the Yellow traffic-light status (assuming default Green threshold is 12%).
2. **Given** the same product, **When** the seller lowers the sale price to R$70, **Then** the platform displays a negative Net Profit and the Red traffic-light status, and the save action is allowed only with an explicit "sell at loss" confirmation.
3. **Given** the same product, **When** the seller opens the Ideal-Price Simulator and requests a target Net Margin of 15%, **Then** the platform returns a Suggested Sale Price such that recomputing Net Margin with that price yields 15.0% ± 0.05 percentage points, honoring the current shipping tier.
4. **Given** any cost input change (CMV, fee, shipping tier, tax regime), **When** the change is saved, **Then** the traffic-light status is recomputed and the dashboard reflects the new status within the tenant-configured refresh SLA.
5. **Given** a tenant with minimum-margin floor set to 5%, **When** any automated flow attempts to set a sale price that would drop margin below 5%, **Then** the write is rejected, a `ProfitabilityFloorHit` event is raised, and the seller is notified.

---

### User Story 2 — Dynamic Repricer & Competitor Radar (Priority: P2)

As a seller competing for the Buy Box, I want the platform to automatically monitor competitor prices and lower my price when it's safe, so that I win more sales without babysitting listings — and I want the platform to REFUSE to lower my price when it would cost me money.

**Why this priority**: The repricer is the module that most directly increases revenue AND is the highest-risk feature — a badly built repricer sells at a loss silently. It is priority P2 because it depends entirely on the Financial Engine (P1) being correct.

**Independent Test**: Can be validated by (1) enabling repricing for a product with a known floor; (2) simulating a competitor price above the floor and confirming the platform lowers to match; (3) simulating a competitor price below the floor and confirming the platform HOLDS the price, alerts the seller, and does not persist the loss-making price.

**Acceptance Scenarios**:

1. **Given** a product with a computed floor of R$95 and current price R$110, **When** a competitor lists at R$100, **Then** the repricer lowers the price to R$100 (or the configured under-cut delta) and records the decision with the floor value at that moment.
2. **Given** the same product with floor R$95 and current price R$100, **When** a competitor lists at R$90, **Then** the repricer HOLDS at R$95 (or the previous price if higher), raises a `ProfitabilityFloorHit` event, notifies the seller and does NOT persist R$90.
3. **Given** a competitor URL is registered on the "Competitor Radar" for a SKU, **When** the seller opens the SKU dashboard, **Then** the current competitor price, position (winning / losing Buy Box) and repricer decision history are visible.
4. **Given** the repricer runs on a schedule, **When** any single marketplace adapter is unavailable, **Then** other marketplaces continue to be repriced normally.

---

### User Story 3 — AI Listing Copier, Image Pipeline & Kit Maker (Priority: P3)

As a seller launching dozens of new listings per week, I want to paste a competitor URL and receive a legally-safe rewritten title and description, clean product images with a white background, and the ability to bundle multiple SKUs into a Kit whose weight, dimensions, shipping cost and price are calculated automatically.

**Why this priority**: Productivity module — huge time-saver but not required for the platform to protect margin (P1) or optimize price (P2). Ships as an incremental enhancement.

**Independent Test**: Can be validated by (1) pasting a competitor URL and confirming the AI returns a title/description with all competitor brand names and trademarks removed; (2) uploading a product image and receiving back the same image with the background replaced by pure `#FFFFFF`; (3) combining Product X (300g, 20×15×10) and Product Y (200g, 12×8×5) into a Bundle and confirming Bundle Weight = 500g and Bundle Dimensions = 20 + 15 + (10+5) = 50 (using the documented Max/Max/Sum rule).

**Acceptance Scenarios**:

1. **Given** a competitor listing URL, **When** the seller submits it, **Then** a background job scrapes the page, calls the Generative AI provider and returns a rewritten SEO-optimized title (max 60 chars) plus a sanitized description with zero competitor brand references, and the AI request/response pair is stored for audit.
2. **Given** a raw product image, **When** the seller submits it to the image pipeline, **Then** an asynchronous job processes it and returns an image with a pure `#FFFFFF` background, and the original image is preserved.
3. **Given** two active SKUs X and Y in the same tenant, **When** the seller creates a Bundle SKU containing X and Y, **Then** the platform creates a new virtual SKU with `Weight = W(X) + W(Y)`, `Dimensions = Max(side1) + Max(side2) + Sum(side3)`, and a Suggested Sale Price generated via the Ideal-Price Simulator (Rule A3 from the Financial Engine).
4. **Given** a Bundle exists, **When** the underlying SKU cost or fee changes, **Then** the Bundle's profitability is recomputed automatically.

---

### User Story 4 — Auto-parts Compatibility Search (Priority: P4)

As an auto-parts seller, I want to search by manufacturer part code (e.g., "BOSCH123") and instantly see the list of compatible vehicles/years/engines that map to the marketplace's official catalog, so that I can attach the correct compatibility list to each listing and cut returns.

**Why this priority**: This is a strong vertical differentiator but only relevant to a segment of sellers. Shippable after the core engine and productivity tools.

**Independent Test**: Can be validated by (1) searching a known part code and receiving a mapped list of vehicles/years/engines under the tenant-configured refresh SLA; (2) verifying every entry references a marketplace catalog compatibility ID; (3) attaching the returned list to a product and confirming it flows to the listing published via the marketplace adapter.

**Acceptance Scenarios**:

1. **Given** a search index populated with the catalog, **When** the seller searches "BOSCH0986AN0510", **Then** the top result returns within the tenant-configured search SLA and includes a compatibility list of vehicles, years and engines.
2. **Given** a compatibility entry, **When** the seller inspects it, **Then** it carries the marketplace's official compatibility catalog ID (not a free-text label).
3. **Given** the seller applies a compatibility list to a listing, **When** the listing is published via the marketplace adapter, **Then** the exact same catalog IDs are used in the outbound payload.

---

### User Story 5 — Smart SAC (Customer Service Inbox with AI Reply) (Priority: P5)

As a seller with 50+ questions per day, I want a single inbox that mixes pre-sale questions and post-sale messages across marketplaces, with an AI-drafted reply that already consulted my product's compatibility list, so that my operators approve answers in one click instead of typing from scratch.

**Why this priority**: Retention/NPS feature. Depends on Auto-parts (P4) to fully deliver context-aware answers for the vertical, so it lands last.

**Independent Test**: Can be validated by (1) receiving a marketplace pre-sale question about vehicle fit; (2) confirming the platform enriches the AI prompt with that product's `CompatibilityList`; (3) confirming the operator sees a proposed reply and can approve/edit/reject with a single action.

**Acceptance Scenarios**:

1. **Given** an incoming pre-sale question mentioning a vehicle model, **When** the message arrives, **Then** the platform fetches the product's compatibility list, calls the AI with the enriched prompt, and produces a draft reply within the tenant-configured drafting SLA.
2. **Given** a proposed reply, **When** the operator clicks "Approve", **Then** the reply is sent through the correct marketplace adapter and marked as answered.
3. **Given** any message thread, **When** the operator opens it, **Then** the full conversation history (pre-sale + post-sale) and linked SKU/order are visible.

---

### Edge Cases

- **Shipping tier changes at the boundary**: what happens when a bundle's volumetric weight crosses a shipping-fee tier? The Financial Engine MUST recompute using the destination tier and MUST NOT cache a stale tier.
- **Marketplace commission override**: some categories carry promotional commissions. The Financial Engine MUST honor a per-tenant, per-category effective commission (with an effective-date window).
- **AI provider outage**: if the AI provider is unavailable, the listing/SAC AI jobs MUST retry with exponential backoff and MUST NOT block the human workflow — the operator can always send a manual reply / manual listing.
- **Marketplace adapter outage**: a single marketplace outage MUST NOT block reads or writes on the others (Principle VII).
- **Sale-at-loss confirmation loophole**: humans can override the floor, but the override MUST always be logged with actor, reason and the exact accepted loss.
- **Tax regime change mid-cycle**: when a tenant switches from Simples Nacional to Lucro Presumido (or vice-versa), all subsequent calculations MUST use the new regime, but historical decisions MUST remain reproducible against the regime that was active at the decision time.
- **Bundle inside a bundle**: nested bundles are OUT OF SCOPE for v1 — a bundle MUST reference concrete SKUs only.
- **Multi-currency**: v1 is BRL-only.

## Requirements *(mandatory)*

### Functional Requirements

**Cross-cutting (all modules)**

- **FR-001**: The platform MUST scope every piece of data by `TenantId`. Cross-tenant reads or writes are forbidden.
- **FR-002**: The platform MUST authenticate every request via the JWT issued by the Security service and MUST enforce role-based authorization (`User`, `Manager`).
- **FR-003**: The platform MUST expose its capabilities via a versioned HTTP API (`/api/v{n}/...`) documented via OpenAPI/Swagger, and every response MUST use RFC 7807 `ProblemDetails` on error.
- **FR-004**: Every domain write MUST be reversible via audit — the platform MUST log actor, tenant, timestamp, before-state and after-state for every state transition of the entities below.
- **FR-005**: The platform MUST NOT commit any external credential (marketplace API key, AI API key, DB connection string) to source; secrets are supplied via configuration provider at runtime.

**Module A — Financial Engine**

- **FR-A1**: The platform MUST calculate Net Profit as: `Sale Price − CMV − (Sale Price × Commission%) − Fixed Unit Fee − Subsidized Shipping − (Sale Price × Tax Rate%)`, where each input is per-tenant, per-SKU, per-marketplace when applicable.
- **FR-A2**: The platform MUST return, together with Net Profit, the Net Margin percentage and a `ProfitabilityStatus` computed against per-tenant Green/Yellow thresholds (default: Green ≥ 12%, Yellow 1–11%, Red ≤ 0%).
- **FR-A3**: The platform MUST expose an Ideal-Price Simulator that, given a target Net Margin %, returns a Suggested Sale Price such that the calculator, when fed with the returned price, produces the requested margin within ±0.05 percentage points, honoring the shipping tier applicable at the returned price.
- **FR-A4**: The platform MUST recompute Net Profit / margin / status whenever any input changes (CMV, commission, fixed fee, shipping tier, tax regime, sale price) and MUST publish a domain event for downstream read models.
- **FR-A5**: The platform MUST support the following tax regimes as first-class inputs: Simples Nacional (flat rate), Lucro Presumido (rate schedule). Additional regimes MUST be pluggable without changing calling code.
- **FR-A6**: The platform MUST expose the minimum-price floor (the price at which margin equals the tenant-configured minimum) as a read API used by all automated writers (Principle VIII).

**Module B — AI & Productivity**

- **FR-B1**: The platform MUST accept a competitor listing URL, scrape the page in a background job, and produce a rewritten title (≤ 60 characters, SEO-oriented) and a sanitized description with all competitor brand names and trademarks removed.
- **FR-B2**: The platform MUST accept raw product images and, via an asynchronous queue, produce cleaned images with a pure `#FFFFFF` background. Original images MUST be preserved.
- **FR-B3**: The platform MUST support Bundle creation from two or more concrete SKUs owned by the same tenant, calculating `Bundle Weight = Σ Weight`, `Bundle Dimensions = Max(side1) + Max(side2) + Σ(side3)`, and a Suggested Sale Price via FR-A3.
- **FR-B4**: Every AI provider call MUST be recorded (prompt, model, response, cost) for auditability and cost control.
- **FR-B5**: AI jobs MUST retry with exponential backoff on provider failure and MUST fail closed — no partial write to a listing.

**Module C — Auto-parts**

- **FR-C1**: The platform MUST expose a search endpoint that, given a manufacturer part code, returns a compatibility list of vehicles/years/engines within the tenant-configured search SLA.
- **FR-C2**: Each compatibility entry MUST reference the marketplace's official compatibility catalog ID (not a free-text label). Attaching a free-text-only entry to a listing is forbidden.
- **FR-C3**: The compatibility list attached to a listing MUST be transmitted via the marketplace adapter using the catalog IDs of that marketplace.

**Module D — Smart SAC**

- **FR-D1**: The platform MUST unify pre-sale questions and post-sale messages across all connected marketplaces in one inbox scoped by `TenantId`.
- **FR-D2**: For every incoming message, the platform MUST identify the linked SKU/order (if any), fetch its `CompatibilityList` and other relevant context, inject it into an AI prompt and produce a draft reply within the tenant-configured drafting SLA.
- **FR-D3**: Every message MUST be routable via a "one-click Approve" that sends the reply through the correct marketplace adapter.
- **FR-D4**: All AI drafts, human edits and final sends MUST be auditable per message.

**Module E — Dynamic Repricer**

- **FR-E1**: The platform MUST monitor competitor prices for opted-in SKUs on a schedule and MUST compare them against the SKU's current price.
- **FR-E2**: The platform MAY lower the seller's price to match or under-cut competitors ONLY when the resulting price is greater than or equal to the SKU's minimum-price floor (FR-A6).
- **FR-E3**: When lowering the price would break the floor, the platform MUST HOLD the current price, emit `ProfitabilityFloorHit`, notify the seller, and record the attempted competitor price for reporting.
- **FR-E4**: Every automated repricing decision MUST record: actor (system), timestamp, competitor price observed, computed floor, action taken, new price (if any).
- **FR-E5**: The repricer MUST run independently per marketplace — failure of one marketplace adapter MUST NOT block others.

### Key Entities

- **Tenant**: the seller organization owning all data. Attributes: id, name, tax regime, default Green/Yellow thresholds, minimum-margin floor.
- **Product (SKU)**: the sellable item. Attributes: id, tenant, SKU code, title, description, weight, dimensions, CMV, images, current sale price, current `ProfitabilityStatus`, category, compatibility list (auto-parts).
- **MarketplaceFee**: commission and fixed-fee table per marketplace, category and effective-date window (per tenant when a promotional override applies).
- **ShippingTier**: mapping from volumetric weight and destination to subsidized shipping cost (per marketplace).
- **TaxProfile**: rules and rates for Simples Nacional / Lucro Presumido used by the Financial Engine.
- **ProfitabilityCalculation**: an immutable computation result — inputs snapshot, computed net profit, net margin, `ProfitabilityStatus`, minimum-price floor, timestamp.
- **RepricerDecision**: audit record for every repricer run — SKU, competitor price observed, computed floor, action (`Lowered`, `Held`, `NoChange`), new price (nullable), timestamp.
- **Bundle**: a virtual SKU composed of two or more concrete SKUs; computed weight/dimensions/floor/suggested price.
- **AIJob**: request/response record for every AI provider call (listing rewrite, image cleanup, SAC draft) — prompt, model, cost, status.
- **ListingCompatibility**: link between a Product and one or more marketplace-catalog compatibility IDs (auto-parts).
- **CustomerMessage**: unified inbox item — direction (in/out), marketplace, thread id, linked SKU/order, body, status, AI draft (if any).
- **ProfitabilityStatus** (enum): `Green`, `Yellow`, `Red`.
- **CompetitorObservation**: snapshot of a competitor listing (price, availability, buy-box position) tied to a monitored SKU.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A seller can go from "empty product" to seeing a correct traffic-light Net Margin in under 2 minutes on their first login (no engineer help).
- **SC-002**: 100% of automated repricing decisions comply with the minimum-price floor (0 loss-making writes over any 30-day audit window).
- **SC-003**: Auto-parts search returns compatibility results within the tenant-configured search SLA on at least 95% of queries.
- **SC-004**: AI-generated listings launched through the platform contain zero competitor brand names and trademarks in a manual audit of a 100-listing sample.
- **SC-005**: Bundle creation to first suggested price takes under 30 seconds end-to-end for a two-SKU bundle.
- **SC-006**: Operators approve at least 60% of SAC AI drafts with a single click (no edit) within the first 90 days of use per tenant.
- **SC-007**: When a marketplace adapter is unavailable, the other marketplaces continue to operate — no cross-marketplace failure is observed during an incident simulation.
- **SC-008**: Every automated price-changing operation carries a stored `ProfitabilityCalculation` snapshot that a support engineer can retrieve without touching production data.

## Assumptions

- The platform is v1 BRL-only; multi-currency is out of scope.
- The seller's operators use the Angular front-end via the BFF; direct Core API consumers must obtain a JWT from the Security service.
- The exact **database engine** (PostgreSQL) and **marketplace API credentials** (Mercado Livre, Shopee, Amazon) are deferred to Infrastructure implementation — the user will supply them later. Domain and Application layers MUST be designed against `IApplicationDbContext`, `IReadDbConnection`, `IMarketplaceAdapter` abstractions.
- The AI provider is treated as a swappable dependency (OpenAI, Gemini, etc.); interfaces stay stable regardless of provider.
- The search engine (Meilisearch or Elasticsearch) for Auto-parts is a swappable dependency behind an `ICompatibilitySearchIndex` abstraction.
- The image-processing back-end (background removal + `#FFFFFF` fill) is a swappable dependency behind an `IImageProcessingJob` abstraction.
- Existing repositories (`MarketplaceAdvisory.Security`, `MarketplaceAdvisory.BFF`) provide authentication and the front-end gateway respectively — this feature does not modify their contracts beyond adding new endpoints that will be consumed by BFF and secured by Security.
- Tenant thresholds and floors are set at onboarding and can be updated by a `Manager` role at any time.
- Tests execute against ephemeral local containers (PostgreSQL + Redis) via Docker Compose, matching the current Core repository setup.
- Nested Bundles (a Bundle inside a Bundle) are out of scope for v1.
