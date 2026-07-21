# Ri-analisi gap ADO.NET (vendor-neutral) per `CiccioSoft.Data.Sqlite`

Data analisi: **20 luglio 2026** (aggiornamento verificato contro il codice sorgente corrente su `main`).

Obiettivo: verificare lo stato attuale del provider rispetto ai contratti ADO.NET generici (non specifici SQLite), correggendo le imprecisioni emerse dal confronto diretto con il codice rispetto al report del 9 aprile 2026.

## Sintesi esecutiva

Il provider resta maturo sui gap P0/P1 storici: i principali problemi individuati nelle analisi precedenti sono coperti da codice e da policy esplicite. Rispetto al report del 9 aprile, questa revisione **corregge tre imprecisioni** che erano presenti nel documento precedente (verificate riga per riga sul codice):

1. L'eccezione sollevata da `CommandType` non è `NotSupportedException` come indicato, ma `ArgumentException`.
2. Il meccanismo di enforcement su `ParameterDirection` è invertito rispetto a quanto descritto: il rifiuto avviene già a livello di proprietà (`SqliteParameter.Direction`), non in fase di bind del comando — la validazione lato `SqliteCommand` è codice morto, commentato.
3. Il gap residuo su `IsKey`/`IsUnique` in `GetSchemaTable()` risultava già superato: l'inferenza da catalogo è implementata e funzionante, non è un default conservativo.

## Stato attuale per area (verificato sul codice)

## 1) `DbCommand.CommandType` — **RISOLTO**

Stato corrente (`SqliteCommand.cs`):
- `SqliteCommand.CommandType` accetta solo `CommandType.Text`.
- Valori diversi vengono rifiutati con **`ArgumentException`** (non `NotSupportedException`) e messaggio risorsa dedicato (`InvalidCommandType`).

Valutazione:
- Contratto chiaro e prevedibile lato consumer cross-provider.
- Nota semantica: `ArgumentException` è discutibile per un valore "non supportato in questo provider" — molti provider storici userebbero `NotSupportedException` in questo scenario. Non è un errore, ma è una scelta di design da confermare consapevolmente (vedi raccomandazioni).

## 2) `CommandTimeout` enforcement — **RISOLTO**

Stato corrente:
- Ogni esecuzione crea una `CommandExecutionScope` che collega cancellazione esterna, cancellazione dell'operazione e timeout interno tramite `CancellationTokenSource` dedicato.
- Tre percorsi di errore distinti sono gestiti esplicitamente: timeout (`_timeoutTriggered`), cancellazione dell'operazione (`operationCancellationToken`), cancellazione esterna (`_externalCancellationToken`) — tutti intercettano `EngineException` con `Result.Interrupt` e la rimappano in modo semantico.
- In caso di timeout viene invocato `Interrupt` e sollevata `SqliteException` con messaggio esplicito di timeout.

Valutazione:
- Comportamento robusto sia su `Execute*` sia durante lo stepping del reader. Nessuna correzione necessaria.

## 3) Semantica `Command.Transaction` — **RISOLTO**

Stato corrente:
- `ValidateTransaction` viene invocato prima di ogni operazione di esecuzione (`ExecuteReader`, `ExecuteNonQuery`, `ExecuteScalar`, prepare).
- Gestisce esplicitamente: transazione richiesta ma assente (`TransactionRequired`), transazione già completata/disconnessa (`TransactionCompleted`), mismatch tra la connessione del comando e quella della transazione (`TransactionConnectionMismatch`).

Valutazione:
- Contratto transaction/connection solido per scenari ORM/unit-of-work. Nessuna correzione necessaria.

## 4) `ParameterDirection` — **RISOLTO, ma descrizione precedente invertita**

Stato corrente (verificato in `SqliteParameter.cs` e `SqliteCommand.cs`):
- Il rifiuto avviene **a livello di proprietà**: il setter di `SqliteParameter.Direction` lancia `ArgumentException` non appena si assegna un valore diverso da `ParameterDirection.Input`. Non è quindi possibile impostare `Output`/`InputOutput`/`ReturnValue` su un'istanza, a differenza di quanto indicato nel report precedente.
- La validazione equivalente lato `SqliteCommand` (`ValidateParameterDirection`) esiste nel codice ma è **interamente commentata** (dead code), quindi non viene mai eseguita in fase di bind.

Valutazione:
- Il comportamento finale verso il consumer è corretto (solo `Input` è utilizzabile), ma il punto di enforcement è diverso da quanto documentato in precedenza. Non è un problema funzionale, ma un disallineamento di documentazione ora corretto.
- Consigliato: rimuovere il codice morto `ValidateParameterDirection` in `SqliteCommand.cs`, oppure riattivarlo esplicitamente come difesa in profondità se si prevede che `SqliteParameter` possa in futuro ammettere altri valori a livello di proprietà.

## 5) `DbDataReader.GetSchemaTable()` — **RISOLTO, con inferenza chiave/unicità già implementata**

Stato corrente:
- Metodo implementato con colonne metadata standard (`ColumnName`, `ColumnOrdinal`, `DataType`, base table/column, alias/expression, ecc.).
- **`IsKey` e `IsUnique` sono effettivamente inferiti dal catalogo quando la colonna ha un'origine tracciabile** (non aliasata): `IsSingleColumnUnique(baseTableName, baseColumnName)` per l'unicità, `TryGetTableColumnMetadata(...)` per chiave primaria, nullability e auto-increment.
- Il fallback a `false`/`DBNull` scatta solo per colonne senza origine tracciabile (espressioni, alias) o quando il metadata non è recuperabile — comportamento corretto, non un default conservativo generalizzato come indicato in precedenza.

