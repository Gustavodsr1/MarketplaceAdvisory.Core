# Contracts — Module B: AI Listing Copier, Image Pipeline & Kit Maker

**Spec**: [../spec.md](../spec.md) · **Plan**: [../plan.md](../plan.md) · **Data model**: [../data-model.md](../data-model.md)

All endpoints are versioned under `/api/v1/listings`.

---

## POST /api/v1/listings/ai/rewrite

**Purpose**: Kick off an AI-driven listing rewrite from a competitor URL (FR-B1). Runs asynchronously and materializes an `AIJob` before doing anything else (Principle: audit).

**Auth**: `Manager`.

**Request** (`RewriteListingRequest`):

```json
{ "competitorUrl": "https://...", "linkedProductId": null }
```

**Response 202** (`AIJobStartedDto`):

```json
{ "aiJobId": "...", "kind": "ListingRewrite", "status": "Pending" }
```

**Errors**: `400 Validation` (invalid URL), `429` (per-tenant AI quota exceeded).

---

## GET /api/v1/listings/ai/jobs/{jobId}

**Purpose**: Poll an AI job's status.

**Auth**: `User` or `Manager`.

**Response 200** (`AIJobStatusDto`):

```json
{
  "aiJobId": "...",
  "kind": "ListingRewrite",
  "status": "Succeeded",
  "result": {
    "listingDraftId": "...",
    "rewrittenTitle": "Filtro de Óleo Premium Compatível com Golf 2012",
    "rewrittenDescription": "..."
  }
}
```

**Errors**: `404 NotFound`.

---

## POST /api/v1/listings/ai/drafts/{draftId}/approve

**Purpose**: Attach the AI draft to a `Product`, replacing title/description. Consults `IProfitabilityFloorGuard` if a suggested price is included.

**Auth**: `Manager`.

**Request** (`ApproveDraftRequest`):

```json
{ "productId": "...", "keepOriginalPrice": true }
```

**Response 200**: `ListingDraftDto` with `Status: Approved`.
**Errors**: `409 Conflict` `code: floor.hit` (if `keepOriginalPrice=false` and the draft's suggested price violates the floor).

---

## POST /api/v1/listings/images/clean

**Purpose**: Submit an image for background removal + `#FFFFFF` fill (FR-B2). Runs asynchronously.

**Auth**: `Manager`.

**Request** (`CleanImageRequest`):

```json
{ "productId": "...", "sourceImageRef": "s3://.../raw/xyz.jpg" }
```

**Response 202** (`AIJobStartedDto` with `kind: ImageCleanup`).

---

## POST /api/v1/listings/bundles

**Purpose**: Create a `Bundle` from two or more concrete SKUs (FR-B3). Computes weight (Σ), dimensions (Max/Max/Sum) and calls the Ideal-Price Simulator to suggest a price.

**Auth**: `Manager`.

**Request** (`CreateBundleRequest`):

```json
{
  "sku": "BUNDLE-KIT-A",
  "items": [
    { "productId": "...X", "quantity": 1 },
    { "productId": "...Y", "quantity": 1 }
  ],
  "targetNetMarginPercent": 15.0
}
```

**Response 201** (`BundleDto`):

```json
{
  "id": "...",
  "sku": "BUNDLE-KIT-A",
  "weightGrams": 500,
  "dimensionsCm": { "width": 20, "height": 15, "length": 15 },
  "suggestedPrices": [
    { "marketplace": "MercadoLivre", "price": { "amount": 235.00, "currency": "BRL" } }
  ]
}
```

**Errors**: `400 Validation` (less than 2 items, nested bundle), `409 Conflict` (SKU already exists), `404 NotFound` (unknown item SKU).

---

## GET /api/v1/listings/bundles/{bundleId}

**Purpose**: Return current `BundleDto` with computed metrics and profitability projections.

**Auth**: `User` or `Manager`.
