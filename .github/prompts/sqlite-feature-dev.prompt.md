---
name: sqlite-feature-dev
description: "Usa quando: aggiungere nuove feature ADO.NET, espandere funzionalità provider, implementare nuovi metodi SqliteCommand, aggiungere opzioni connessione, estendere statement cache, implementare nuovi tipi dati o modalità parameter binding."
type: prompt
---

# Feature SQLite: [Nome Feature]

## Panoramica Feature

**Cosa**: [Breve descrizione di cosa implementa questa feature]

**Perché**: [Problema che risolve o use case che abilita]

**Scope**: [Quali layer: Interop, Core, o entrambi]

## Requisiti

### Requisiti Funzionali
- [ ] Requisito 1
- [ ] Requisito 2
- [ ] Requisito 3

### Requisiti Non-Funzionali
- [ ] Thread-safe / async-safe
- [ ] Nessun breaking change su API public
- [ ] Documentato con commenti XML
- [ ] ~80%+ test coverage

## Piano Implementazione

### Step 1: Interop Layer (se necessario)
- [ ] Aggiungi dichiarazioni P/Invoke in [CiccioSoft.Data.Sqlite.Interop/Sqlite3.cs](../../CiccioSoft.Data.Sqlite.Interop/Sqlite3.cs)
- [ ] Aggiungi pattern SafeHandle per nuove risorse
- [ ] Aggiungi traduzione errori in [SqliteErrorHelper.cs](../../CiccioSoft.Data.Sqlite.Interop/SqliteErrorHelper.cs)

### Step 2: Core ADO.NET Layer
- [ ] Aggiungi property/metodo in [SqliteConnection.cs](../../Microsoft.Data.Sqlite.Core/SqliteConnection.cs) o class rilevante
- [ ] Aggiungi documentazione XML
- [ ] Implementa variante sia sync che async
- [ ] Supporta `CancellationToken` per metodi async

### Step 3: Testing
- [ ] Aggiungi unit test in [CiccioSoft.Data.Sqlite.Tests/](../../CiccioSoft.Data.Sqlite.Tests/)
- [ ] Aggiungi test async/concurrency in [CiccioSoft.Data.Sqlite.Tests.Extra/](../../CiccioSoft.Data.Sqlite.Tests.Extra/)
- [ ] Verifica thread-safety tramite scenari test concorrenti

### Step 4: Documentazione
- [ ] Aggiorna [README.md](../../README.md) se user-facing
- [ ] Aggiungi esempi codice
- [ ] Aggiorna copilot-instructions.md se nuovi pattern introdotti

## Checklist Stile Codice

- [ ] Statement `using` espliciti (no implicit usings)
- [ ] Nullable reference types abilitati (`T?`, `string?`)
- [ ] Commenti XML doc su tutti i member pubblici
- [ ] Intestazione MIT license su nuovi file sorgente
- [ ] Seguire convenzioni naming (PascalCase per API pubbliche)
- [ ] Usa pattern C# 12 moderni (switch expressions, null coalescing)

## Pattern Async/Concurrency

- [ ] Tutti i metodi async accettano `CancellationToken cancellationToken = default`
- [ ] Controlla `cancellationToken.ThrowIfCancellationRequested()` all'ingresso metodo
- [ ] Ritorna `Task.CompletedTask` (non `Task.Run`)
- [ ] Usa `sqlite3_interrupt()` nativo per operazioni lunghe
- [ ] Thread-safe tramite `SingleWriterCoordinator` o altra synchronizzazione

## Example Template (Copy for New Features)

```csharp
// Sync variant
public void MyNewMethod(string param)
{
    ObjectDisposedException.ThrowIf(_disposed, this);
    
    // Validate input
    if (string.IsNullOrEmpty(param))
        throw new ArgumentNullException(nameof(param));
    
    // Implement or delegate
    MyImplementation(param);
}

// Async variant
public Task MyNewMethodAsync(string param, CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    MyNewMethod(param);  // Call sync variant
    return Task.CompletedTask;
}
```

## Testing Template

```csharp
[Fact]
public void MyNewMethod_ProducesExpectedResult()
{
    using var connection = new SqliteConnection("Data Source=:memory:");
    connection.Open();
    
    // Arrange
    var result = connection.MyNewMethod("input");
    
    // Assert
    Assert.Equal(expected, result);
}

[Fact]
public async Task MyNewMethodAsync_RespectsUseCancellationToken()
{
    using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();
    
    var cts = new CancellationTokenSource();
    cts.CancelAfter(TimeSpan.FromMilliseconds(100));
    
    await Assert.ThrowsAsync<OperationCanceledException>(
        () => connection.MyNewMethodAsync("input", cts.Token)
    );
}
```

## Build & Verify

```bash
# Build
dotnet build CiccioSoft.Data.Sqlite.Repository.slnx

# Run tests
dotnet test CiccioSoft.Data.Sqlite.Repository.slnx

# Check coverage
dotnet test CiccioSoft.Data.Sqlite.Repository.slnx /p:CollectCoverage=true
```

## Riferimenti

- **Architettura**: [README.md](../../README.md) — Panoramica design two-layer
- **Pattern Async**: [AsyncConcurrencyTests.cs](../../CiccioSoft.Data.Sqlite.Tests.Extra/AsyncConcurrencyTests.cs)
- **P/Invoke**: [Sqlite3.cs](../../CiccioSoft.Data.Sqlite.Interop/Sqlite3.cs)
- **Lifecycle Connessione**: [SqliteConnection.cs](../../Microsoft.Data.Sqlite.Core/SqliteConnection.cs)
- **Esecuzione Comando**: [SqliteCommand.cs](../../Microsoft.Data.Sqlite.Core/SqliteCommand.cs)

---

## Note

[Aggiungi qui qualsiasi nota aggiuntiva, decisioni o preoccupazioni]
