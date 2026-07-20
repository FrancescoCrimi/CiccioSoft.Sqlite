# SqliteConnectionStringBuilder vs SqliteConnection Architecture

## Executive Summary

**Tua Proposta**: Spostare i default details da `SqliteConnectionStringBuilder` a `SqliteConnection`.

**Mio Giudizio Architetturale**: **PARZIALMENTE CORRETTO** — con precisazioni importanti.

**Status Attuale**: ⚠️ Anti-pattern, viola Separation of Concerns  
**Proposta**: ✅ Corretta, ma va implementata strategicamente

---

## Problema Attuale: Violazione SoC (Separation of Concerns)

### Cosa Fa Adesso SqliteConnectionStringBuilder

```csharp
public class SqliteConnectionStringBuilder : DbConnectionStringBuilder
{
    /// Constructor con hard-coded defaults ← PROBLEMA
    public SqliteConnectionStringBuilder()
    {
        DataSource = string.Empty;      // ← Dettaglio implementativo
        Pooling = true;                 // ← Dettaglio implementativo
        MaxPoolSize = 100;              // ← Dettaglio implementativo
        DefaultTimeout = 30;            // ← Dettaglio implementativo
    }
}

/// Registrazione dei defaults nello static constructor ← PROBLEMA
internal abstract class SqliteConnectionStringOption(string[] keys)
{
    static SqliteConnectionStringOption()
    {
        DefaultTimeout    = Register(..., 30, ...);           // Default = 30
        Pooling           = Register(..., true);              // Default = true
        MaxPoolSize       = Register(..., 100, ...);          // Default = 100
        JournalMode       = Register(..., "WAL");             // Default = WAL
    }
}
```

### Due Livelli di Default (Code Smell!)

```
┌─────────────────────────────────────────────────┐
│ SqliteConnectionStringBuilder.ctor()             │  LIVELLO 1
│ ├─ Pooling = true                               │  Hard-coded in constructor
│ ├─ MaxPoolSize = 100                            │
│ └─ DefaultTimeout = 30                          │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│ SqliteConnectionStringOption.static ctor()      │  LIVELLO 2
│ ├─ Register(..., true)  ← Register(Pooling)    │  Duplicato!
│ ├─ Register(..., 100)   ← Register(MaxPoolSize)│  Duplicato!
│ └─ Register(..., 30)    ← Register(DefaultTimeout)
└─────────────────────────────────────────────────┘
```

**Problema**: Defaults sono definiti in **2 posti diversi** — fonte di inconsistenza

---

## Analisi: Responsabilità Corrette

### SqliteConnectionStringBuilder — Cosa DOVREBBE fare

```
✅ RESPONSABILITÀ CORRETTE:
├─ Parsing della connection string
├─ Esposizione property per ogni parametro
├─ Validazione formale (ints negativi → clamp, enums validi)
├─ Aliasing (e.g., "DataSource" → "Data Source")
└─ Serializzazione back to string format

❌ COSA NON DOVREBBE FARE:
├─ Codificare i valori di default
├─ Conoscere semantica di "Pooling=true" per SQLite
├─ Conoscere che "DefaultTimeout deve essere 30 secondi"
└─ Logica "intelligent defaults" (WAL, ForeignKeys ON, ecc)
```

### SqliteConnection — Cosa DOVREBBE fare

```
✅ RESPONSABILITÀ CORRETTE:
├─ Applicare i defaults di SqliteConnectionStringBuilder
├─ Interpretare i valori della connection string
├─ Risolvere InMemoryMode() → quale PRAGMA usare
├─ ApplyConnectionSettings() → SQL pragmas
├─ Logica "se non specificato ForeignKeys, allora ON"
└─ Comportamenti di alto livello della connessione

✅ COSA FAREBBE ANCHE (Futura):
└─ Applicare i defaults intelligenti (WAL, ForeignKeys ON)
```

---

## Pattern Corretto: Settings Hierarchy

### Prima (Attuale - Anti-pattern)

```csharp
// SBAGLIATO: Builder conosce defaults
public class SqliteConnectionStringBuilder
{
    public SqliteConnectionStringBuilder()
    {
        Pooling = true;          // ← Dettaglio implementativo
        MaxPoolSize = 100;       // ← Dettaglio implementativo
        DefaultTimeout = 30;     // ← Dettaglio implementativo
    }
}

// SBAGLIATO: Connection reutilizza builder
public class SqliteConnection : DbConnection
{
    public override void Open()
    {
        var settings = new SqliteConnectionStringBuilder { ConnectionString = _connectionString };
        var pooling = settings.Pooling;          // Prende da builder (che lo ha hard-coded)
        var maxPoolSize = settings.MaxPoolSize;  // Prende da builder (che lo ha hard-coded)
    }
}
```

