# Endpoints — MarketplaceAdvisory.Core

> Reference for every HTTP endpoint planned for the Core API in the MarketplaceCopilot Platform Foundation feature.
>
> **Status**: contracts frozen (`specs/001-marketplacecopilot-platform/contracts/`), implementation in progress per `tasks.md`.
>
> **Auth**: all endpoints require a JWT issued by `MarketplaceAdvisory.Security`. Roles are `User` and `Manager` (`SharedKernel.Authentication.AppRoles`). See [`architecture.md`](./architecture.md) for the full flow.
>
> **Versioning**: everything under `/api/v{version}/...` via `Asp.Versioning`. Current version: `v1`.
>
> **Errors**: RFC 7807 `ProblemDetails` produced by the shared `ToProblem` mapping.

---

## Existing endpoints (already implemented, retained)

| Method | Path | Auth | Purpose | Source |
|--------|------|------|---------|--------|
| `GET`  | `/api/v1/products/{id}` | `User` or `Manager` | Return a single product projection. | `ProductsController.GetById` |
| `POST` | `/api/v1/products` | `Manager` | Create a product (RBAC demonstration). | `ProductsController.Create` |
| `POST` | `/api/v1/identity/token/user` | anonymous | Proxy to Security — issue a `User` JWT. | `IdentityProxyController.GetUserToken` |
| `POST` | `/api/v1/identity/token/manager` | anonymous | Proxy to Security — issue a `Manager` JWT. | `IdentityProxyController.GetManagerToken` |

---

## Financial Engine (`/api/v1/financial`)

Full contract: [`../specs/001-marketplacecopilot-platform/contracts/financial-engine.md`](../specs/001-marketplacecopilot-platform/contracts/financial-engine.md)

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `POST` | `/api/v1/financial/products/{productId}/costs` | `Manager` | Update the cost inputs (CMV, category, weight, dimensions) for a SKU and recompute Net Profit/Margin/Status per marketplace. |
| `POST` | `/api/v1/financial/products/{productId}/sale-price` | `Manager` | Set the sale price on a marketplace. **Consults `IProfitabilityFloorGuard` (Principle VIII)**. Returns `409 code:floor.hit` or `409 code:floor.override_required` when the floor blocks the write. |
| `POST` | `/api/v1/financial/simulator` | `User` \| `Manager` | Ideal-Price Simulator (Rule A3): given a target margin %, returns the sale price that yields that margin, honoring the shipping tier at the suggested price. |
| `GET`  | `/api/v1/financial/products/{productId}/profitability` | `User` \| `Manager` | Current profitability snapshot per marketplace for a SKU. |
| `GET`  | `/api/v1/financial/dashboard` | `User` \| `Manager` | Traffic-light dashboard, paginated and filterable by `status` / `marketplace`. Backed by the Dapper view `catalog.product_profitability_v`. |
| `PUT`  | `/api/v1/financial/tenants/{tenantId}/thresholds` | `Manager` | Update Green/Yellow/Floor thresholds for the tenant. Triggers full dashboard recomputation. |

---

## Dynamic Repricer & Competitor Radar (`/api/v1/pricing`)

Full contract: [`../specs/001-marketplacecopilot-platform/contracts/repricer.md`](../specs/001-marketplacecopilot-platform/contracts/repricer.md)

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `POST` | `/api/v1/pricing/repricer/{productId}` | `Manager` | Enable/disable the automated repricer for a SKU per marketplace and set `underCutDelta` / `minCadenceMinutes`. |
| `GET`  | `/api/v1/pricing/repricer/{productId}/history` | `User` \| `Manager` | Paginated history of `RepricerDecision` rows (Lowered / Held / NoChange). Backed by `pricing.repricer_history_v`. |
| `POST` | `/api/v1/pricing/radar/{productId}/competitors` | `Manager` | Register a competitor listing URL to be observed. |
| `GET`  | `/api/v1/pricing/radar/{productId}/competitors` | `User` \| `Manager` | List current `CompetitorObservation` snapshots for a SKU. |

> **Internal (job-only)**: `RepricerJob`, `CompetitorRadarJob` — not HTTP-exposed. Scheduled via Quartz, per marketplace, `[DisallowConcurrentExecution]`.

---

## AI Listing / Image Pipeline / Kit Maker (`/api/v1/listings`)

