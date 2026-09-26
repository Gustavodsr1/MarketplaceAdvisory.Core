# Contracts — Module D: Smart SAC (Inbox + AI Reply)

**Spec**: [../spec.md](../spec.md) · **Plan**: [../plan.md](../plan.md) · **Data model**: [../data-model.md](../data-model.md)

All endpoints are versioned under `/api/v1/sac`. The read side is served by the `support.inbox_v` projection.

---

## GET /api/v1/sac/inbox

**Purpose**: Paginated, tenant-scoped unified inbox across all marketplaces (FR-D1).

**Auth**: `User` or `Manager`.

**Query parameters**:

- `status` — `Unread | Read | Answered` (optional).
- `marketplace` — optional filter.
- `page`, `pageSize` (default 25, max 100).

**Response 200** (`InboxPage`):

```json
{
  "page": 1, "pageSize": 25, "total": 340,
  "items": [
    {
      "threadId": "...",
      "marketplace": "MercadoLivre",
      "linkedProductId": "...",
      "linkedProductSku": "PN-1024",
      "lastMessageAt": "2026-09-24T21:55:00Z",
      "lastMessageSnippet": "Serve no Golf 2012?",
      "unreadCount": 1,
      "hasDraftReply": true
    }
  ]
}
```

---

## GET /api/v1/sac/threads/{threadId}

**Purpose**: Return the full thread (all `CustomerMessage` items + linked `MessageReplyDraft` if any).

**Auth**: `User` or `Manager`.

**Response 200** (`ThreadDto`):

```json
{
  "threadId": "...",
  "marketplace": "MercadoLivre",
  "messages": [
    {
      "id": "...",
      "direction": "Inbound",
      "body": "Serve no Golf 2012?",
      "arrivedAt": "2026-09-24T21:55:00Z"
    }
  ],
  "draftReply": {
    "id": "...",
    "body": "Sim, este filtro é compatível com Golf 1.6 2012 (motores BSE/BSF). ...",
    "status": "Draft"
  }
}
```

---

## POST /api/v1/sac/threads/{threadId}/draft

**Purpose**: Force the generation of an AI draft reply (FR-D2). Also triggered automatically when a new inbound message arrives. Enqueues an `AIJob` of kind `SacDraft` that materializes a `MessageReplyDraft`.

**Auth**: `Manager`.

**Response 202** (`AIJobStartedDto`).
**Errors**: `409 Conflict` (a `Draft` already exists for the last inbound message).

---

## POST /api/v1/sac/threads/{threadId}/reply/approve

**Purpose**: One-click approval (FR-D3). Sends the current `Draft` through the correct marketplace adapter (Principle VII) and marks the thread `Answered`.

**Auth**: `Manager`.

**Request** (`ApproveReplyRequest`):

```json
{ "editedBody": null }
```

If `editedBody` is provided, it replaces the draft body before sending; the edit is logged for audit (FR-D4).

**Response 200** (`SendReplyResult`):

```json
{ "messageId": "...", "sentAt": "2026-09-24T22:00:12Z" }
```

**Errors**: `503` if the marketplace adapter is down (the reply stays in `Approved`, waiting for adapter recovery).

---

## POST /api/v1/sac/threads/{threadId}/reply/reject

**Purpose**: Reject the current `Draft` (operator will write from scratch or trigger a new draft).

**Auth**: `Manager`.

**Request**: `{ "reason": "off-brand-tone" }`
**Response 204**.

---

## Internal (job-only)

- `SacDraftJob` — Quartz job that watches for new inbound messages, fetches the product's `CompatibilityList` and other context, and invokes `IAiProvider.DraftReplyAsync` to materialize a `MessageReplyDraft`.
