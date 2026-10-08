# VNS.3CStats

Independent three-cushion billiards statistics and head-to-head database.

## Goals

- Aggregate official tournament results from multiple billiards organizations.
- Normalize players, tournaments and matches into a canonical data model.
- Preserve source provenance and raw-source metadata for verification/reprocessing.
- Provide player, tournament, match and head-to-head queries.
- Start with CEB as the first ingestion source, followed by UMB and other federations.

## Architecture

```text
src/
  VNS.3CStats.Domain/          Core billiards domain
  VNS.3CStats.Application/     Use cases, queries, ingestion contracts
  VNS.3CStats.Infrastructure/  EF Core, Identity, source adapters, normalization
  VNS.3CStats.Web/             ASP.NET Core MVC UI and administration

tests/
  VNS.3CStats.Domain.Tests/
  VNS.3CStats.Application.Tests/
  VNS.3CStats.Infrastructure.Tests/
```

Dependency direction:

```text
Domain <- Application <- Infrastructure
          ^                  ^
          +------- Web ------+
```

The project is standalone and has no dependency on VCMS Website or other VNS applications.

## Authentication baseline

- ASP.NET Core Identity
- Local email/password login
- Google external login
- Microsoft external login
- Public registration disabled by default, but designed to be enabled later
- Emergency Super Admin login configured exclusively through environment variables

Emergency credentials must never be committed to configuration files. The emergency principal is independent from Identity database users so it remains usable for recovery when Identity/database authentication is unavailable.

## Data ingestion principles

```text
Source -> Fetch -> Raw source -> Parse -> Normalize -> Identity match -> Validate -> Canonical database
```

Parsers do not directly create canonical players. Source player identities and aliases are resolved during normalization.

Every imported match retains provenance back to its source document. Raw HTML/PDF metadata and hashes are retained so parsers can be rerun when they evolve.

## First vertical slice

1. Fetch and parse one CEB tournament.
2. Persist players, tournament and matches.
3. Expose player and tournament views.
4. Provide head-to-head queries derived from canonical matches.

UMB/ACBC and additional sources come after the CEB pipeline is stable.