Full contract: [`../specs/001-marketplacecopilot-platform/contracts/ai-productivity.md`](../specs/001-marketplacecopilot-platform/contracts/ai-productivity.md)

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `POST` | `/api/v1/listings/ai/rewrite` | `Manager` | Kick off an AI-driven rewrite of a listing from a competitor URL. Materializes an `AIJob` before any outbound call (audit). Returns `202`. |
| `GET`  | `/api/v1/listings/ai/jobs/{jobId}` | `User` \| `Manager` | Poll an AI job status. Returns the produced `ListingDraft` on `Succeeded`. |
| `POST` | `/api/v1/listings/ai/drafts/{draftId}/approve` | `Manager` | Attach a draft to a product. Consults the Floor Guard if a suggested price is included. |
| `POST` | `/api/v1/listings/images/clean` | `Manager` | Submit an image for background removal + `#FFFFFF` fill. Runs as a background job (Redis + Quartz). |
| `POST` | `/api/v1/listings/bundles` | `Manager` | Create a virtual Bundle SKU from two or more concrete SKUs. Computes weight (Σ), dimensions (Max/Max/Sum) and suggested price via the Ideal-Price Simulator. |
| `GET`  | `/api/v1/listings/bundles/{bundleId}` | `User` \| `Manager` | Current bundle state, computed metrics and per-marketplace suggested price. |

---

## Auto-parts Compatibility Search (`/api/v1/autoparts`)

Full contract: [`../specs/001-marketplacecopilot-platform/contracts/autoparts.md`](../specs/001-marketplacecopilot-platform/contracts/autoparts.md)

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `GET`  | `/api/v1/autoparts/search` | `User` \| `Manager` | Search by manufacturer part code (e.g., `BOSCH0986AN0510`) and return compatible vehicles/years/engines with each marketplace's official catalog id. Served by `ICompatibilitySearchIndex` (Meilisearch by default). |
| `GET`  | `/api/v1/autoparts/products/{productId}/compatibility` | `User` \| `Manager` | Compatibility list currently attached to a product. |
| `POST` | `/api/v1/autoparts/products/{productId}/compatibility` | `Manager` | Attach compatibility entries to a product. **Required**: non-empty marketplace `catalogId` per entry (FR-C2). Free-text-only entries are rejected. |
| `POST` | `/api/v1/autoparts/products/{productId}/compatibility/publish` | `Manager` | Publish the attached compatibility list to selected marketplaces through the ACL adapter. Per-marketplace status reported. |

---

## Smart SAC (`/api/v1/sac`)

Full contract: [`../specs/001-marketplacecopilot-platform/contracts/sac.md`](../specs/001-marketplacecopilot-platform/contracts/sac.md)

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `GET`  | `/api/v1/sac/inbox` | `User` \| `Manager` | Paginated unified inbox across marketplaces, filterable by `status` / `marketplace`. Backed by `support.inbox_v`. |
| `GET`  | `/api/v1/sac/threads/{threadId}` | `User` \| `Manager` | Full thread (messages + linked draft, if any). |
| `POST` | `/api/v1/sac/threads/{threadId}/draft` | `Manager` | Force generation of an AI draft reply. Also triggered automatically for new inbound messages. |
| `POST` | `/api/v1/sac/threads/{threadId}/reply/approve` | `Manager` | One-click approval — sends the current draft (optionally edited) through the marketplace adapter and marks the thread `Answered`. |
| `POST` | `/api/v1/sac/threads/{threadId}/reply/reject` | `Manager` | Reject the current draft; operator writes from scratch or triggers a new draft. |

> **Internal (job-only)**: `SacDraftJob` — watches for new inbound messages, injects the product's `CompatibilityList` and other context into the AI prompt and materializes a `MessageReplyDraft`.

---

## Cross-cutting response conventions

- **Auth failure**: `401` on missing/invalid JWT, `403` on wrong role (`RequireManager` / `RequireUser` policies).
- **Validation**: `400` with `type: Validation`, `title` describing the failure, list of field errors.
- **Not found**: `404` with `type: NotFound`.
- **Conflict**: `409` with `type: Conflict` — for the Financial Engine, `code: floor.hit` and `code: floor.override_required` are the two Principle VIII outcomes.
- **Adapter unavailable**: `503` with `type: DependencyUnavailable`. Other marketplaces are still served (Principle VII).
- **Async operations**: `202 Accepted` with an `AIJobId` — poll `GET /api/v1/listings/ai/jobs/{id}` or the SAC equivalent to get the terminal status.
