# Analisi gap ADO.NET (vendor-neutral) per `CiccioSoft.Data.Sqlite`

Obiettivo: evidenziare le funzionalità mancanti **non specifiche di SQLite** per arrivare a una copertura più completa dei contratti ADO.NET usati da ORM/librerie data-access.

## Stato attuale (base già presente)

Il provider implementa già i blocchi fondamentali:
- `DbConnection` con `Open/Close/OpenAsync`, stato, factory command/transaction.
- `DbCommand` con esecuzione sync/async, `Prepare`, `Cancel`, parametri.
- `DbDataReader` con lettura tipizzata, indicizzazione per nome/ordinale, `Read/ReadAsync`.
- `DbTransaction` con `Commit/Rollback` sync/async e isolamento base.
- `DbProviderFactory` già presente nel progetto e testato.

## Gap principali rispetto a un provider ADO.NET “general purpose”

## 1) Contratto `DbCommand.CommandType` incompleto

**Problema**
`SqliteCommand` espone `CommandType`, ma l’esecuzione non cambia comportamento: non c’è validazione/gestione esplicita di `StoredProcedure` e `TableDirect`.

**Perché è ADO.NET core**
Molti provider gestiscono esplicitamente questi valori (anche solo lanciando eccezioni coerenti). Lasciare proprietà “inerte” può creare incompatibilità con ORM e utility cross-provider.

**Raccomandazione**
- Supportare solo `CommandType.Text`.
- Per altri valori: `NotSupportedException` esplicita e testata.

## 2) `CommandTimeout` non applicato in modo robusto

**Problema**
`CommandTimeout` è presente ma non viene tradotto in un meccanismo di timeout per singolo comando (l’esecuzione è legata a `Step()` senza enforcement temporale).

**Perché è ADO.NET core**
Timeout prevedibile è un requisito comune cross-provider per resilienza applicativa.

**Raccomandazione**
- Applicare timeout per comando (timer + `Interrupt`, o policy equivalente).
- Uniformare comportamento sync/async e documentare semantica timeout.

## 3) Semantica transazionale su `DbCommand.Transaction` da irrobustire

**Problema**
La proprietà `Transaction` esiste ma non si vedono controlli rigorosi su:
- transazione completata,
- transazione su connessione differente,
- connessione chiusa.

**Perché è ADO.NET core**
Il legame command/transaction è uno dei punti più sensibili per ORM e unit-of-work.

**Raccomandazione**
- Validare coerenza `command.Connection` ↔ `command.Transaction.Connection` prima di execute/prepare.
- Fallire con `InvalidOperationException` coerente.

## 4) Parametri: copertura limitata a input (manca semantica output/return)

**Problema**
`SqliteParameter` espone `Direction`, ma la pipeline di bind tratta solo input; non c’è ciclo di valorizzazione output/return.

**Perché è ADO.NET core (vendor-neutral)**
Anche quando il backend non supporta stored procedure, i provider in genere:
- o supportano parzialmente output,
- o rifiutano con errore esplicito coerente.

**Raccomandazione**
- Definire policy chiara: supporto o rifiuto esplicito per `Output/InputOutput/ReturnValue`.
- Aggiungere test di comportamento.

## 5) `DbDataReader.GetSchemaTable()` / metadati avanzati

**Problema**
Il reader implementa API base, ma non espone un `GetSchemaTable()` ricco (metadati colonna standard ADO.NET).

**Perché è ADO.NET core**
Tooling, mapper legacy, ETL e alcuni ORM usano `GetSchemaTable()` per inferenza.

**Raccomandazione**
- Implementare `GetSchemaTable()` con subset utile (ColumnName, ColumnOrdinal, DataType, AllowDBNull, IsKey se disponibile, ecc.).
- Testare presenza/consistenza colonne metadato standard.

## 6) Multi-result set (`NextResult`) e batch SQL

**Problema**
`NextResult()` ritorna sempre `false`.

**Perché è ADO.NET core (in pratica)**
Molte librerie inviano batch multi-statement e si aspettano scorrimento result-set.

**Raccomandazione**
- Se non si supporta multi-result: errore/limitazione documentata e testata.
- Preferibile: supporto progressivo `NextResult` per statement multipli.

## 7) `DbParameterCollection` – robustezza contrattuale

**Problema**
La collezione è funzionale ma minimale (casting diretto, eccezioni poco uniformi, validazioni tipo/nome limitate).

**Perché è ADO.NET core**
Molto codice riflessivo/generico si appoggia a comportamenti standard della collection.

**Raccomandazione**
- Migliorare validazioni input (`null`, tipo non `DbParameter`, nomi duplicati/edge case).
- Allineare eccezioni a pattern `DbParameterCollection`.

## 8) Async: evitare dipendenza eccessiva da `Task.Run`

**Problema**
`OpenAsync`/`ReadAsync` usano `Task.Run`.

**Perché è rilevante cross-provider**
Non è un bug, ma limita scalabilità e prevedibilità in contesti ad alto throughput.

**Raccomandazione**
- Dove possibile, preferire async cooperativo senza thread dedicato.
- Mantenere cancellazione coerente con `Interrupt`.

## Priorità consigliata (roadmap)

1. **P0**: `CommandType` contract + validazioni `Transaction` + policy `ParameterDirection`.
2. **P1**: enforcement `CommandTimeout` e test robusti sync/async.
3. **P1**: `GetSchemaTable()` base e hardening `DbParameterCollection`.
4. **P2**: multi-result (`NextResult`) e miglioramento async non-blocking.

## Criterio di accettazione minimo (vendor-neutral)

Una volta completati i punti P0/P1, il provider copre in modo più affidabile la maggior parte delle aspettative ADO.NET generiche senza aggiungere feature specifiche SQLite.
