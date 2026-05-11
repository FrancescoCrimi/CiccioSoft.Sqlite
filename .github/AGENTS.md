# CiccioSoft.Data.Sqlite — AI Agent & Customizzazioni

Guida di riferimento rapido per gli AI agent, prompt e customizzazioni disponibili in questo workspace.

## Panoramica

Questo workspace contiene agent specializzati e istruzioni che guidano gli AI coding assistant attraverso lo sviluppo CiccioSoft.Data.Sqlite con conoscenza profonda di:
- **Architettura provider SQLite two-layer** (Interop + OOP)
- **Pattern async vero** con cancellazione
- **Compliance ADO.NET di qualità production**
- **Binding P/Invoke ad alte performance**
- **Pattern concorrenti WAL journaling**

## Istruzioni a Livello Workspace

**File**: `.github/copilot-instructions.md`

Applicate a tutte le richieste di codice. Copre:
- Stile di codice (C# 12+, nullable reference types, XML docs)
- Architettura (progettazione two-layer, componenti chiave)
- Comandi build/test
- Pattern async/await (true cooperative async, no Task.Run)
- Gestione eccezioni (SqliteException con extended error codes)
- Connection string defaults (Foreign Keys, Journal Mode, Busy Timeout)
- Pattern P/Invoke (SafeHandle, memory efficiency)
- Scelte thread & synchronization
- Pattern di test (XUnit, Fact/Theory)

---

## Agent Specializzati

Usali digitando `/run-agent` e selezionandoli dalla lista, o referenziandoli direttamente in chat:

### 1. SQLite Testing Agent 🧪

**File**: `.github/agents/sqlite-testing.agent.md`

**Quando usare**: Scrivere/debuggare test XUnit, analizzare comportamento async/concurrency, migliorare coverage

**Expertise**:
- Pattern test XUnit (Fact, Theory, InlineData)
- Test async con CancellationToken
- Scenari test concorrenti (WAL, connection pooling)
- Cleanup risorse (IDisposable)
- Diagnosi fallimenti test e flakiness

**Richieste di esempio**:
- "Scrivi un test async per il handling del timeout della connessione"
- "Debugga perché questo test di concurrency è flaky"
- "Aggiungi test parametrici per tutti i PRAGMA defaults"

**File di riferimento**:
- [AsyncConcurrencyTests.cs](../CiccioSoft.Data.Sqlite.Tests.Extra/AsyncConcurrencyTests.cs)
- [SqliteParameterBindingParityTests.cs](../CiccioSoft.Data.Sqlite.Tests.Extra/SqliteParameterBindingParityTests.cs)

---

### 2. SQLite Interop Agent ⚙️

**File**: `.github/agents/sqlite-interop.agent.md`

**Quando usare**: Lavoro P/Invoke, pattern SafeHandle, ottimizzazione memoria, codice platform-specific

**Expertise**:
- Dichiarazioni P/Invoke e binding FFI
- Eredità SafeHandle e cleanup
- Gestione memoria (stackalloc, ArrayPool, Span<T>)
- Traduzione errori e extended error codes
- Gestione platform-specific (Windows/Linux/macOS)

**Richieste di esempio**:
- "Aggiungi binding P/Invoke per sqlite3_wal_checkpoint_v2"
- "Ottimizza questa conversione UTF8 usando stackalloc"
- "Correggi una memory leak nel statement wrapper"

**File di riferimento**:
- [Sqlite3.cs](../CiccioSoft.Data.Sqlite.Interop/Sqlite3.cs)
- [SqliteErrorHelper.cs](../CiccioSoft.Data.Sqlite.Interop/SqliteErrorHelper.cs)

---

### 3. SQLite Async/Concurrency Agent ⚡

**File**: `.github/agents/sqlite-async.agent.md`

**Quando usare**: Implementare metodi async, debuggare timeout, testare concorrenza WAL

**Expertise**:
- True cooperative async (no wrapper Task.Run)
- Propagazione cancellation token e sqlite3_interrupt()
- Enforcement CommandTimeout
- Pattern Single-Writer Coordinator
- Scenari concorrenti WAL
- Connection pooling sotto carico async

**Richieste di esempio**:
- "Implementa CheckpointAsync con supporto per cancellazione"
- "Debugga perché CommandTimeout non viene enforced"
- "Aggiungi test per lettori concorrenti + writer sotto WAL"

**File di riferimento**:
- [AsyncConcurrencyTests.cs](../CiccioSoft.Data.Sqlite.Tests.Extra/AsyncConcurrencyTests.cs)
- [SqliteCommand.cs](../Microsoft.Data.Sqlite.Core/SqliteCommand.cs)

---

## Prompt Riutilizzabili

Usali digitando `/` in chat e selezionando da prompt disponibili:

### SQLite Feature Development Template 📋

**File**: `.github/prompts/sqlite-feature-dev.prompt.md`

**Quando usare**: Pianificare e implementare nuove feature ADO.NET

**Contenuti**:
- Struttura panoramica feature (Cosa, Perché, Scope)
- Checklist requisiti funzionali/non-funzionali
- Roadmap implementazione (Interop → Core → Testing → Docs)
- Checklist stile codice
- Requisiti pattern async/concurrency
- Template codice di esempio (sync + async)
- Template testing
- Comandi build/verify

**Workflow di esempio**:
1. Copia il prompt
2. Compila nome feature e requisiti
3. Usa come checklist mentre implementi
4. Referenzia gli agent secondo necessità per step

---

## Istruzioni File-Scoped

Queste si applicano automaticamente a file corrispondenti a pattern specifici:

### Istruzioni Interop Layer 🔗

**File**: `.github/instructions/interop.instructions.md`  
**Si applica a**: `**/CiccioSoft.Data.Sqlite.Interop/**/*.cs`

**Copre**:
- Vincoli P/Invoke (Cdecl, charset, marshaling)
- Requisiti SafeHandle
- Strategie allocazione memoria
- Traduzione errori
- Nullable reference types
- Header file (MIT license)

---

### Istruzioni Core ADO.NET Layer 📚

**File**: `.github/instructions/core-adonet.instructions.md`  
**Si applica a**: `**/Microsoft.Data.Sqlite.Core/**/*.cs`

**Copre**:
- Compliance interfaccia ADO.NET
- Lifecycle connessione & defaults (Foreign Keys, WAL, Busy Timeout)
- Pattern statement caching
- Utilizzo Single-Writer Coordinator
- Implementazione async/await
- Enforcement CommandTimeout
- Gestione eccezioni
- Connection pooling

---

### Istruzioni Test ✅

**File**: `.github/instructions/tests.instructions.md`  
**Si applica a**: `**/CiccioSoft.Data.Sqlite.Tests*/**/*.cs`

**Copre**:
- Organizzazione test (Contract tests vs. Behavior tests)
- Pattern XUnit (Fact, Theory, InlineData)
- Pattern test async con CancellationToken
- Test operazioni concorrenti
- Cleanup risorse (IDisposable)
- Setup test data
- Common pitfalls

---

## Esempi di Utilizzo

### Esempio 1: Aggiungi Nuovo Metodo Async

1. Usa prompt **SQLite Feature Development** per pianificare
2. Implementa in Core ADO.NET layer (auto-applica `core-adonet.instructions.md`)
3. Usa **SQLite Async/Concurrency Agent** per review pattern cancellation
4. Scrivi test con **SQLite Testing Agent**
5. Build & test: `dotnet test CiccioSoft.Data.Sqlite.slnx`

### Esempio 2: Debugga Problema Timeout

Chiedi al **SQLite Async/Concurrency Agent**:
- "Perché CommandTimeout non viene enforced in [method name]?"
- Ricevi guidance su timeout mechanism e sqlite3_interrupt() usage
- Rivedi [SqliteCommand.cs](../Microsoft.Data.Sqlite.Core/SqliteCommand.cs) per pattern

### Esempio 3: Ottimizza Utilizzo Memoria

Chiedi al **SQLite Interop Agent**:
- "Come posso ottimizzare allocazione string in [method]?"
- Ricevi raccomandazioni stackalloc/ArrayPool
- Referenzia [Sqlite3.cs](../CiccioSoft.Data.Sqlite.Interop/Sqlite3.cs) per esempi

### Esempio 4: Scrivi Test Concorrente

Chiedi al **SQLite Testing Agent**:
- "Scrivi un test per lettura/scrittura concorrente sotto WAL"
- Ricevi pattern corretti SemaphoreSlim/Barrier
- Referenzia [AsyncConcurrencyTests.cs](../CiccioSoft.Data.Sqlite.Tests.Extra/AsyncConcurrencyTests.cs)

---

## Build e Testing

**Build tutti i progetti:**
```bash
dotnet build CiccioSoft.Data.Sqlite.slnx
```

**Esegui tutti i test:**
```bash
dotnet test CiccioSoft.Data.Sqlite.slnx
```

**Esegui suite test specifica:**
```bash
dotnet test CiccioSoft.Data.Sqlite.Tests.Extra/ --filter ClassName=AsyncConcurrencyTests
```

**Report coverage:**
```bash
dotnet test CiccioSoft.Data.Sqlite.slnx /p:CollectCoverage=true
```

---

## Risorse Architettura Chiave

- **Project README**: [README.md](../README.md) — Overview, key features, concurrency/async notes
- **Interop Guide**: [CiccioSoft.Data.Sqlite.Interop/README.md](../CiccioSoft.Data.Sqlite.Interop/README.md)
- **Test Philosophy**: [CiccioSoft.Data.Sqlite.Tests.Extra/README.md](../CiccioSoft.Data.Sqlite.Tests.Extra/README.md)
- **ADO.NET Gap Analysis**: [ADONET_GAP_ANALYSIS.md](../ADONET_GAP_ANALYSIS.md)

---

## File Structure Overview

```
.github/
├── copilot-instructions.md        ← Workspace defaults (always loaded)
├── AGENTS.md                       ← This file
├── agents/
│   ├── sqlite-testing.agent.md    ← Testing specialist
│   ├── sqlite-interop.agent.md    ← P/Invoke specialist
│   ├── sqlite-async.agent.md      ← Async/concurrency specialist
├── prompts/
│   └── sqlite-feature-dev.prompt.md ← Feature planning template
└── instructions/
    ├── interop.instructions.md     ← For Interop layer files
    ├── core-adonet.instructions.md ← For Core layer files
    └── tests.instructions.md       ← For test files
```

---

## Tips for Best Results

1. **Be specific**: "Add a test for CommandTimeout" → AI understands exact requirement
2. **Reference files**: "Following the pattern in SqliteCommand.cs..." → AI learns by example
3. **Use the right agent**: Choose the specialist (Testing, Interop, Async) for the domain
4. **Check conventions first**: Workspace instructions & file-scoped instructions are auto-applied
5. **Build & test often**: `dotnet build` and `dotnet test` after changes to catch issues early

---

## Quick Links

- 🎯 **Main README**: [README.md](../README.md)
- 🏗️ **Architecture**: [README.md — Architecture section](../README.md#-architecture)
- 📖 **Interop Guide**: [CiccioSoft.Data.Sqlite.Interop/README.md](../CiccioSoft.Data.Sqlite.Interop/README.md)
- 🧪 **Test Examples**: [CiccioSoft.Data.Sqlite.Tests.Extra/README.md](../CiccioSoft.Data.Sqlite.Tests.Extra/README.md)
- 🔗 **SQLite C API**: [https://www.sqlite.org/capi3ref.html](https://www.sqlite.org/capi3ref.html)
