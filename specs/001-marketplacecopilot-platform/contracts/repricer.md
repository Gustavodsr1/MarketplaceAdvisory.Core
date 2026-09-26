# Contracts — Module E: Dynamic Repricer & Competitor Radar

**Spec**: [../spec.md](../spec.md) · **Plan**: [../plan.md](../plan.md) · **Data model**: [../data-model.md](../data-model.md)

All endpoints are versioned under `/api/v1/pricing`.

---

## POST /api/v1/pricing/repricer/{productId}

**Purpose**: Enable or disable the automated repricer for a SKU on a marketplace and configure its knobs.

**Auth**: `Manager`.

**Request** (`RepricerConfigDto`):

```json
{
  "marketplace": "MercadoLivre",
  "enabled": true,
  "underCutDelta": 0.01,
  "minCadenceMinutes": 5
}
```

**Response 200**: `RepricerConfigDto` (persisted).
**Errors**: `400 Validation` (negative delta, cadence below the tenant minimum), `404 NotFound`.

---

## GET /api/v1/pricing/repricer/{productId}/history

**Purpose**: Return the history of `RepricerDecision` rows for a SKU/marketplace, from the read model `pricing.repricer_history_v`.

**Auth**: `User` or `Manager`.

**Query parameters**:

- `marketplace` (optional), `from`, `to` (ISO timestamps), `action` (`Lowered | Held | NoChange`, optional), `page`, `pageSize`.

**Response 200** (`RepricerHistoryPage`):

```json
{
  "page": 1, "pageSize": 50, "total": 2100,
  "items": [
    {
      "evaluatedAt": "2026-09-24T22:00:00Z",
      "marketplace": "MercadoLivre",
      "competitorPriceObserved": { "amount": 100.00, "currency": "BRL" },
      "minimumPriceFloorApplied": { "amount":  95.00, "currency": "BRL" },
      "action": "Lowered",
      "newPrice": { "amount": 99.99, "currency": "BRL" }
    }
  ]
}
```

---

## POST /api/v1/pricing/radar/{productId}/competitors

**Purpose**: Register a competitor listing URL to be observed for a SKU.

**Auth**: `Manager`.

**Request** (`RegisterCompetitorRequest`):

```json
{ "marketplace": "MercadoLivre", "competitorUrl": "https://..." }
```

**Response 201** (`CompetitorObservationDto`, with initial `Unknown` status until the first radar run):

```json
{
  "id": "...",
  "marketplace": "MercadoLivre",
  "competitorUrl": "https://...",
  "lastSeenAt": null,
  "price": null,
  "available": null,
  "buyBoxPositionHint": "Unknown"
}
```

**Errors**: `400 Validation` (invalid URL / duplicate URL for the SKU).

---

## GET /api/v1/pricing/radar/{productId}/competitors

**Purpose**: List the current `CompetitorObservation` snapshots for a SKU.

**Auth**: `User` or `Manager`.

**Response 200**: array of `CompetitorObservationDto`.

---

## Internal (job-only, not exposed via HTTP)

- `RunRepricerDecisionCommand` — invoked by `RepricerJob` on the Quartz schedule; consults `IProfitabilityFloorGuard` and persists a `RepricerDecision`. Not exposed publicly.
- `CompetitorRadarJob` — scheduled Quartz job that hits each marketplace adapter to refresh the `CompetitorObservation` snapshots. Independent per marketplace (FR-E5).

## Domain events emitted

- `ProfitabilityFloorHit` — when the repricer holds the price because lowering would break the floor (Principle VIII).
- `RepricerDecided(action, newPrice?)` — every job run emits one per SKU/marketplace evaluated.
