# Timeline evolutiva di CiccioSoft.Data.Sqlite

Questa timeline organizza l’evoluzione del progetto in quattro fasi incrementali, mettendo in evidenza **obiettivi**, **implementazioni chiave** e **impatto** su affidabilità e compatibilità ADO.NET.

## Fase 1 — Interop foundation (CiccioSoft.Sqlite.Interop)

### Obiettivo
Costruire una base nativa robusta e performante per dialogare con SQLite tramite P/Invoke, con controllo esplicito di risorse e errori.

### Cosa è stato implementato
- Wrapper `Sqlite3` con apertura connessione e marshalling UTF-8 ottimizzato (`stackalloc`/`ArrayPool`).
- API per compilazione statement (`Prepare`) sia singolo comando sia batch con offset byte (`Prepare(sql, offset, out nextOffset)`).
- Primitive operative native (`Changes`, `TotalChanges`, `SetBusyTimeout`, `Interrupt`, `GetTransactionState`, error code estesi).
- Wrapper `Sqlite3Stmt` per ciclo statement (`Step`, `Reset`, `ClearBindings`), binding parametri e metadata colonne di origine.

### Come è stato implementato
- Uso di `SafeHandle` e `IDisposable` per garantire rilascio deterministico delle risorse native.
- Conversione stringhe e buffer con approccio low-allocation per minimizzare pressione GC.
- Mappatura errori nativi in eccezioni managed coerenti.

### Impatto
- Fondamenta solide per implementare sopra un provider ADO.NET senza dipendere da librerie esterne.
- Supporto tecnico al parsing batch e quindi al futuro `NextResult` del reader.

## Fase 2 — Provider core ADO.NET (CiccioSoft.Data.Sqlite)

### Obiettivo
Esporre un provider ADO.NET completo nei componenti essenziali (`Connection`, `Command`, `Reader`, `Transaction`, `Factory`).

### Cosa è stato implementato
- `SqliteConnection` con lifecycle `Open/Close/OpenAsync`, stato, creazione command/transaction.
- Applicazione setting di connessione (busy timeout, `foreign_keys`, `journal_mode`) e profili (`Default`, `StrictSingleConnection`).
- `SqliteCommand` con pipeline di esecuzione sync/async e binding parametri .NET → SQLite.
- `SqliteDataReader` con lettura tipizzata, indicizzazione ordinale/nome, gestione lifecycle e behavior (`CloseConnection`, `SingleRow`).
- `SqliteTransaction` con `Commit/Rollback` sync/async e normalizzazione livelli isolamento.
- `SqliteFactory` e `SqliteConnectionStringBuilder` per integrazione standard provider/factory.
- Pooling interno sessioni con `SqliteConnectionPool` (`Rent/Return`, limite max pool size).

### Come è stato implementato
- Sessione nativa incapsulata (`SqliteSession`) con `SemaphoreSlim` (`Gate`) per serializzare accesso al handle SQLite.
- Costruzione a layer: il provider alto livello usa esclusivamente primitive stabili del layer interop.
- Strategia coerente con policy di scope: prima contratti ADO.NET core, poi extra cross-provider selettivi.

### Impatto
- Provider usabile end-to-end per scenari data-access comuni.
- Architettura pulita e mantenibile, con separazione netta managed/native.

## Fase 3 — Hardening ADO.NET contract

### Obiettivo
Ridurre i gap di compatibilità vendor-neutral evidenziati dall’analisi ADO.NET e rendere il comportamento più prevedibile per ORM/librerie generiche.

### Cosa è stato implementato
- `CommandType` reso esplicito: supportato solo `Text`; altri valori rifiutati con `NotSupportedException`.
- `CommandTimeout` applicato all’intero scope comando (prepare + execute + reader lifecycle) con interrupt nativo.
- Semantica `DbCommand.Transaction` rinforzata con validazioni di coerenza connessione/transazione e transazione completata.
- Policy `ParameterDirection` chiarita: supporto solo `Input`, rifiuto esplicito di `Output/InputOutput/ReturnValue`.
- `DbParameterCollection` irrobustita (validazioni null/tipo/nomi edge-case).
- `DbDataReader.NextResult()` implementato in modo reale su batch multi-statement.
- `GetSchemaTable()` arricchito con metadati standard utili a tooling e mapper.

### Come è stato implementato
- Introduzione di `CommandExecutionScope` con token linked (timeout + cancellation) e traduzione errori/cancellazioni.
- Preparazione statement batch via offset byte UTF-8 per avanzare in modo deterministico tra statement multipli.
- Allineamento eccezioni/messaggi risorsa per comportamenti consistenti e testabili.

### Impatto
- Maggiore compatibilità con aspettative ADO.NET “general purpose”.
- Migliore resilienza operativa (timeout/cancel) e riduzione di comportamenti ambigui.

## Fase 4 — Test parity & affidabilità comportamentale

### Obiettivo
Consolidare i contratti implementati con test ampi e mirati, in ottica regressione e compatibilità.

### Cosa è stato implementato
- Test su behavior asincroni e cancellazione durante attesa gate connessione.
- Test su semantica transazionale (isolamento, completed transaction).
- Test su policy `ParameterDirection` e su parity binding parametri (prefissi, null/blob, formattazioni).
- Test su `NextResult` e `GetSchemaTable` in scenari articolati.
- Test su `SqliteFactory` e `SqliteConnectionStringBuilder` (default, parsing, clamp valori).

### Come è stato implementato
- Approccio “contract-first”: ogni comportamento critico introdotto/hardening è accompagnato da test dedicati.
- Copertura bilanciata tra API surface (factory/builder) e semantiche runtime (command/reader/transaction).

### Impatto
- Aumento confidenza sui refactor futuri.
- Riduzione rischio regressioni su percorsi ADO.NET ad alto impatto per consumer e ORM.

---

## Vista sintetica per milestone

- **Milestone A (Fase 1):** base interop solida e performante.
- **Milestone B (Fase 2):** provider ADO.NET funzionante end-to-end.
- **Milestone C (Fase 3):** hardening vendor-neutral dei contratti più sensibili.
- **Milestone D (Fase 4):** parity comportamentale consolidata via test.

