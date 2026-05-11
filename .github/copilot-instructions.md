# CiccioSoft.Data.Sqlite — Linee Guida per AI Agent

Questo è un **provider SQLite educational di qualità production** per .NET 10 con forte enfasi su **pattern async/await** e **architettura two-layer** (raw P/Invoke + astrazioni OOP).

## Stile di Codice

- **Linguaggio**: C# 12+, .NET 10.0
- **Nullable**: Reference types abilitati (`Nullable=enable`) — usa `T?`, `string?`, operatori null-coalescing
- **Implicit usings**: Disabilitati (`ImplicitUsings=false`) — statement `using` espliciti richiesti
- **XML docs**: Richiesti su tutti gli API public e metodi interni complessi
- **Intestazione licenza**: Commento intestazione MIT license su ogni file sorgente
- **Pattern matching**: Usa pattern C# 12 moderni (switch expressions, null coalescing, pattern guards)
- **File di esempio**: 
  - [SqliteConnection.cs](../Microsoft.Data.Sqlite.Core/SqliteConnection.cs) — lifecycle della connessione & defaults
  - [SqliteCommand.cs](../Microsoft.Data.Sqlite.Core/SqliteCommand.cs) — esecuzione comandi & statement caching
  - [Sqlite3.cs](../CiccioSoft.Data.Sqlite.Interop/Sqlite3.cs) — pattern P/Invoke & utilizzo SafeHandle

## Architettura

**Progettazione two-layer:**

1. **Interop Layer** (`CiccioSoft.Data.Sqlite.Interop/`)
   - Wrapper FFI P/Invoke a basso livello per SQLite nativo
   - Raw bindings esposti tramite classe `Sqlite3` per casi d'uso avanzati
   - Efficienza memoria: `stackalloc`, `ArrayPool<T>`, `Span<T>`
   - Gestione handle thread-safe tramite eredità `SafeHandle`
   - Vedi [CiccioSoft.Data.Sqlite.Interop/README.md](../CiccioSoft.Data.Sqlite.Interop/README.md)

2. **OOP Layer** (`Microsoft.Data.Sqlite.Core/`, `CiccioSoft.Data.Sqlite/`)
   - Astrazioni C# idiomatiche seguendo **interfacce ADO.NET** (`DbConnection`, `DbCommand`, `DbDataReader`, etc.)
   - Coordinamento single-writer tramite `SingleWriterCoordinator` per serializzare accesso SQLite nativo
   - Connection pooling con `ConcurrentDictionary` thread-safe e `SemaphoreSlim`
   - Statement caching su `SqliteCommand` per riutilizzo prepared statement
   - Supporto transazioni con interfaccia `DbTransaction`

**Pattern di Design Chiave:**
- **Factory** (singleton): `SqliteFactory.cs`
- **Connection Pool** (thread-safe): `SqliteConnectionPool.cs`, `SqliteConnectionPoolGroup.cs`
- **Single-Writer Coordinator**: Assicura accesso serializzato a SQLite al confine interop
- **Statement Cache** (per-command): Prepared statement riutilizzati nel lifetime del comando

Vedi [README.md](../README.md) per panoramica architettura completa.

## Build e Test

**Build:**
```bash
dotnet build CiccioSoft.Data.Sqlite.slnx
```

**Unit Test:**
```bash
dotnet test CiccioSoft.Data.Sqlite.slnx
```

**Test Framework**: XUnit 2.9+ con supporto theory e coverlet coverage  
**Target Framework**: .NET 10.0  
**CI/CD**: GitHub Actions (Ubuntu 24.04, Windows 2025, macOS 15)

**Progetti Test:**
- `CiccioSoft.Data.Sqlite.Tests/` — Core ADO.NET API contracts e behavior
- `CiccioSoft.Data.Sqlite.Tests.Extra/` — Test async/concurrency, WAL semantics, parity

Vedi [CiccioSoft.Data.Sqlite.Tests.Extra/README.md](../CiccioSoft.Data.Sqlite.Tests.Extra/README.md) per esempi test async/concurrency.

## Convenzioni

### Pattern Async/Await (Core del Progetto)

Il provider implementa **async cooperativo vero** (no wrapper `Task.Run`) con supporto interrupt nativo:

```csharp
// ✅ Corretto: Consapevole della cancellazione, non-blocking
public override Task OpenAsync(CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    Open();  // Operazione sincrona, ma superficie API async-compliant
    return Task.CompletedTask;
}

// ✅ Corretto: Consapevole dell'interrupt (per operazioni lunghe)
public virtual Task CheckpointAsync(SqliteWalCheckpointMode mode, CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    Checkpoint(mode, cancellationToken);  // sqlite3_interrupt() nativo su cancellazione
    return Task.CompletedTask;
}
```

**Punti Chiave:**
- I metodi async devono accettare `CancellationToken` come ultimo parametro
- Sempre controllare `cancellationToken.ThrowIfCancellationRequested()` all'ingresso del metodo
- Usa `sqlite3_interrupt()` nativo per operazioni lunghe per supportare cancellazione mid-execution
- Per interni sincroni, ritorna `Task.CompletedTask` piuttosto che wrappare in `Task.Run()`
- Rispetta `CommandTimeout` (0 = senza timeout, >0 = secondi) tramite meccanismo interrupt

