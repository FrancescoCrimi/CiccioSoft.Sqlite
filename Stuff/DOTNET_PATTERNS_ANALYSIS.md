# CiccioSoft.Sqlite — .NET Patterns Analysis

> A didactic SQLite ADO.NET provider for .NET 10, reimplementing the full ADO.NET surface with raw P/Invoke.

---

## Architecture Overview

```
CiccioSoft.Sqlite.Interop          ← Raw P/Invoke layer (unsafe, stackalloc, ArrayPool)
         │
         ▼
CiccioSoft.Data.Sqlite             ← ADO.NET abstraction layer (DbConnection, DbCommand, …)
         │
         ▼
CiccioSoft.Sqlite.Interop.Example  ← Console app using the raw Interop layer directly
```

---

## ✅ Patterns Well-Applied

| Pattern | Where | Notes |
|---|---|---|
| **SafeHandle** | `Sqlite3Handle`, `Sqlite3StmtHandle`, `Sqlite3BackupHandle` | Correct `SafeHandleZeroOrMinusOneIsInvalid` subclass |
| **IDisposable (Dispose pattern)** | All resource-owning types | `Dispose(bool disposing)` throughout |
| **Stackalloc + ArrayPool hybrid** | `Sqlite3.Open`, `Execute`, `Prepare`, `Sqlite3Stmt.BindText` | Threshold 1024 bytes → stackalloc, else `ArrayPool<byte>.Shared.Rent` |
| **ReadOnlySpan\<byte\> zero-copy BLOB** | `Sqlite3Stmt.GetBlob` | Native memory directly exposed; lifetime constraint documented |
| **SemaphoreSlim as gate** | `SqliteSession.Gate`, `SingleWriterCoordinator`, `SqliteConnectionPool` | Per-connection and per-writer-key serialization |
| **CancellationToken propagation** | `CommandExecutionScope`, `ReadAsync`, `CheckpointAsync` | Linked sources for timeout + external cancellation |
| **Native interrupt via cancellation** | `CommandExecutionScope` | `CancellationToken.Register(() => session.Native.Interrupt())` — elegant |
| **Switch expressions** | `SqliteTransaction`, `SqliteCommand`, `SqliteDataReader` | Idiomatic C# 8+ throughout |
| **FrozenDictionary** | `SqliteConnectionStringOption` static registry | .NET 8+ immutable lookup |
| **Primary constructors** | `SqliteConnectionStringOption` | C# 12 |
| **Nullable reference types** | All projects (`<Nullable>enable</Nullable>`) | `[AllowNull]` used where ADO.NET base class demands it |
| **Static factory method** | `SqliteDataReader.Create(...)` | Exception-safe initialization (dispose on failure in ctor) |
| **Modern guard clauses** | `Sqlite3.InitBackup` | `ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace` |
| **record type** | `UserRow` in example | Immutable DTO |

---

## 🔴 Bugs / High Priority

### 1. `SqliteConnectionPool.Return` — Semaphore Never Released (Deadlock Bug)

```csharp
// SqliteConnectionPool.cs
public static void Return(string connectionString, SqliteSession session)
{
    if (Pools.TryGetValue(connectionString, out PoolState? state))
    {
        state.Bag.Add(session);
        // BUG: state.Semaphore.Release() is MISSING
        // Threads waiting in Rent() via state.Semaphore.Wait() will deadlock
    }
}
```

**Fix:** call `state.Semaphore.Release()` after `state.Bag.Add(session)`.

---

### 2. `OpenAsync` Is Not Actually Async

```csharp
// SqliteConnection.cs
public override Task OpenAsync(CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    Open();                    // ← synchronous — blocks the thread
    return Task.CompletedTask;
}
```

Same applies to `CheckpointAsync`, `OptimizeAsync`, `CommitAsync`, `RollbackAsync`, `ExecuteReaderAsync`. These run synchronously but return `Task`, misleading callers using `await`.

**Options:**
- Use `ValueTask` and add XML doc noting they complete synchronously
- Move blocking I/O to a `Task.Run` wrapper (honest async)
- Document clearly in the README as a known intentional tradeoff

---

### 3. `SqliteConnectorFactory` — Stale MySqlConnector Copy-Paste

XML docs and CA1822 suppressions still reference `MySqlConnector`, `MySqlBatch`, `MySqlDataSource`. Additionally, `SqliteFactory` and `SqliteConnectorFactory` are two separate `DbProviderFactory` implementations — only one (`SqliteFactory`) is referenced by `SqliteConnection.DbProviderFactory`. The other should be removed.

---

## 🟡 Medium Priority / Design Improvements

### 4. `SqliteException` Should Override `DbException.ErrorCode`

```csharp
// SqliteException.cs — Missing override
public override int ErrorCode => SqliteErrorCode; // ← add this
```

ADO.NET convention: code inspecting `DbException.ErrorCode` (e.g., middleware, Polly retry policies) won't see the SQLite error code without this.

---

### 5. `IsValid()` Uses Exception for Control Flow + Expensive Health Check

```csharp
public bool IsValid()
{
    try { Native.Execute("SELECT 1"); return true; }
    catch { return false; }   // ← swallows ObjectDisposedException and everything else
}
```

- Executing SQL per pool-rent is expensive on hot paths
- The bare `catch {}` is too broad

**Fix:** check `_handle.IsInvalid` directly from the `SafeHandle`.

---

### 6. `DefaultTimeout` TODO Remains Unresolved