Valutazione:
- Il gap "P1: arricchire inferenza chiavi/unicità" del report precedente **risulta già coperto** dal codice attuale. Non richiede più priorità P1; resta eventualmente da verificare la copertura di test su casi limite (colonne composte in chiavi multi-colonna, viste).

## 6) Multi-result set (`NextResult`) — **RISOLTO**

Stato corrente:
- `NextResultCore` attraversa gli statement successivi e salta i DML senza result-set.
- Gestisce correttamente fine batch/errori/close, sia dalla chiamata iniziale (`initialResult: true`) sia da `NextResult()` esplicito.

Valutazione:
- Comportamento allineato alle aspettative pratiche ADO.NET. Nessuna correzione necessaria.

## 7) `DbParameterCollection` hardening — **PARZIALMENTE RISOLTO**

Stato corrente:
- Validazione robusta tipo parametro (`SqliteParameter` obbligatorio, no null).
- Normalizzazione prefissi (`@`, `:`, `$`) tramite `NormalizeParameterName`, usata sia in ricerca/lookup sia in validazione nome (rifiuto di nomi che contengono solo il prefisso).

Gap residui confermati (non bloccanti):
- Nessuna policy esplicita sui nomi duplicati oltre al confronto case-insensitive già presente nel lookup lineare.
- Semantiche di errore su alcuni edge case restano minimali rispetto ai provider storici più maturi.

## 8) Async model — **RISOLTO (nessun `Task.Run` di facciata)**

Stato corrente:
- `OpenAsync` (in `SqliteConnection.cs`) e `ReadAsync` (in `SqliteDataReader.cs`) sono implementati in modalità cooperativa: `ReadAsync` esegue lo step sincrono e ritorna `Task.FromResult(_hasRow)`, senza alcun `Task.Run` o offload artificiale su thread pool.
- Il controllo cancel/timeout passa dal meccanismo comune di `CommandExecutionScope` + `Interrupt` descritto al punto 2.

Valutazione:
- Comportamento prevedibile e coerente con l'assenza di vera asincronia nativa di SQLite. Nessuna correzione necessaria.

## Gap residui (fotografia aggiornata)

1. **Factory completeness (perimetro intenzionale)** — confermato nel codice:
   - `SqliteFactory.CanCreateDataAdapter` e `CanCreateCommandBuilder` sono esplicitamente `false`.
   - Decisione di prodotto: `DataAdapter`/`CommandBuilder` fuori scope, in linea con l'impostazione moderna di `Microsoft.Data.Sqlite`.
   - Impatto: scenari DataSet/DataAdapter richiedono un layer applicativo dedicato o migrazione a pattern data reader/ORM.

2. **Coerenza semantica dell'eccezione su `CommandType`** — nuovo finding di questa revisione:
   - Il codice usa `ArgumentException`; se l'intento architetturale è segnalare "funzionalità non supportata dal provider" (piuttosto che "argomento non valido in questo contesto"), `NotSupportedException` sarebbe più coerente con le convenzioni ADO.NET e con altri punti del provider (es. `CanCreateDataAdapter = false`).
   - Da decidere consapevolmente: mantenere `ArgumentException` (documentandolo come scelta esplicita) o migrare a `NotSupportedException` con relativo aggiornamento dei test.

3. **Codice morto in `SqliteCommand.ValidateParameterDirection`** — nuovo finding di questa revisione:
   - Metodo completo ma interamente commentato. Da rimuovere se ridondante rispetto al controllo sulla property, oppure da riattivare se si vuole difesa in profondità.

4. **`DbParameterCollection` edge case** — invariato dal report precedente:
   - Nessuna policy forte sui nomi duplicati.
   - Parity con provider ADO.NET storici su messaggi/eccezioni di edge case ancora da rifinire.

## Priorità consigliata (roadmap aggiornata)

1. **P1**: decidere e applicare in modo coerente il tipo di eccezione per funzionalità non supportate (`CommandType`, `ParameterDirection`) — uniformare a `NotSupportedException` oppure documentare esplicitamente la scelta di `ArgumentException`.
2. **P2**: rimuovere o riattivare il codice morto `ValidateParameterDirection` in `SqliteCommand.cs`.
3. **P2**: rifinire ulteriormente `DbParameterCollection` per edge case (duplicati) e parity con provider ADO.NET storici.
4. **P3**: aggiungere test espliciti di regressione per l'inferenza `IsKey`/`IsUnique` in `GetSchemaTable()` su chiavi composte e viste, per blindare un comportamento già corretto ma non ancora coperto da questa verifica documentale.
5. **P3**: verifica periodica di coerenza tra contratti runtime e test (eccezioni/tipi/messaggi) — retained dal report precedente.

## Conclusione

Il provider resta in uno stato **molto vicino a un ADO.NET provider general-purpose**: tutti i gap funzionali critici P0/P1 storici sono coperti e testati. Questa revisione non individua nuove regressioni funzionali, ma corregge tre imprecisioni documentali del report precedente (tipo di eccezione su `CommandType`, punto di enforcement su `ParameterDirection`, stato dell'inferenza in `GetSchemaTable()`) e segnala un piccolo pezzo di codice morto da ripulire. Le attività rimanenti sono di **finitura contrattuale e igiene del codice**, non di core functionality; il perimetro `DataAdapter` resta intenzionalmente escluso.
