# CiccioSoft.Data.Sqlite.AsyncConcurrency.Tests

![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET Version](https://img.shields.io/badge/.NET-10.0-purple.svg)
![Language](https://img.shields.io/badge/language-C%23-brightgreen.svg)
![Testing](https://img.shields.io/badge/testing-xUnit-green.svg)

Test suite validating asynchronous operations and concurrency features of CiccioSoft.Data.Sqlite.

## Overview

This test project validates the core async and concurrency capabilities of the CiccioSoft.Data.Sqlite provider, ensuring:

- **Truly Asynchronous Methods**: All async APIs are cooperative (no `Task.Run` wrappers)
- **WAL Journaling Benefits**: Concurrent reads/writes work correctly with WAL mode
- **Cancellation Support**: `CancellationToken` propagation works properly
- **Thread Safety**: Multiple concurrent operations don't corrupt data

## Test Scenarios

### Async Method Validation
- `AsyncMethods_DoNotBlock`: Verifies async methods complete immediately (sync wrappers)
- `CancellationToken_Propagates`: Ensures cancellation tokens are respected

### WAL Concurrency Tests
- `ConcurrentReads_WithWAL_ShouldWork`: Multiple simultaneous reads from same data
- `ConcurrentWrites_WithWAL_ShouldWork`: Parallel writes to same table
- `MixedReadWrite_WithWAL_ShouldWork`: Concurrent reads and writes together

## Key Test Explanations

### Why WAL Matters for Async

SQLite's default rollback journaling requires exclusive locks during writes, blocking all reads. WAL (Write-Ahead Logging) changes this:

- **Concurrent Access**: Readers and writers can operate simultaneously
- **Snapshot Isolation**: Readers see consistent database snapshots
- **Buffered Writes**: Changes accumulate in WAL file before main database update
- **Periodic Checkpoint**: WAL changes merged into main file during low activity

### Async Implementation Details

The provider implements "cooperative async" for SQLite operations:

- **No Thread Pool Usage**: Methods return `Task.CompletedTask` or `Task.FromResult`
- **Cancellation via Interrupt**: `CancellationToken` triggers native `sqlite3_interrupt`
- **Timeout Enforcement**: Command timeouts use native interrupt mechanism
- **Non-Blocking Semantics**: Caller thread remains free during database operations

## Running Tests

```bash
dotnet test CiccioSoft.Data.Sqlite.AsyncConcurrency.Tests.csproj
```

## Dependencies

- `CiccioSoft.Data.Sqlite`: The main provider library
- `xUnit`: Testing framework
- .NET 10.0 or later

## Test Data

Tests create temporary SQLite databases that are cleaned up automatically. Each test uses unique database files to avoid interference.

## Coverage Areas

- **Async API Surface**: All async methods (OpenAsync, ExecuteReaderAsync, etc.)
- **Concurrency Patterns**: Reader/writer concurrency with WAL
- **Cancellation Handling**: Token propagation and interrupt behavior
- **Thread Safety**: Multi-threaded access patterns
- **Performance Characteristics**: Ensuring non-blocking behavior