```csharp
//TODO: fix DefaultTimeout
public virtual int DefaultTimeout
{
    get => _defaultTimeout ?? _settings.DefaultTimeout;
    set => _defaultTimeout = value;  // ← doesn't update native busy timeout
}
```

Setting `DefaultTimeout` after `Open()` doesn't call `sqlite3_busy_timeout` on the live native handle. Either update the native handle on set, or document the limitation clearly.

---

### 7. Guard Clause Inconsistency

Some methods use modern helpers, others use manual checks:

```csharp
// Inconsistent — found in Sqlite3.GetTableColumnMetadata
ArgumentNullException.ThrowIfNull(tableName);
if (tableName.Length == 0)
    throw new ArgumentException("Table name cannot be empty.", nameof(tableName));

// Should be:
ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
```

Audit all guard clauses and standardize on the modern static helpers.

---

### 8. `UserRepository` Example — Resource Leak

```csharp
// UserRepository.cs
private readonly Sqlite3 _connection;
public UserRepository(string? nomeDaInserire)
{
    _connection = Sqlite3.Open("test.db");
    // No Dispose(), no IDisposable implementation
}
```

For an educational project, this teaches bad habits. `UserRepository` should implement `IDisposable` and dispose `_connection`.

---

### 9. `BackupDatabase` Stubs Throw `NotImplementedException`

```csharp
public virtual void BackupDatabase(SqliteConnection destination)
    => throw new NotImplementedException("Not Implemented");
```

The interop layer (`Sqlite3Backup`, `InitBackup`) has the full backup API ready. Either wire it up or remove these stubs from the public surface.

---

## 🟢 Lower Priority / Polish

### 10. Immutability Gaps

```csharp
// Should be IReadOnlyList<string> or FrozenSet<string> — never changes after init
public List<string> OptionNames { get; }

// Should be FrozenDictionary for consistency with rest of codebase
private static readonly Dictionary<Type, SqliteType> _sqliteTypeMapping = ...
```

---

### 11. `CommandExecutionScope` — Duplicated `Execute<T>` / `Execute(Action)` Logic

Both overloads share ~90% of their logic (CancellationToken registration, Gate.Wait, error handling). Refactor to a common private helper to eliminate divergence risk.

---

### 12. `BatchExecutionState` — Unnecessary Backing Fields

```csharp
// Before: manual backing fields with no logic
private int _sqlByteOffset;
public int SqlByteOffset { get => _sqlByteOffset; set => _sqlByteOffset = value; }

// After: simple auto-properties
public int SqlByteOffset { get; set; }
public int PreparedStatementIndex { get; set; }
```

---

### 13. `ResolveParameterIndex` — Allocates Unnecessarily

```csharp
// Before
string coreName = parameterName[0] is ('@' or ':' or '$')
    ? parameterName.Substring(1)   // allocates
    : parameterName;

// After — C# 8+ range operator
string coreName = parameterName[0] is ('@' or ':' or '$')
    ? parameterName[1..]           // no allocation
    : parameterName;
```

---

### 14. Empty `foreach` Loop is Surprising

```csharp
// SqliteCommand — hard to read at a glance
foreach ((Sqlite3Stmt _, int _) in PrepareAndEnumerateStatements(session))
{
    // intentionally empty — just populates the cache
}
```

Extract to a dedicated `WarmStatementCache(session)` method with a clear name and XML doc.

---

## Priority Summary

| # | Area | Priority | Type |
|---|---|---|---|
| 1 | Connection pool `Return` deadlock | 🔴 High | Bug |
| 2 | `OpenAsync` not truly async | 🔴 High | Design |
| 3 | `SqliteConnectorFactory` stale / duplicate | 🔴 High | Cleanup |
| 4 | `SqliteException.ErrorCode` missing override | 🟡 Medium | ADO.NET conformance |
| 5 | `IsValid()` expensive + broad catch | 🟡 Medium | Performance / correctness |
| 6 | `DefaultTimeout` TODO | 🟡 Medium | Correctness |
| 7 | Guard clause inconsistency | 🟡 Medium | Consistency |
| 8 | `UserRepository` resource leak | 🟡 Medium | Example quality |
| 9 | `BackupDatabase` not implemented | 🟡 Medium | API surface |
| 10 | Immutability gaps | 🟢 Low | Polish |
| 11 | `CommandExecutionScope` duplication | 🟢 Low | Maintainability |
| 12 | `BatchExecutionState` backing fields | 🟢 Low | Simplification |
| 13 | `Substring` vs `[1..]` | 🟢 Low | Performance |
| 14 | Empty `foreach` | 🟢 Low | Readability |

---

## Key Files

| File | Lines | Purpose |
|---|---|---|
| `CiccioSoft.Sqlite.Interop/Sqlite3.cs` | 773 | Raw interop wrapper |
| `CiccioSoft.Sqlite.Interop/Sqlite3Stmt.cs` | 641 | Statement wrapper |
| `CiccioSoft.Data.Sqlite/SqliteConnection.cs` | 634 | ADO.NET connection |
| `CiccioSoft.Data.Sqlite/SqliteCommand.cs` | 970 | ADO.NET command + scope |
| `CiccioSoft.Data.Sqlite/SqliteDataReader.cs` | 1292 | ADO.NET reader |
| `CiccioSoft.Data.Sqlite/SqliteConnectionPool.cs` | 174 | Pool with the `Return` bug |
| `CiccioSoft.Data.Sqlite/SqliteConnectionStringBuilder.cs` | 432 | Options + FrozenDictionary registry |