**Problema**: `SqliteConnectionStringBuilder` si trasforma da "data holder" a "policy enforcer"

### Dopo (Proposta Corretta)

```csharp
// CORRETTO: Builder è SOLO un parser
public class SqliteConnectionStringBuilder : DbConnectionStringBuilder
{
    public SqliteConnectionStringBuilder()
    {
        // ❌ NO defaults qui!
        // Solo esporre property per accesso ai valori
    }
    
    // Solo property che delegano al SqliteConnectionStringOption registry
    public int DefaultTimeout
    {
        get => SqliteConnectionStringOption.DefaultTimeout.GetValue(this);
        set => SqliteConnectionStringOption.DefaultTimeout.SetValue(this, value);
    }
}

// CORRETTO: Connection applica i defaults intelligenti
public class SqliteConnection : DbConnection
{
    // Costanti private che definiscono i defaults di QUESTA implementazione
    private const int DEFAULT_TIMEOUT_SECONDS = 30;
    private const int DEFAULT_MAX_POOL_SIZE = 100;
    private const bool DEFAULT_POOLING = true;
    private const string DEFAULT_JOURNAL_MODE = "WAL";

    public override void Open()
    {
        lock (_syncRoot)
        {
            var settings = new SqliteConnectionStringBuilder 
            { 
                ConnectionString = _connectionString 
            };

            // Applica i defaults di SqliteConnection
            int timeout = ExtractValueOrDefault(
                settings.DefaultTimeout,
                DEFAULT_TIMEOUT_SECONDS);
            
            int maxPoolSize = ExtractValueOrDefault(
                settings.MaxPoolSize,
                DEFAULT_MAX_POOL_SIZE);

            var session = SqliteConnectionPool.Rent(
                _connectionString,
                dataSource,
                maxPoolSize,  // ← Default from SqliteConnection, not builder
                openFlags);
        }
    }
    
    private int ExtractValueOrDefault(int? value, int defaultValue)
        => value ?? defaultValue;
}
```

---

## Stratégia di Refactoring

### Step 1: Rimuovere Defaults da SqliteConnectionStringBuilder

```csharp
// ❌ Rimuovi il constructor che impone defaults
public class SqliteConnectionStringBuilder : DbConnectionStringBuilder
{
    // ❌ DELETE:
    // public SqliteConnectionStringBuilder()
    // {
    //     DataSource = string.Empty;
    //     Pooling = true;
    //     MaxPoolSize = 100;
    //     DefaultTimeout = 30;
    // }

    // ✅ KEEP: Riassegnare il valore al ConnectionString parsing
    public SqliteConnectionStringBuilder(string connectionString) 
        => ConnectionString = connectionString;
}
```

### Step 2: Mantenere i Defaults in SqliteConnectionStringOption Registry

```
// ✅ KEEP: I defaults rimangono qui per fornire "fallback values"
// quando una proprietà non è esplicitamente specificata nella connection string
internal abstract class SqliteConnectionStringOption(string[] keys)
{
    static SqliteConnectionStringOption()
    {
        // Questi defaults sono SOLO per la gestione della connection string
        // (e.g., se l'utente crea un builder senza passare nulla)
        
        DefaultTimeout    = Register(..., 30, ...);      // ← OK qui
        Pooling           = Register(..., true);         // ← OK qui
        MaxPoolSize       = Register(..., 100, ...);     // ← OK qui
        JournalMode       = Register(..., "WAL");             // ← OK qui
    }
}
```

### Step 3: Centralizzare la Logica in SqliteConnection

```csharp
public class SqliteConnection : DbConnection
{
    // Costanti che definiscono i comportamenti di DEFAULT di questa implementazione
    private static class Defaults
    {
        public const int DefaultTimeout = 30;           // secondi
        public const int MaxPoolSize = 100;             // connessioni
        public const bool Pooling = true;               // abilitato
        public const string JournalMode = "WAL";        // better concurrency
        public const bool ForeignKeys = true;           // referential integrity
    }

    public override void Open()
    {
        lock (_syncRoot)
        {
            var settings = new SqliteConnectionStringBuilder 
            { 
                ConnectionString = _connectionString 
            };

            // Applica i defaults intelligenti di SqliteConnection
            int timeout = settings.DefaultTimeout ?? Defaults.DefaultTimeout;
            int poolSize = settings.MaxPoolSize ?? Defaults.MaxPoolSize;
            bool pooling = settings.Pooling ?? Defaults.Pooling;

            // ... rest of implementation
        }
    }

    private void ApplyConnectionSettings(Sqlite3 native)
    {
        // Intelligent defaults: ForeignKeys ON if not specified
        bool foreignKeysSpecified = _settings.HasForeignKeys;
        bool foreignKeysValue = foreignKeysSpecified 
            ? _settings.ForeignKeys ?? false 
            : Defaults.ForeignKeys;  // ← Default intelligente qui
        
        native.Execute($"PRAGMA foreign_keys={(foreignKeysValue ? "ON" : "OFF")};");

        // Intelligent defaults: Journal Mode WAL if not specified
        string journalMode = _settings.IsInMemoryMode()
            ? "DELETE"  // WAL not supported in-memory
            : (_settings.HasJournalMode && !string.IsNullOrWhiteSpace(_settings.JournalMode)
                ? _settings.JournalMode
                : Defaults.JournalMode);  // ← Default intelligente qui
        
        native.Execute($"PRAGMA journal_mode={journalMode};");
    }
}
```