**Riferimento:** [SqliteCommand.cs](../Microsoft.Data.Sqlite.Core/SqliteCommand.cs#L190), [AsyncConcurrencyTests.cs](../CiccioSoft.Data.Sqlite.Tests.Extra/AsyncConcurrencyTests.cs)

### Gestione Eccezioni

Usa `SqliteException` per errori SQLite nativi con extended error codes:

```csharp
public class SqliteException : DbException
{
    public int SqliteErrorCode { get; }
    public int SqliteExtendedErrorCode { get; }
}
```

Traduci native error codes tramite `SqliteErrorHelper.CreateException()`. Sempre preserva il codice errore SQLite originale per diagnostica.

**Riferimento:** [SqliteException.cs](../Microsoft.Data.Sqlite.Core/SqliteException.cs), [SqliteErrorHelper.cs](../CiccioSoft.Data.Sqlite.Interop/SqliteErrorHelper.cs)

### Connection String & Defaults

**Default opinioni su `Open()`:**
- `Foreign Keys = ON` (referential integrity enforced)
- `Journal Mode = WAL` (file-backed) o `DELETE` (in-memory)
- `Busy Timeout = 30000` ms (30 secondi)

**Override tramite connection string** (alias compatibili Microsoft.Data.Sqlite):
```csharp
"Data Source=app.db;Foreign Keys=False;Journal Mode=DELETE;Busy Timeout=5000"
// Alias supportati anche:
"Data Source=app.db;foreign_keys=false;journal_mode=DELETE;busy_timeout=5000"
```

**Riferimento:** [README.md — ADO.NET Provider Deep Dive](../README.md#-adonet-provider-deep-dive-defaults--async), [SqliteConnectionStringBuilder.cs](../Microsoft.Data.Sqlite.Core/SqliteConnectionStringBuilder.cs)

### Pattern P/Invoke (Interop Layer)

- **Eredità SafeHandle** per cleanup risorse pulito: `Sqlite3Handle : SafeHandleZeroOrMinusOneIsInvalid`
- **Stack allocation** per conversioni UTF8 a breve vita: `stackalloc byte[...]`
- **ArrayPool<T>** per buffer più grandi per ridurre GC pressure
- **Span<T>** per operazioni pointer sicure senza `unsafe` quando possibile
- **Esplicito `[DllImport]`** con `CallingConvention = CallingConvention.Cdecl`
- **Error code checks** ad ogni confine native call

**Riferimento:** [Sqlite3.cs](../CiccioSoft.Data.Sqlite.Interop/Sqlite3.cs), [NativeSqlite3.cs](../CiccioSoft.Data.Sqlite.Interop/Sqlite3.cs) (dichiarazioni P/Invoke)

### Threading & Synchronization

- **Thread safety basata on lock** per gestione connessioni: `private readonly object _syncRoot = new();`
- **ConcurrentDictionary** per gestione pool lock-free
- **SemaphoreSlim** per operazioni semaforo async-aware nei connection pool
- **Garanzia single-writer** al confine API SQLite (coordinato tramite `SingleWriterCoordinator`)

**Riferimento:** [SqliteConnection.cs](../Microsoft.Data.Sqlite.Core/SqliteConnection.cs) (lifecycle connessione), [SqliteConnectionPool.cs](../Microsoft.Data.Sqlite.Core/SqliteConnectionPool.cs)

### Pattern di Test

- **Fact** per semplici unit test
- **Theory + InlineData** per test parametrizzati
- **IDisposable** per cleanup risorse di test
- **Assertion xUnit** (`Assert.Equal`, `Assert.Throws`, `Assert.True`, etc.)
- **Classi di test separate** per ADO.NET contracts (`*Test.cs`) vs. behavior/semantics (`*Tests.cs`)

**Riferimento:** [SqliteConnectionStringBuilderTest.cs](../CiccioSoft.Data.Sqlite.Tests/SqliteConnectionStringBuilderTest.cs), [AsyncConcurrencyTests.cs](../CiccioSoft.Data.Sqlite.Tests.Extra/AsyncConcurrencyTests.cs)

## File Chiave da Consultare

| File | Scopo |
|------|-------|
| [README.md](../README.md) | Panoramica progetto, key features, architettura |
| [CiccioSoft.Data.Sqlite.Interop/README.md](../CiccioSoft.Data.Sqlite.Interop/README.md) | Guida P/Invoke binding layer |
| [Microsoft.Data.Sqlite.Core/SqliteConnection.cs](../Microsoft.Data.Sqlite.Core/SqliteConnection.cs) | Defaults connessione, pooling, transaction lifecycle |
| [Microsoft.Data.Sqlite.Core/SqliteCommand.cs](../Microsoft.Data.Sqlite.Core/SqliteCommand.cs) | Statement caching, parameter binding, execution |
| [Microsoft.Data.Sqlite.Core/SqliteConnectionPool.cs](../Microsoft.Data.Sqlite.Core/SqliteConnectionPool.cs) | Implementazione pool thread-safe |
| [CiccioSoft.Data.Sqlite.Interop/Sqlite3.cs](../CiccioSoft.Data.Sqlite.Interop/Sqlite3.cs) | Pattern SafeHandle, utilizzo P/Invoke |
| [CiccioSoft.Data.Sqlite.Tests.Extra/AsyncConcurrencyTests.cs](../CiccioSoft.Data.Sqlite.Tests.Extra/AsyncConcurrencyTests.cs) | Esempi async/cancellation/WAL |

## Risorse Aggiuntive

- **Spec ADO.NET**: System.Data namespace contracts (DbConnection, DbCommand, DbDataReader, DbTransaction)
- **Documentazione SQLite**: [https://www.sqlite.org/capi3ref.html](https://www.sqlite.org/capi3ref.html)
- **Best practices P/Invoke**: [Microsoft P/Invoke docs](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)
- **WAL journaling**: [SQLite WAL mode](https://www.sqlite.org/wal.html)
