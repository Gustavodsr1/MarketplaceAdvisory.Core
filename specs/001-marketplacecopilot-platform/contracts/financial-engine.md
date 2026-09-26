# Contracts — Module A: Financial Engine

**Spec**: [../spec.md](../spec.md) · **Plan**: [../plan.md](../plan.md) · **Data model**: [../data-model.md](../data-model.md)

All endpoints are versioned under `/api/v1/financial` and require a valid JWT issued by the Security service (Principle IV).

Error responses use RFC 7807 `ProblemDetails` produced by the existing `ToProblem` mapping.

---

## POST /api/v1/financial/products/{productId}/costs

**Purpose**: Register or update the cost inputs (CMV, category, weight, dimensions) for a SKU. Triggers immediate recomputation of Net Profit and traffic-light status for every configured marketplace.

**Auth**: `Manager` (writes) — the seller must have permission to change financial inputs.

**Request** (`UpdateProductCostsRequest`):

```json
{
  "cmv": { "amount": 50.00, "currency": "BRL" },
  "weightGrams": 300,
  "dimensionsCm": { "width": 20, "height": 15, "length": 10 },
  "categoryRefs": [
    { "marketplace": "MercadoLivre", "categoryId": "MLB1234" },
    { "marketplace": "Shopee",       "categoryId": "SP1234" }
  ]
}
```

**Response 200** (`ProductProfitabilityDto[]`):

```json
[
  {
    "marketplace": "MercadoLivre",
    "salePrice":  { "amount": 100.00, "currency": "BRL" },
    "netProfit":  { "amount":   9.00, "currency": "BRL" },
    "netMarginPercent": 9.0,
    "status": "Yellow",
    "minimumPriceFloor": { "amount": 95.00, "currency": "BRL" }
  }
]
```

**Errors**: `400 Validation` (negative CMV/weight), `404 NotFound` (unknown product), `409 Conflict` (concurrency).

---

## POST /api/v1/financial/products/{productId}/sale-price

**Purpose**: Set the sale price of a SKU on a marketplace. **Consults `IProfitabilityFloorGuard` (Principle VIII)** before persisting.

**Auth**: `Manager`.

**Request** (`SetSalePriceRequest`):

```json
{
  "marketplace": "MercadoLivre",
  "price":       { "amount": 100.00, "currency": "BRL" },
  "override":    null
}
```

Override structure (`HumanOverrideToken`) is only required when a previous attempt returned `409 Conflict` with `code: floor.override_required`:

```json
{
  "override": {
    "reasonCode": "clearance-liquidation",
    "reasonNote": "Liquidação Q4",
    "acceptedLoss": { "amount": 3.00, "currency": "BRL" }
  }
}
```

**Response 200**: same as above (`ProductProfitabilityDto` for the affected marketplace).
**Errors**: `409 Conflict` `code: floor.hit` (write held, floor value returned), `409 Conflict` `code: floor.override_required` (retry with an override token).

---

## POST /api/v1/financial/simulator

**Purpose**: Ideal-Price Simulator (Rule A3). Given a target margin, returns the sale price that yields that margin.

**Auth**: `User` or `Manager`.

**Request** (`SimulatorRequest`):

```json
{
  "productId":            "8f9b...",
  "marketplace":          "MercadoLivre",
  "targetNetMarginPercent": 15.0
}
```

**Response 200** (`SimulatorResponse`):

```json
{
  "suggestedSalePrice": { "amount": 118.42, "currency": "BRL" },
  "computedMarginPercent": 15.00,
  "shippingTierAtSuggestedPrice": "0-500g",
  "minimumPriceFloor": { "amount": 95.00, "currency": "BRL" }
}
```

**Errors**: `400 Validation` (target margin out of range), `422 Unprocessable` (no shipping tier covers the resulting volumetric weight).

---

## GET /api/v1/financial/products/{productId}/profitability

**Purpose**: Return current `ProductProfitabilityDto` per marketplace for a SKU.

**Auth**: `User` or `Manager`.

**Response 200**: array of `ProductProfitabilityDto`.

---

## GET /api/v1/financial/dashboard

**Purpose**: Traffic-light dashboard — paginated list of SKUs and their current status, backed by the Dapper read model `catalog.product_profitability_v`.

**Auth**: `User` or `Manager`.

**Query parameters**:

- `status` — optional filter: `Green | Yellow | Red`.
- `marketplace` — optional filter: `MercadoLivre | Shopee | Amazon`.
- `page`, `pageSize` — pagination (`pageSize` ≤ 100).

**Response 200** (`ProfitabilityDashboardPage`):

```json
{
  "page": 1,
  "pageSize": 50,
  "total": 812,
  "items": [
    {
      "productId": "...",
      "sku": "PN-1024",
      "title": "Filtro de óleo Bosch",
      "marketplace": "MercadoLivre",
      "netMarginPercent": 9.0,
      "status": "Yellow"
    }
  ]
}
```

---

## PUT /api/v1/financial/tenants/{tenantId}/thresholds

**Purpose**: Update the Green/Yellow/Floor thresholds for the tenant. Triggers a full recomputation of the dashboard read model.

**Auth**: `Manager`.

**Request** (`ProfitabilityThresholdsDto`):

```json
{ "greenPercent": 12.0, "yellowPercent": 1.0, "floorPercent": 0.0 }
```

**Response 204**.
**Errors**: `400 Validation` if `green < yellow` or `yellow < floor` or any negative.