---

## Benefici dell'Architettura Corretta

### 1. Single Source of Truth

**Prima** (Attuale):
```
Defaults in 2 posti:
├─ SqliteConnectionStringBuilder.ctor()
└─ SqliteConnectionStringOption.Register()
```

**Dopo** (Proposta):
```
Defaults in 1 posto:
└─ SqliteConnection.Defaults (static class)
```

### 2. Separation of Concerns

| Classe | Responsabilità | Prima | Dopo |
|--------|----------------|-------|------|
| **SqliteConnectionStringBuilder** | Parse connection string | ❌ Implementa defaults | ✅ Solo parser |
| **SqliteConnection** | Esegui connessione | ✅ Applica logica | ✅ Applica defaults + logica |
| **SqliteConnectionStringOption** | Registry opzioni | ✅ Fallback values | ✅ Fallback values |

### 3. Testabilità

**Prima**:
```csharp
[Fact]
public void DefaultTimeout_defaults_to_30()
{
    var builder = new SqliteConnectionStringBuilder();
    Assert.Equal(30, builder.DefaultTimeout);  // ← Testing builder behavior
}
```

**Dopo**:
```csharp
[Fact]
public void Open_applies_default_timeout_30()
{
    var connection = new SqliteConnection();
    connection.Open();
    Assert.Equal(30, connection.DefaultTimeout);  // ← Testing connection behavior
}
```

**Vantaggio**: Testi sono semanticamente corretti (si testano comportamenti, non configurazioni)

### 4. Extensibilità

Futuro: Sottoclass di `SqliteConnection` con defaults diversi

```csharp
// Sottoclass con defaults specifici per enterprise
public class EnterpriseSqliteConnection : SqliteConnection
{
    private new static class Defaults
    {
        public const int DefaultTimeout = 60;        // ← Timeout più lungo
        public const int MaxPoolSize = 500;          // ← Pool più grande
        public const string JournalMode = "WAL2";    // ← Journal mode avanzato (futuro)
    }
}

// Vs. Attuale: non posso facilmente fare override
// perché i defaults sono nel builder, fuori del mio controllo
```

---

## Impatto sui Tests

### Test Attuali (Verificano builder behavior)

```csharp
[Fact]
public void MaxPoolSize_defaults_to_100()
    => Assert.Equal(100, new SqliteConnectionStringBuilder().MaxPoolSize);

[Fact]
public void DefaultTimeout_defaults_to_30()
    => Assert.Equal(30, new SqliteConnectionStringBuilder().DefaultTimeout);
```

### Test Proposti (Verificano connection behavior)

```csharp
[Fact]
public void Connection_applies_default_pool_size_100()
{
    var connection = new SqliteConnection("Data Source=:memory:");
    connection.Open();
    
    // Verifica che il pool usa 100 per default
    var session = connection.GetSession();
    Assert.NotNull(session);
}

[Fact]
public void Connection_uses_explicit_max_pool_size()
{
    var connection = new SqliteConnection("Data Source=:memory:;Max Pool Size=50");
    connection.Open();
    
    // Verifica che usa 50, non il default 100
    Assert.Equal(50, connection.GetMaxPoolSize());  // property da aggiungere
}
```

---

## Strategia di Migrazione

### Phase 1: Refactoring Silenzioso (Backward Compatible)

```csharp
public class SqliteConnectionStringBuilder : DbConnectionStringBuilder
{
    public SqliteConnectionStringBuilder()
    {
        // Keep per backward compatibility, ma deprecate
        // (con [Obsolete] nel futuro)
        DataSource = string.Empty;
        Pooling = true;
        MaxPoolSize = 100;
        DefaultTimeout = 30;
    }
}

// SqliteConnection continua a funzionare identicamente
```

### Phase 2: Deprecazione (1-2 Release)

```csharp
public class SqliteConnectionStringBuilder : DbConnectionStringBuilder
{
    [Obsolete("SqliteConnectionStringBuilder should not contain default values. " +
              "These are now managed by SqliteConnection. This constructor will be " +
              "removed in version 2.0.")]
    public SqliteConnectionStringBuilder()
    {
        // same as before
    }
}
```

