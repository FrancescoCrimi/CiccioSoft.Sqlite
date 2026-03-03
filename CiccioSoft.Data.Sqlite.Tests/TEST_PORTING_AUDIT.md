# CiccioSoft.Data.Sqlite.Tests – Microsoft.Data.Sqlite porting audit

This file tracks which copied suites have been pruned and adapted to the current provider scope.

## Active suites (kept and aligned)
- `SqliteBehaviorTests.cs`
- `SqliteConnectionProfileTests.cs`
- `SqliteExceptionTests.cs`
- `SqliteFactoryTests.cs`
- `SqliteParameterBindingParityTests.cs`
- `SqliteParameterCollectionContractTests.cs`
- `SqliteParameterDirectionBehaviorTests.cs`
- `SqliteTransactionSemanticsTests.cs`
- `SqliteCommandTests.cs` (pruned to provider-supported command behaviors)
- `SqliteParameterTests.cs` (pruned to provider-supported parameter contract)
- `SqliteDataReaderTests.cs` (pruned to provider-supported reader contract)

## Deferred suites (still copied but compile-gated)
These remain in-tree for future incremental porting, but are not part of default compilation yet:
- `SqliteBlobTests.cs`
- `SqliteConnectionFactoryTests.cs`
- `SqliteConnectionStringBuilderTests.cs`
- `SqliteConnectionTests.cs`
- `SqliteTransactionTests.cs`

## Scope note
No full Microsoft.Data.Sqlite compatibility contract is targeted. Tests relying on APIs not exposed by
this provider are removed or deferred; tests covering shared ADO.NET/provider behavior are kept.
