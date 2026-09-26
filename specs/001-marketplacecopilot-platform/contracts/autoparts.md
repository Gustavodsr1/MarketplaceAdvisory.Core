# Contracts — Module C: Auto-parts Compatibility Search

**Spec**: [../spec.md](../spec.md) · **Plan**: [../plan.md](../plan.md) · **Data model**: [../data-model.md](../data-model.md)

All endpoints are versioned under `/api/v1/autoparts`. The search itself is served by the Meilisearch index behind `ICompatibilitySearchIndex` (see `research.md` R3).

---

## GET /api/v1/autoparts/search

**Purpose**: Search by manufacturer part code and return compatible vehicles/years/engines with their marketplace catalog ids (FR-C1, FR-C2).

**Auth**: `User` or `Manager`.

**Query parameters**:

- `partCode` (required) — e.g., `BOSCH0986AN0510`.
- `marketplace` (optional filter).
- `page`, `pageSize` (default 20, max 50).

**Response 200** (`CompatibilitySearchResponse`):

```json
{
  "partCode": "BOSCH0986AN0510",
  "totalHits": 32,
  "items": [
    {
      "vehicle": "VW Golf 1.6",
      "years": [2008, 2009, 2010, 2011, 2012],
      "engineCodes": ["BSE", "BSF"],
      "marketplaceCatalogRefs": [
        { "marketplace": "MercadoLivre", "catalogId": "MLM-COMPAT-42" }
      ]
    }
  ]
}
```

**Errors**: `400 Validation` (empty `partCode`), `503` (search index unavailable — degrade gracefully with a retry hint).

---

## POST /api/v1/autoparts/products/{productId}/compatibility

**Purpose**: Attach one or more compatibility entries (with **required** marketplace catalog ids — FR-C2) to a product.

**Auth**: `Manager`.

**Request** (`AttachCompatibilityRequest`):

```json
{
  "entries": [
    {
      "marketplace": "MercadoLivre",
      "catalogId": "MLM-COMPAT-42",
      "label":     "VW Golf 1.6 2008-2012 BSE/BSF"
    }
  ]
}
```

**Response 200**: updated `ProductCompatibilityDto`.
**Errors**: `400 Validation` (missing `catalogId`), `404 NotFound`.

---

## GET /api/v1/autoparts/products/{productId}/compatibility

**Purpose**: Return the compatibility list already attached to a product.

**Auth**: `User` or `Manager`.

**Response 200**: `ProductCompatibilityDto`.

---

## POST /api/v1/autoparts/products/{productId}/compatibility/publish

**Purpose**: Publish the attached compatibility list to the marketplace via the adapter (FR-C3). The outbound payload uses the exact catalog ids stored — no free-text mapping.

**Auth**: `Manager`.

**Request**: `{ "marketplaces": ["MercadoLivre"] }`

**Response 202**: `PublishStatusDto` (per marketplace: success / error).
**Errors**: `503` if a specific adapter is down (other marketplaces still processed — per-marketplace status returned).