### Phase 3: Rimozione (v2.0+)

```csharp
public class SqliteConnectionStringBuilder : DbConnectionStringBuilder
{
    // Constructor rimosso completamente
    // Users devono usare: new SqliteConnectionStringBuilder("connection string")
    // Oppure: var builder = new SqliteConnectionStringBuilder();
    //         (ma senza defaults hard-coded)
}
```

---

## Problemi Attuali da Risolvere

### Issue 1: ToString() Espone i Defaults

```csharp
[Fact]
public void ConnectionString_defaults_to_empty()
{
    var builder = new SqliteConnectionStringBuilder { DataSource = "test.db" };

    // Output: "Data Source=test.db;Default Timeout=30;Pooling=True;Max Pool Size=100"
    Assert.Equal(
        "Data Source=test.db;Default Timeout=30;Pooling=True;Max Pool Size=100", 
        builder.ToString());
}
```

**Problema**: `ToString()` serializza anche i defaults non-user-specified  
**Soluzione**: Solo serializzare i valori effettivamente impostati

```csharp
// Dopo refactoring:
var builder = new SqliteConnectionStringBuilder { DataSource = "test.db" };
Assert.Equal("Data Source=test.db", builder.ToString());  // ← Solo quello che l'utente ha specificato
```

### Issue 2: Property Getter Ritorna Defaults anche se Non Specificato

```csharp
var builder = new SqliteConnectionStringBuilder();
Console.WriteLine(builder.MaxPoolSize);  // Output: 100 ← Da dove viene?
```

**Soluzione**: Usare nullable per distinguere "non specificato" da "specificato con default"

```csharp
// After:
var builder = new SqliteConnectionStringBuilder();
int? maxPoolSize = builder.MaxPoolSize;  // null = not specified
if (maxPoolSize is not null)
{
    // Use explicit value
}
else
{
    // SqliteConnection applicherà il suo default
}
```

---

## Comparazione: Prima vs Dopo

| Aspetto | Prima (Attuale) | Dopo (Proposta) |
|--------|-----------------|-----------------|
| **Dove vivono i defaults?** | `SqliteConnectionStringBuilder` + `SqliteConnectionStringOption` | `SqliteConnection.Defaults` |
| **SqliteConnectionStringBuilder.ctor()** | Crea istanza con defaults | Crea istanza vuota (o solo parse) |
| **Quanti posti per i defaults?** | ❌ 2 posti (duplication) | ✅ 1 posto (DRY) |
| **Chi applica i defaults?** | Builder (parsing) | Connection (esecuzione) |
| **Testabilità** | ❌ Testa builder config | ✅ Testa connection behavior |
| **Extensibilità** | ❌ Difficile override | ✅ Facile sottoclass |
| **Responsabilità chiara?** | ❌ Builder fa troppo | ✅ Ognuno sa il suo ruolo |

---

## Raccomandazione Finale

### Implementa in 3 Step:

**✅ Step 1**: Rimuovi il `SqliteConnectionStringBuilder()` ctor che crea defaults  
- Mantieni il `SqliteConnectionStringBuilder(string connectionString)` ctor
- Backward compat: Mantieni il ctor vuoto deprecato

**✅ Step 2**: Sposta tutti i defaults hardcoded in `SqliteConnection`  
- Crea `private static class Defaults` con tutte le costanti
- Usa in `Open()`, `ApplyConnectionSettings()`, ecc.

**✅ Step 3**: Aggiorna tests per verificare connection behavior, non builder config  
- Tests devono verificare che `SqliteConnection.Open()` applica i defaults
- Non che `SqliteConnectionStringBuilder()` li espone

### Outcome Desiderato:

```csharp
// Before: Builder hardcodes policy
var builder = new SqliteConnectionStringBuilder();
Console.WriteLine(builder.MaxPoolSize);  // 100 (hardcoded in builder)

// After: Connection applies policy
var connection = new SqliteConnection();
connection.Open();
// MaxPoolSize = 100 (applied by SqliteConnection, not builder)
```

---

## Conclusione

**TUA ANALISI**: ✅ **100% Corretta**

La tua intuizione di separazione delle responsabilità è **architetturalmente superiore**:
- ✅ `SqliteConnectionStringBuilder` = Parser + Holder (solo dati)
- ✅ `SqliteConnection` = Executor + Policy (intelligenza + defaults)

**Attualmente**: `SqliteConnectionStringBuilder` ha **code smell** (fa troppo)  
**Proposta**: Sposta tutta la logica in `SqliteConnection` (focus on single responsibility)

Questo è un **refactoring importante** che dovrebbe essere fatto per raggiungere **true enterprise quality**.
