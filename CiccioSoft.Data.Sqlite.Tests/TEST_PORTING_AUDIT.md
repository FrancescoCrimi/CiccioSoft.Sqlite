# CiccioSoft.Data.Sqlite.Tests – Microsoft.Data.Sqlite porting audit

This file tracks which copied test suites are currently usable for `CiccioSoft.Data.Sqlite` and which are intentionally deferred.

## Kept active now (aligned with current provider direction)
- `SqliteBehaviorTests.cs`
- `SqliteConnectionProfileTest.cs`
- `SqliteParameterBindingParityTest.cs`
- `SqliteExceptionTest.cs`
- `SqliteFactoryTest.cs`
- `SqliteTransactionSemanticsTest.cs` (provider-aligned transaction behavior and isolation mapping)
- `SqliteDataReaderTest.cs` (reader behavior remains a parity target)
- `SqliteCommandTest.cs` (command semantics and cancellation remain relevant)
- `SqliteParameterTest.cs` (binding-related parity target; keep for future interop/binding completion)

## Deferred / commented out for now
These suites were copied from `Microsoft.Data.Sqlite` but are not aligned with the current
opinionated design (Default + StrictSingleConnection profiles, no full keyword/options parity).

- `SqliteConnectionStringBuilderTest.cs`
  - relies on extensive keyword aliasing and mode/cache compatibility surface.
- `SqliteConnectionTest.cs`
  - expects Microsoft-compatible connection string and open-mode behaviors.
- `SqliteConnectionFactoryTest.cs`
  - expects internal handle/pooling behaviors tied to Microsoft provider contracts.
- `SqliteBlobTest.cs`
  - depends on blob-stream API/features not yet in current provider scope.
- `SqliteTransactionTest.cs`
  - expects Microsoft-compatible savepoint/deferred APIs and richer transaction surface not yet implemented.

These suites are wrapped in `#if CICCIOSOFT_ENABLE_MICROSOFT_PARITY_TESTS` and can be re-enabled later.

## Notes
- Binding-related tests with missing implementation are intentionally kept in place, per roadmap.
- When interop/binding work lands, remove deferrals incrementally and adapt expectations explicitly.
