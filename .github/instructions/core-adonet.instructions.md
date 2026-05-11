---
name: core-adonet-instructions
description: "Istruzioni file-scoped per Core ADO.NET Layer (Microsoft.Data.Sqlite.Core/). Si applicano a implementazioni connection, command, transaction e data reader."
applyTo: "**/Microsoft.Data.Sqlite.Core/**/*.cs"
---

# Istruzioni Core ADO.NET Layer

Questo file si applica a tutti i file sorgente C# in `Microsoft.Data.Sqlite.Core/`.

## Compliance Contratto ADO.NET

Implementa interfacce da `System.Data`:
- `DbConnection` — Lifecycle connessione e gestione stato
- `DbCommand` — Preparazione comando, parameter binding, execution
- `DbDataReader` — Iterazione result set e recupero valori
- `DbTransaction` — Controllo transazioni (BEGIN, COMMIT, ROLLBACK)
- `DbFactory` — Provider factory (pattern singleton)

## Lifecycle Connessione & Defaults

Tutte le connessioni devono applicare questi defaults su `Open()`:

```csharp
public override void Open()
{
    // 1. Foreign Keys ON (enforza referential integrity)
    ExecuteNonQuery("PRAGMA foreign_keys=ON");
    
    // 2. Journal Mode: WAL (file) o DELETE (in-memory)
    string mode = IsMemoryConnection ? "DELETE" : "WAL";
    ExecuteNonQuery($"PRAGMA journal_mode={mode}");
    
    // 3. Busy Timeout: 30 secondi default
    ExecuteNonQuery("PRAGMA busy_timeout=30000");
}
```

Questi possono essere overridati via connection string:
```
"Data Source=app.db;Foreign Keys=False;Journal Mode=DELETE;Busy Timeout=5000"
```

Alias supportati:
- `foreign_keys` → `Foreign Keys`
- `journal_mode` → `Journal Mode`
- `busy_timeout` → `Busy Timeout`

Referenza: [SqliteConnectionStringBuilder.cs](.github/../SqliteConnectionStringBuilder.cs)

## Statement Caching & Riutilizzo

Implementa statement cache su `SqliteCommand`:

```csharp
internal SqliteStatement? _cachedStatement;

public void Prepare()
{
    _cachedStatement ??= _connection.Prepare(CommandText);
}
```

Benefici:
- Evita ri-parsing di SQL
- Riduce allocazioni
- Migliora performance per comandi ripetuti

## Coordinamento Single-Writer

Assicuri accesso serializzato a SQLite via `SingleWriterCoordinator`:

```csharp
private readonly SingleWriterCoordinator _coordinator;

internal T ExecuteWithCoordination<T>(Func<T> operation)
{
    return _coordinator.Execute(operation);
}
```

Questo previene accesso concorrente al motore SQLite nativo (che non è thread-safe).

## Implementazione Async/Await

Tutti i metodi async pubblici devono:

1. Accettare `CancellationToken cancellationToken = default`
2. Controllare cancellazione all'entry: `cancellationToken.ThrowIfCancellationRequested()`
3. Usare true cooperative async (no `Task.Run`)
4. Ritornare `Task.CompletedTask` per implementazioni sincrone

```csharp
public override Task OpenAsync(CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    Open();
    return Task.CompletedTask;
}

public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    
    int result = ExecuteNonQuery();  // Synchronous implementation
    
    return result;
}
```

## Enforcement CommandTimeout

`SqliteCommand.CommandTimeout` (in secondi, 0 = no timeout):

```csharp
public int CommandTimeout { get; set; } = 30;

internal void ApplyTimeout(CancellationToken externalToken)
{
    if (CommandTimeout > 0)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        cts.CancelAfter(TimeSpan.FromSeconds(CommandTimeout));
        
        // Passa cts.Token alle operazioni interne
        // Su cancellazione, sqlite3_interrupt() viene chiamato
    }
}
```

Timeout applica a **scope completo comando**: preparazione + execution + result reading.

## Gestione Eccezioni

Lancia `SqliteException` per errori SQLite-specifici:

```csharp
try
{
    result = NativeSqlite3.sqlite3_prepare_v2(...);
    if (result != SqliteResult.Ok)
        throw SqliteErrorHelper.CreateException(_handle, result);
}
catch (SqliteException ex)
{
    // Extended error codes disponibili tramite ex.SqliteExtendedErrorCode
    throw;
}
```

Sempre preserva extended error codes per diagnostica.

## Connection Pooling

Pooling thread-safe tramite `SqliteConnectionPool`:

```csharp
private static readonly ConcurrentDictionary<string, PoolState> _pools = new();

internal class PoolState
{
    public SemaphoreSlim AvailableConnections { get; set; }
    public Stack<SqliteConnectionInternal> Stack { get; set; }
}
```

- Usa `Pooling=True` in connection string per abilitare
- Connessioni ritornate al pool vengono validate prima del riuso
- Connessioni leaked vengono pulite su timeout

## Nullable Reference Types

- Abilita `Nullable=enable`
- Usa `string?` per stringhe nullable
- Usa `T?` per value types nullable
- Valida parametri: `ArgumentNullException.ThrowIfNull(param)`

## Header File

Ogni file sorgente deve includere intestazione MIT license (vedi [interop-instructions](interop.instructions.md) per template).

## Pattern Testing

Scrivi test che verificano:
- Transizioni stato connessione (Closed → Open → Closed)
- Statement caching (prepared statement riutilizzati)
- Comportamento async/cancellation
- Connection pooling sotto carico
- Isolation level transazioni
- Parameter binding per tutti i tipi SQLite

Referenza: [SqliteCommandTest.cs](.github/../CiccioSoft.Data.Sqlite.Tests/SqliteCommandTest.cs), [AsyncConcurrencyTests.cs](.github/../CiccioSoft.Data.Sqlite.Tests.Extra/AsyncConcurrencyTests.cs)

## Examples to Follow

- [SqliteConnection.cs](.github/../SqliteConnection.cs) — Connection lifecycle, defaults, pooling
- [SqliteCommand.cs](.github/../SqliteCommand.cs) — Statement cache, parameter binding, execution
- [SqliteConnectionPool.cs](.github/../SqliteConnectionPool.cs) — Thread-safe pool
- [SqliteFactory.cs](.github/../SqliteFactory.cs) — Factory pattern (singleton)
