# Ri-analisi gap ADO.NET (vendor-neutral) per `CiccioSoft.Data.Sqlite`

Data analisi: **3 aprile 2026**.

Obiettivo: verificare lo stato attuale del provider rispetto ai contratti ADO.NET generici (non specifici SQLite), aggiornando la precedente analisi.

## Sintesi esecutiva

Rispetto alla fotografia precedente, il provider è maturato in modo significativo: i principali gap P0/P1 individuati in precedenza risultano oggi **coperti** (o coperti con policy esplicita e test dedicati).

In particolare:
- `CommandType` è ora vincolato a `Text` con rifiuto esplicito degli altri valori.
- `CommandTimeout` è applicato con un meccanismo reale di timeout/cancel via `Interrupt`.
- La coerenza `Command.Transaction` è validata in esecuzione/prepare.
- `ParameterDirection` ha una policy esplicita: solo `Input` in esecuzione.
- `DbDataReader.NextResult()` supporta batch/multi-result set.
- `DbDataReader.GetSchemaTable()` è implementato con una shape utile e allineata al contratto ADO.NET.

## Stato attuale per area (delta rispetto al precedente report)

## 1) `DbCommand.CommandType` — **RISOLTO**

Stato corrente:
- `SqliteCommand.CommandType` accetta solo `CommandType.Text`.
- Valori diversi vengono rifiutati con `NotSupportedException` e messaggio risorsa dedicato.

Valutazione:
- Contratto chiaro e prevedibile lato consumer cross-provider.

## 2) `CommandTimeout` enforcement — **RISOLTO**

Stato corrente:
- Ogni esecuzione crea una `CommandExecutionScope` che collega cancellazione esterna e timeout interno.
- In caso di timeout viene invocato `Interrupt` e sollevata `SqliteException` con messaggio semantico di timeout.

Valutazione:
- Comportamento robusto sia su `Execute*` sia durante lo stepping del reader.

## 3) Semantica `Command.Transaction` — **RISOLTO**

Stato corrente:
- Prima delle operazioni viene validata la transazione.
- Sono gestiti i casi di transazione completata e mismatch di connessione.

Valutazione:
- Contratto transaction/connection ora solido per ORM/unit-of-work.

## 4) `ParameterDirection` — **RISOLTO (policy di non supporto esplicito)**

Stato corrente:
- `SqliteParameter` ammette i valori `Input/Output/InputOutput/ReturnValue` a livello proprietà.
- In fase di bind comando, qualunque direzione diversa da `Input` viene rifiutata esplicitamente.

Valutazione:
- Policy coerente e trasparente: output/return non supportati in execute.

## 5) `DbDataReader.GetSchemaTable()` — **RISOLTO (implementazione base avanzata)**

Stato corrente:
- Metodo implementato con colonne metadata standard (`ColumnName`, `ColumnOrdinal`, `DataType`, base table/column, alias/expression, ecc.).
- Include inferenze tipo utili in scenari SQLite dinamici.

Valutazione:
- Copertura adeguata per `DataTable.Load`, tooling, mapper e casi legacy.

## 6) Multi-result set (`NextResult`) — **RISOLTO**

Stato corrente:
- `NextResult()` attraversa gli statement successivi e salta i DML senza result-set.
- Gestisce correttamente fine batch/errori/close.

Valutazione:
- Comportamento allineato alle aspettative pratiche ADO.NET.

## 7) `DbParameterCollection` hardening — **PARZIALMENTE RISOLTO**

Miglioramenti presenti:
- Validazione robusta tipo parametro (`SqliteParameter` obbligatorio, no null).
- Normalizzazione prefissi (`@`, `:`, `$`) per lookup coerente.

Gap residui (non bloccanti):
- Nessuna policy forte sui nomi duplicati oltre alla logica attuale di lista.
- Eccezioni/semantiche su alcuni edge case restano “minimali” rispetto ai provider storici più maturi.

## 8) Async model — **RISOLTO (no `Task.Run` di facciata)**

Stato corrente:
- `OpenAsync` e `ReadAsync` sono implementati in modalità cooperativa (completamento immediato + cancellazione), senza wrapper `Task.Run`.
- Il controllo del cancel/timeout passa dal meccanismo comune di scope + interrupt.

Valutazione:
- Migliorata prevedibilità e assenza di offload artificiale su thread pool.

## Gap residui (nuova fotografia)

Questi punti non sono regressioni, ma possibili evoluzioni per una copertura “enterprise-grade” ancora più ampia:

1. **Factory completeness**
   - `SqliteFactory` espone connection/command/parameter/builder, ma non `DbDataAdapter` o `DbCommandBuilder`.
   - Impatto: scenari legacy DataSet/DataAdapter possono richiedere componenti aggiuntivi.

2. **Metadata fidelity avanzata in `GetSchemaTable()`**
   - Campi come `IsKey`/`IsUnique` risultano oggi impostati in modo conservativo (default) e non sempre inferiti dal catalogo.
   - Impatto: alcuni tool di generazione schema potrebbero voler maggiore precisione.

3. **Allineamento test/contratto eccezioni su `CommandType`**
   - Nel codice produzione il rifiuto usa `NotSupportedException`; verificare che i test riflettano esattamente questa scelta semantica.

## Priorità consigliata (nuova roadmap)

1. **P1**: completare il perimetro `DbProviderFactory` con `DataAdapter`/`CommandBuilder` (se target di compatibilità richiesto).
2. **P1**: arricchire inferenza chiavi/unicità in `GetSchemaTable()`.
3. **P2**: rifinire ulteriormente `DbParameterCollection` per edge case e parity con provider ADO.NET storici.
4. **P2**: verifica periodica di coerenza tra contratti runtime e test (eccezioni/tipi/messaggi).

## Conclusione

Il provider è oggi in uno stato **molto più vicino a un ADO.NET provider general-purpose** rispetto al report precedente: i gap funzionali più critici risultano coperti e testati in modo esteso.

Le attività rimanenti sono prevalentemente di **completamento ecosistema** e **finitura contrattuale**, più che di core functionality.
