# Custom SQLite ADO.NET Data Provider

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/language-C%23_14-brightgreen.svg)](https://learn.microsoft.com/dotnet/csharp/)

A high-performance, low-level custom ADO.NET Data Provider for SQLite, written from scratch in C# targeting **.NET 10** and **C# 14**. This project is engineered to minimize context-switching overhead between managed and unmanaged code while offering seamless integration with modern ORMs like **Entity Framework Core, Dapper, and NHibernate**.

---

## 🚀 Project Architecture

The provider is strictly divided into two decoupled layers to isolate marshalling responsibilities from high-level abstractions:

1. **Low-Level Layer (C-API Wrapper):** 
   - Direct interaction with native SQLite C-APIs using `P/Invoke`.
   - Deterministic and safe memory management via custom `SafeHandle` implementations (`SafeSqlite3Handle` and `SafeSqlite3StmtHandle`).
   - Zero-allocation optimizations leveraging `ReadOnlySpan<byte>` and native UTF-8 string marshalling.

2. **High-Level Layer (ADO.NET Provider):**
   - Full implementation of the standard `System.Data.Common` specifications.
   - Core compliant classes: `DbConnection`, `DbCommand`, `DbTransaction`, `DbDataReader`, and `DbBatch`.
   - Native compatibility with expression trees and dialects used by mainstream .NET ORMs.

---

## 📌 Design Philosophy & Concurrency

The core principle of this project is to **embrace the native behavior of SQLite**, completely avoiding artificial C# abstractions that hinder multi-process capabilities.

- **No Custom Pessimistic Concurrency:** We reject software-based lock coordinators (e.g., a custom C# `WalWriterCoordinator`). Concurrency is fully delegated to native `.wal-mmap` files and OS-level locking mechanisms.
- **Native WAL Mode:** Optimized for *Multi-Reader / Single-Writer* scenarios. Concurrent, non-blocking reads are maintained alongside single-writer serialization handled directly by the native engine.
- **Queue Management via `busy_timeout`:** Temporary write conflicts do not throw immediate exceptions. Instead, they leverage the native `sqlite3_busy_timeout` C-API to gracefully queue the operations.
- **Deadlock Prevention via `BEGIN IMMEDIATE`:** The `BeginTransaction()` method maps standard isolation levels to the native `BEGIN IMMEDIATE;` SQL command, acquiring the write lock at the very beginning of the transaction.
- **Efficient Batching:** Built-in implementation of `DbBatch` (inspired by the open-source MIT logic of `Microsoft.Data.Sqlite`) to minimize interop transitions and execute blocks of commands under a single, short-lived implicit transaction.

---

## ✨ Key Features

- ⚡ **Zero-Allocation Hot Paths:** Direct UTF-8 byte span handling (`ReadOnlySpan<byte>`) across statements and parameter bindings, avoiding intermediate string allocations.
- 🔄 **True Cooperative Asynchronous I/O:** Non-blocking async APIs (`OpenAsync`, `ExecuteReaderAsync`, `ReadAsync`) integrated with cancellation tokens and native `sqlite3_interrupt` triggers without `Task.Run` overhead.
- 🛡️ **Deterministic Native Lifetime:** Robust and safe unmanaged resource disposal via `SafeHandle` hierarchies preventing memory and handle leaks under any execution path.
- 📊 **WAL Journaling by Default:** Out-of-the-box multi-reader / single-writer configuration for file-backed databases for optimal concurrent read performance.
- 🔒 **Deadlock-Resistant Transactions:** Native promotion to `BEGIN IMMEDIATE` stops write locks from conflicting or deadlocking halfway through execution.
- 🔌 **Enterprise ORM Compatibility:** Adherence to standard `System.Data.Common` contracts to act as a drop-in engine for Entity Framework Core, Dapper, and NHibernate.

---

## 🔬 ADO.NET Provider Deep Dive

This provider intentionally ships with **opinionated, production-grade defaults** designed to eliminate common SQLite configuration pitfalls.

### Default Behaviors at `Open()`

When a connection is opened, the provider applies these defaults unless explicitly overridden in the connection string:

- **Foreign Keys Enabled by Default** (`PRAGMA foreign_keys=ON;`): Guarantees relational integrity out-of-the-box.
- **WAL Journal Mode by Default** (`PRAGMA journal_mode=WAL;`): Standard for all file-backed databases to allow concurrent readers without blocking writers.
- **Busy Timeout Configured** (`Default Timeout=30`): Translates into native `sqlite3_busy_timeout` (30,000 ms) directly inside the SQLite engine, preventing transient `SQLITE_BUSY` errors.

#### In-Memory Database Semantics
- In-memory databases automatically use `journal_mode=DELETE` (as WAL is not applicable in RAM).
- Named in-memory databases use shared cache by default (`Mode=Memory` / URI `cache=shared`) so multiple connections can access the shared memory space.

### Connection String Configuration

Override defaults or fine-tune behavior using standard connection string parameters and aliases:

| Parameter | Aliases | Default | Description |
|-----------|---------|:-------:|-------------|
| `Data Source` | `Filename`, `DataSource` | `""` | Path to the SQLite database file or URI filename. |
| `Default Timeout` | `Command Timeout`, `default_timeout` | `30` | Default timeout in seconds for commands and native busy timeout. |
| `Foreign Keys` | `foreign_keys` | `True` | Enforces relational foreign key constraints. |
| `Journal Mode` | `journal_mode` | `WAL` | SQLite journaling mode (`WAL`, `DELETE`, `MEMORY`, `OFF`). |
| `Cache` | `cache` | `Default` | Cache sharing mode (`Default`, `Private`, `Shared`). |
| `Mode` | `mode` | `ReadWriteCreate` | Database opening mode (`ReadWriteCreate`, `ReadWrite`, `ReadOnly`, `Memory`). |

Example connection string:
```ini
Data Source=production.db;Foreign Keys=True;Journal Mode=WAL;Default Timeout=60
```

### Async Model: True Cooperative Async

Because SQLite is an embedded engine with synchronous stepping mechanics, our async support avoids fake async (`Task.Run` thread-pool handoffs). Instead, it implements **cooperative cancellation and timeout propagation**:

- Asynchronous ADO.NET contracts (`OpenAsync`, `ExecuteReaderAsync`, `ReadAsync`, `BatchAsync`).
- Direct integration with `CancellationToken` and native `sqlite3_interrupt` to abort active native operations immediately on demand or timeout.
- Internal per-session synchronization ensures thread safety across concurrent method calls while native SQLite handles multi-process concurrency via WAL.

### Explicit WAL Lifecycle APIs

In long-running or high-throughput services, WAL growth can be managed deterministically using first-class maintenance methods:

```csharp
// Execute a passive or truncating checkpoint
await connection.CheckpointAsync(SqliteWalCheckpointMode.Truncate, cancellationToken);

// Trigger planner and statistics optimization
await connection.OptimizeAsync(cancellationToken);
```

---

## 🔍 Comparison with Related Projects

| Feature | CiccioSoft.Data.Sqlite | Microsoft.Data.Sqlite | SQLitePCL.raw |
|:--------|:----------------------:|:---------------------:|:-------------:|
| **Target Runtime** | **.NET 10 / C# 14** | .NET Standard / .NET 8+ | Multi-target |
| **System.Data.Common (ADO.NET)** | ✅ Full | ✅ Full | ❌ (Raw FFI only) |
| **P/Invoke Memory Management** | Custom `SafeHandle` | IntPtr / SafeHandle | IntPtr handles |
| **Zero-Alloc UTF-8 Span Interop** | ✅ Native `ReadOnlySpan<byte>` | ⚠️ Partial | ❌ (Allocations / buffers) |
| **WAL Journal by Default** | ✅ Yes | ❌ (DELETE) | ❌ (Engine default) |
| **Deadlock Prevention (`BEGIN IMMEDIATE`)** | ✅ By Default | ⚠️ Manual (`DEFERRED` default) | N/A |
| **Concurrency Coordination** | Native WAL / OS-level locks | Managed coordination / retry loops | N/A |
| **Batching (`DbBatch`)** | ✅ Built-in | ✅ Built-in | ❌ |
| **ORM Readiness (EF Core / Dapper)** | ✅ Drop-in Ready | ✅ Drop-in Ready | ❌ |
| **License** | MIT | MIT | Apache 2.0 |

---

## 📁 Repository Structure

```
CiccioSoft.Sqlite/
├── CiccioSoft.Sqlite.Native/            # Low-Level Layer: SafeHandle wrappers, P/Invoke C-APIs, Zero-copy UTF-8
├── CiccioSoft.Sqlite/                   # Intermediate Layer: Thin object-oriented C# layer over native handles
├── CiccioSoft.Data.Sqlite/              # High-Level Layer: ADO.NET Provider (DbConnection, DbCommand, DbBatch, etc.)
├── Benchmark/                           # BenchmarkDotNet performance suites
│   ├── CiccioSoft.Sqlite.Native.Benchmark/  # Low-level P/Invoke and Span marshalling benchmarks
│   └── CiccioSoft.Data.Sqlite.Benchmark/    # Provider benchmarks against Microsoft.Data.Sqlite
├── Example/                             # Practical examples and usage scenarios
│   ├── CiccioSoft.Sqlite.Native.Example/    # Low-level API console sample
│   └── CiccioSoft.Sqlite.Native.WpfExample/ # Native WPF demo application
├── Tests/                               # Comprehensive xUnit test suites
│   ├── CiccioSoft.Sqlite.Native.Tests/      # P/Invoke and native handle lifecycle tests
│   ├── CiccioSoft.Sqlite.Tests/             # Intermediate wrapper tests
│   ├── CiccioSoft.Data.Sqlite.Tests/        # Core ADO.NET compliance tests (net10.0)
│   └── CiccioSoft.Data.Sqlite.Tests.Extra/  # Concurrency, async, and WAL stress tests
├── CiccioSoft.Sqlite.slnx               # Visual Studio solution file
├── LICENSE                              # MIT License
└── README.md                            # Project documentation
```

---

## 🚀 Quick Start

### Installation & Build

```bash
# Clone the repository
git clone https://github.com/FrancescoCrimi/CiccioSoft.Sqlite.git
cd CiccioSoft.Sqlite

# Build the solution
dotnet build -c Release
```

### Basic ADO.NET Usage

```csharp
using CiccioSoft.Data.Sqlite;

await using var connection = new SqliteConnection("Data Source=app.db;Default Timeout=30");
await connection.OpenAsync();

// Execute a command with parameters
await using var command = connection.CreateCommand();
command.CommandText = "SELECT id, username, email FROM users WHERE id = @id";
command.Parameters.AddWithValue("@id", 42);

await using var reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    int id = reader.GetInt32(0);
    string username = reader.GetString(1);
    string email = reader.GetString(2);
    Console.WriteLine($"User [{id}]: {username} <{email}>");
}
```

### Deadlock-Free Transactions

Transactions automatically use `BEGIN IMMEDIATE;` to acquire write locks upfront, eliminating runtime upgrade deadlocks:

```csharp
await using var transaction = connection.BeginTransaction();

await using var insertCmd = connection.CreateCommand();
insertCmd.Transaction = transaction;
insertCmd.CommandText = "INSERT INTO logs (message) VALUES (@msg)";
insertCmd.Parameters.AddWithValue("@msg", "Application initialized.");
await insertCmd.ExecuteNonQueryAsync();

await transaction.CommitAsync();
```

### Low-Level C-API Wrapper Usage

For scenarios demanding maximum throughput with direct UTF-8 spans:

```csharp
using CiccioSoft.Sqlite.Native;

// Open native connection via SafeHandle
using var connection = Connection.Open("app.db");

// Direct zero-allocation statement preparation and execution
ReadOnlySpan<byte> sql = "SELECT COUNT(*) FROM users"u8;
using var statement = connection.Prepare(sql);
if (statement.Step())
{
    long count = statement.GetColumnInt64(0);
    Console.WriteLine($"Total users: {count}");
}
```

---

## 🛠️ Technical Requirements

- **Runtime:** .NET 10.0+
- **Language:** C# 14
- **Native Dependency:** SQLite 3 (dynamically loaded or statically linked)

---

## 🛣️ Roadmap

- [x] Raw P/Invoke native bindings with custom `SafeHandle` implementations (`CiccioSoft.Sqlite.Native`)
- [x] Zero-allocation UTF-8 `ReadOnlySpan<byte>` APIs
- [x] Full `System.Data.Common` provider suite (`DbConnection`, `DbCommand`, `DbDataReader`, `DbTransaction`)
- [x] Built-in `DbBatch` support for multi-statement execution
- [x] Cooperative async model with native `sqlite3_interrupt` cancellation
- [x] Native WAL mode and `sqlite3_busy_timeout` queue management
- [x] Connection pooling support (`SqliteConnectionPool`)
- [ ] User-Defined Functions (UDF) & Aggregates (`CreateFunction`, `CreateAggregate`)
- [ ] Dedicated Entity Framework Core Provider package

---

## 🙏 References & Acknowledgements

- **[Microsoft.Data.Sqlite](https://github.com/dotnet/efcore/tree/main/src/Microsoft.Data.Sqlite.Core):** Inspiration for ADO.NET `DbBatch` handling and provider architecture under the MIT License.
- **[SourceGear.sqlite3](https://sqlite.sourcegear.com/):** Packaging references and SQLite native interop patterns.
- **[ClangSharp](https://github.com/dotnet/ClangSharp):** Tooling and patterns for C/C++ interop in .NET.

---

## 📄 License

This project is licensed under the MIT License.
