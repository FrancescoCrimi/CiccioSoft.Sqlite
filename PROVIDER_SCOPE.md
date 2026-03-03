# CiccioSoft.Data.Sqlite – Provider Scope & Architecture Policy

This document defines the product/architecture scope for `CiccioSoft.Data.Sqlite`.

## Vision

Build a reliable SQLite provider for .NET that is:
- compliant with ADO.NET core contracts,
- practical for ORM integration,
- intentionally minimal and maintainable.

This project is **not** intended to be a 1:1 reimplementation of `Microsoft.Data.Sqlite`.

## Prioritization Model

### Tier 1 — ADO.NET Core (must-have)
Features required for robust ORM usage and general provider interoperability:
- `DbConnection` lifecycle and state semantics.
- `DbCommand` execution semantics (sync/async/cancellation).
- `DbParameter` binding and type conversion behavior.
- `DbDataReader` typed reads and schema/ordinal behavior.
- `DbTransaction` base semantics (`Begin`, `Commit`, `Rollback`).
- Error mapping into provider exceptions.

All Tier 1 behavior should be covered by active tests.

### Tier 2 — Common Cross-Provider Extras (should-have)
Optional enhancements accepted only when they are common across mainstream ADO.NET providers
(e.g., SqlServer/Oracle/MariaDB/PostgreSQL ecosystems) and useful for ORM or data-access patterns.

Examples:
- predictable timeout behavior,
- commonly supported connection-string options,
- provider-factory integration consistency.

## Explicit Non-Goal

- Achieving full parity with `Microsoft.Data.Sqlite` feature-for-feature.

## Test Strategy Alignment

- Active tests should validate Tier 1 + accepted Tier 2 behavior.
- Copied parity suites that enforce out-of-scope behavior should stay deferred/guarded.
- Scope decisions must be documented in `CiccioSoft.Data.Sqlite.Tests/TEST_PORTING_AUDIT.md`.

## Change Acceptance Rule

A new feature is accepted when:
1. it strengthens Tier 1 compliance, or
2. it is a proven Tier 2 cross-provider need with reasonable maintenance burden.

Otherwise, it remains out of scope for this provider.
