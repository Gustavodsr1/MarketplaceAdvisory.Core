# MarketplaceAdvisory.Core

Backend for **MarketplaceCopilot** — a B2B SaaS for high-volume Brazilian marketplace sellers
(Mercado Livre, Shopee, Amazon). Its promise: eliminate "financial blindness" by giving
sellers real-time net profit per SKU, protecting every automated write path from selling at a
loss, and automating listing/reply/pricing workflows.

.NET 10 · Clean Architecture + DDD · CQRS-lite · Multi-tenant · RBAC via JWT.

## Modules

| Code | Module | HTTP prefix |
|------|--------|-------------|
| A | **Financial Engine** (Guardião da Lucratividade) — cost, margin, traffic light, ideal-price simulator | `/api/v1/financial` |
| B | **AI & Productivity Pipeline** — competitor rewrite, image cleanup, Kit Maker | `/api/v1/listings` |
| C | **Auto-parts Search** — part code → compatible vehicles/years/engines | `/api/v1/autoparts` |
| D | **Smart SAC** — unified inbox with one-click AI-drafted replies | `/api/v1/sac` |
| E | **Dynamic Repricer & Competitor Radar** — profit-safe automated pricing | `/api/v1/pricing` |

## Contents

```
MarketplaceAdvisory.Core/          # Domain, Application, Infrastructure, Api (+ tests)
MarketplaceAdvisory.Shared/        # SharedKernel, Contracts, Integrations.Abstractions (vendored)
MarketplaceAdvisory.Aspire/        # ServiceDefaults (OpenTelemetry, health checks)
tools/MarketplaceAdvisory.Migrator # out-of-process EF Core migration runner
docs/                              # architecture.md, endpoints.md
specs/                             # Spec Kit feature specs (see below)
.specify/                          # Spec Kit toolchain (memory/constitution.md is versioned)
```

> The shared libraries are vendored into this repo so it builds independently. Keep them in
> sync across the context repos (or promote them to a shared NuGet feed later).

## Stack

CQRS-lite (EF Core/Npgsql writes + Dapper reads), Redis cache, Quartz.NET jobs, MediatR
(behind an abstraction), FluentValidation, Mapster, ErrorOr, multi-tenancy (TenantId + EF
global query filters). Validates JWTs issued by the Security service and enforces RBAC
(`RequireManager` / `RequireUser` policies).

Deferred by design (waiting on the user): PostgreSQL connection strings, marketplace API
credentials, AI provider keys, blob storage credentials. Everything is abstracted so the
platform builds and tests today against ships-with-the-repo fakes.

## Documentation

- **Product & architecture** — [`docs/architecture.md`](./docs/architecture.md)
- **HTTP endpoints reference** — [`docs/endpoints.md`](./docs/endpoints.md)
- **Constitution (principles & governance)** — [`.specify/memory/constitution.md`](./.specify/memory/constitution.md)
- **Current feature** — [`specs/001-marketplacecopilot-platform/`](./specs/001-marketplacecopilot-platform/)
  - [`spec.md`](./specs/001-marketplacecopilot-platform/spec.md) — user stories, functional requirements, success criteria
  - [`plan.md`](./specs/001-marketplacecopilot-platform/plan.md) — implementation plan + Constitution Check
  - [`research.md`](./specs/001-marketplacecopilot-platform/research.md) — decisions (Meilisearch, Quartz, AI provider…)
  - [`data-model.md`](./specs/001-marketplacecopilot-platform/data-model.md) — aggregates, value objects, events
  - [`contracts/`](./specs/001-marketplacecopilot-platform/contracts) — HTTP contracts per module
  - [`quickstart.md`](./specs/001-marketplacecopilot-platform/quickstart.md) — end-to-end validation guide
  - [`tasks.md`](./specs/001-marketplacecopilot-platform/tasks.md) — Phases 1–5 of the development plan

## Spec-Driven Development (SpecKit)

This repository is managed with [GitHub Spec Kit](https://github.com/github/spec-kit). The
`.specify/` toolchain and the `.github/skills/speckit-*/` skills are regenerable — clone the
repo on a new machine and run:

```powershell
specify init --here --force --integration copilot --script ps
```

Then use the slash commands in Copilot Chat:

- `/speckit-constitution` — amend the project constitution
- `/speckit-specify` — create a new feature spec
- `/speckit-clarify` — resolve ambiguities before planning
- `/speckit-plan` — produce the implementation plan + design artifacts
- `/speckit-tasks` — break plan into actionable tasks
- `/speckit-implement` — execute the tasks

The **product of SpecKit** (`.specify/memory/constitution.md` and everything under `specs/`)
IS versioned. The toolchain boilerplate (`.specify/scripts/`, `.specify/templates/`,
`.github/skills/speckit-*/`) is gitignored — regenerate it locally.

## Build & test

```powershell
docker compose up -d          # PostgreSQL + Redis + Seq (add Meilisearch when Auto-parts lands)
dotnet build MarketplaceAdvisory.Core.sln
dotnet test  MarketplaceAdvisory.Core.sln
dotnet run --project MarketplaceAdvisory.Core/MarketplaceAdvisory.Core.Api
```

Swagger is served at `https://localhost:<port>/swagger`. Use the **Bearer** button and paste
a JWT obtained from `POST /api/v1/identity/token/manager` (which proxies to the Security
service).

## Related repositories

- **MarketplaceAdvisory.Security** — Identity Provider (JWT issuer).
- **MarketplaceAdvisory.BFF** — API gateway consumed by the Angular front-end.

## Push to GitHub

```powershell
# Option A — GitHub CLI (after `gh auth login`)
gh repo create MarketplaceAdvisory.Core --private --source . --remote origin --push

# Option B — create an empty repo on github.com, then:
git remote add origin https://github.com/Gustavodsr1/MarketplaceAdvisory.Core.git
git push -u origin main
```
