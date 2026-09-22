# MarketplaceAdvisory.Core

Standalone repository for the **Core** bounded context of MarketplaceAdvisory
(.NET 10, Clean Architecture + DDD). This is the main back-end system: business logic,
financial calculations, persistence and marketplace integrations.

## Contents

```
MarketplaceAdvisory.Core/          # Domain, Application, Infrastructure, Api (+ tests)
MarketplaceAdvisory.Shared/        # SharedKernel, Contracts, Integrations.Abstractions (vendored)
MarketplaceAdvisory.Aspire/        # ServiceDefaults (OpenTelemetry, health checks)
tools/MarketplaceAdvisory.Migrator # out-of-process EF Core migration runner
```

> The shared libraries are vendored into this repo so it builds independently. Keep them in
> sync across the context repos (or promote them to a shared NuGet feed later).

## Stack

CQRS-lite (EF Core/Npgsql writes + Dapper reads), Redis cache, Quartz.NET jobs, MediatR
(behind an abstraction), FluentValidation, Mapster, ErrorOr, multi-tenancy (TenantId + EF
global query filters). Validates JWTs issued by the Security service and enforces RBAC
(`[Authorize(Roles = "Manager")]` on `POST /api/v1/products`).

## Build & test

```powershell
docker compose up -d          # PostgreSQL + Redis + Seq
dotnet build MarketplaceAdvisory.Core.sln
dotnet test  MarketplaceAdvisory.Core.sln
dotnet run --project MarketplaceAdvisory.Core/MarketplaceAdvisory.Core.Api
```

## Push to GitHub

```powershell
# Option A — GitHub CLI (after `gh auth login`)
gh repo create MarketplaceAdvisory.Core --private --source . --remote origin --push

# Option B — create an empty repo on github.com, then:
git remote add origin https://github.com/Gustavodsr1/MarketplaceAdvisory.Core.git
git push -u origin main
```
