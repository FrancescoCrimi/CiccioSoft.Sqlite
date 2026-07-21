# CiccioSoft.Data.Sqlite — Roadmap architetturale verso un provider ADO.NET enterprise-grade

Data analisi: 21 luglio 2026.
Ambito: `CiccioSoft.Data.Sqlite` (provider ADO.NET), confrontato con `Microsoft.Data.SqlClient`, `Npgsql`, `MySqlConnector` e `Oracle.ManagedDataAccess.Core` — i quattro provider ADO.NET .NET considerati oggi lo stato dell'arte in ambito enterprise.

Questo documento parte da `ADONET_GAP_ANALYSIS.md` (conformità al contratto ADO.NET generico, oggi in buono stato) e da `CONNECTION_POOL_COMPARISON.md` (pooling), e li estende: l'obiettivo qui non è "essere un provider ADO.NET corretto", ma **"essere un provider su cui un team enterprise scommette in produzione"**. Sono due barre diverse: la seconda include osservabilità, resilienza, operabilità, superficie di estensione e garanzie sotto carico.

---

## 0. Premessa: cosa manca ancora, in una frase

`CiccioSoft.Data.Sqlite` implementa correttamente i *contratti* ADO.NET (`DbConnection`/`DbCommand`/`DbDataReader`/`DbTransaction`), ma non implementa ancora nessuna delle funzionalità "di piattaforma" che i provider maggiori hanno aggiunto negli ultimi 4-5 anni: `DbDataSource`, `DbBatch`, telemetria `Activity`/`OpenTelemetry`, statement caching a livello di connessione, savepoint, retry/resiliency hook, integrazione `Microsoft.Extensions.*`. Questo documento li tratta uno per uno, con priorità e sketch implementativi.

---

## 1. Bug architetturale bloccante da correggere prima di tutto

Prima di aggiungere qualunque feature enterprise, va chiuso il difetto già identificato in `SqliteConnectionPool.Return()`: manca `state.Semaphore.Release()`, quindi qualunque `Rent`/`RentAsync` che si accoda quando `Count >= MaxPoolSize` **si blocca per sempre**, anche se altre sessioni tornano nella `Bag`. Nessun provider enterprise può permettersi un deadlock silenzioso sotto carico: è la prima cosa che un load test in CI deve intercettare (vedi §7, "Testing di carico continuo"). Questo fix è un prerequisito, non un item della roadmap: senza, ogni altra discussione su "enterprise-grade" è teorica.

---

## 2. Analisi comparativa per area

### 2.1 Modello di pooling: oggetto singolo vs `DbDataSource`

**Stato attuale**: pool statico per `connectionString` (`SqliteConnectionPool`), `ConcurrentBag` + semaforo, nessuna API pubblica per possedere/configurare il pool a livello di applicazione.

**Come fanno gli altri**: da .NET 7 tutti i provider principali (Npgsql `NpgsqlDataSource`, SqlClient `SqlDataSource`/`SqlConnection.CreateDataSource`, MySqlConnector `MySqlDataSource`, Oracle `OracleDataSource`) hanno adottato l'astrazione `System.Data.Common.DbDataSource`: un oggetto factory *owned dall'applicazione*, registrato una volta in DI, che possiede il pool, espone `CreateConnection()`/`OpenConnectionAsync()`, e — punto chiave — permette di **agganciare interceptor, logger e configurazione per singola istanza** invece che per connection string globale (che con dizionari statici globali crea problemi di isolamento nei test e in scenari multi-tenant).

**Impatto se non colmato**: senza `DbDataSource`, ogni consumer enterprise che vuole DI-friendly pooling (es. ASP.NET Core con `AddSqliteDataSource`) deve arrangiarsi con wrapper custom. È oggi il gap più visibile rispetto ai peer.

**Raccomandazione (P0)**:
```csharp
public sealed class SqliteDataSource : DbDataSource
{
    private readonly string _connectionString;
    private readonly PoolState _pool; // owned, non statico globale

    public SqliteDataSource(string connectionString) { /* crea pool dedicato */ }

    public override string ConnectionString => _connectionString;

    protected override DbConnection CreateDbConnection() =>
        new SqliteConnection(this); // la connection referenzia il DataSource, non un dizionario statico

    protected override DbConnection OpenDbConnection() { /* ... */ }
    protected override async ValueTask<DbConnection> OpenDbConnectionAsync(CancellationToken ct) { /* ... */ }

    public override DbBatch CreateBatch() => new SqliteBatch(this);
}
```
Questo cambio ha un effetto collaterale positivo: **elimina il dizionario statico `Pools`** attuale (stato globale per processo, difficile da isolare nei test paralleli e da smaltire in scenari con molte connection string diverse, es. multi-tenant/sharding). Il pool diventa scope-owned, con `DisposeAsync` deterministico.

---

### 2.2 Batching (`DbBatch`)

**Stato attuale**: `SqliteFactory.CanCreateBatch => false`. Nessun supporto.

**Come fanno gli altri**: `DbBatch`/`DbBatchCommand` (.NET 6+) sono implementati da Npgsql e SqlClient per eseguire più comandi in un'unica interazione, con un solo giro di validazione/interceptor e — dove il protocollo lo consente — un solo round-trip. Per SQLite, che è in-process, il "round trip" non è di rete, ma il batching resta prezioso per:
- ridurre l'overhead di `Prepare`/`Step`/`Finalize` per comandi ripetuti (bulk insert applicativo);
- dare ai consumer (EF Core, Dapper.AOT, micro-ORM) un'unica API invece di doverla emulare con stringhe SQL concatenate.

EF Core, in particolare, da EF Core 7 preferisce `DbBatch` quando il provider lo espone, per gli `UPDATE`/`INSERT` generati da `SaveChanges`. **Senza `DbBatch`, il provider non può beneficiare di questa ottimizzazione EF Core** anche se un domani un provider EF Core Sqlite basato su `CiccioSoft.Data.Sqlite` venisse scritto.

**Raccomandazione (P1)**: implementare `SqliteBatch : DbBatch` che internamente esegue gli statement sulla stessa `SqliteSession`, riusando la transazione implicita/esplicita corrente, e che one-shot prepara tutti i `BatchCommands` prima di eseguire (fail-fast su errori di sintassi dell'intero batch, comportamento coerente con Npgsql).

---

### 2.3 Osservabilità: `Activity`/OpenTelemetry, `EventSource`, logging strutturato

**Stato attuale**: nessun riferimento a `System.Diagnostics.Activity`, `DiagnosticListener` o `EventSource` in tutto il progetto (verificato via grep sull'intero albero sorgenti).

**Come fanno gli altri**: è oggi lo standard de facto.
- **SqlClient** emette `Activity` con `ActivitySource` (`Microsoft.Data.SqlClient.EventSource` + tracing OTel nativo dalla 5.x), tag semantic-convention (`db.system`, `db.statement`, `db.name`).
- **Npgsql** ha `Npgsql.OpenTelemetry` package + `EventSource` interno per contatori (comandi/sec, pool size, connessioni in uso).
- **MySqlConnector** espone `ActivitySource` con nome `"MySqlConnector"` e tag OTel-compliant di default, senza pacchetto separato.

Per un provider enterprise, questo non è un "nice to have": senza tracing distribuito e metriche di pool (in uso / disponibili / in attesa), un team SRE non ha visibilità su saturazione pool, query lente, retry — cioè esattamente lo scenario del deadlock di §1, che con un `EventCounter` su "thread in attesa sul semaforo del pool" sarebbe stato visibile in produzione prima del post-mortem.

**Raccomandazione (P0/P1)**:
1. `ActivitySource` (`"CiccioSoft.Data.Sqlite"`) con una `Activity` per `ExecuteReader`/`ExecuteNonQuery`/`ExecuteScalar`, tag `db.system=sqlite`, `db.statement` (troncato/redatto secondo policy), `db.sqlite.data_source`.
2. `EventSource` (`CiccioSoft.Data.Sqlite.EventSource`) con `EventCounter`: `pool-active-connections`, `pool-idle-connections`, `pool-waiters`, `commands-per-second`, `command-duration-ms` (istogramma). Questo è anche lo strumento più economico per diagnosticare in futuro bug di pooling come quello di §1 senza dover leggere il codice sorgente.
3. Hook opzionale `Microsoft.Extensions.Logging.ILogger` iniettabile via `SqliteDataSource`, per log strutturati (`LoggerMessage` source-generated per zero-alloc in hot path, coerente con l'enfasi già presente nel progetto su zero-allocation).

---

### 2.4 Statement caching a livello di connessione

**Stato attuale**: `SqliteCommand` mantiene una cache *locale al singolo comando* (`_preparedStatements`), invalidata quando cambia `CommandText`/parametri. Non c'è invece una cache **a livello di connessione/sessione** che permetta di riusare uno statement SQLite già preparato quando un consumer (tipico in scenari ORM/Dapper) ricrea `SqliteCommand` a ogni chiamata con lo stesso testo SQL.

**Come fanno gli altri**: Npgsql fa "automatic preparation" — dopo N esecuzioni identiche (`Statement Preparation Threshold`, default 5), promuove automaticamente lo statement a prepared e lo tiene in una LRU cache per connessione (`Max Auto Prepare`, default 20). Oracle ha uno statement cache esplicito e dimensionabile (`Statement Cache Size` in connection string). SqlClient si appoggia al plan cache lato server ma mantiene comunque un client-side handle cache per `sp_prepare`.

**Impatto**: per SQLite, dove `sqlite3_prepare_v2` + bind ha un costo non trascurabile su query ripetute ad alta frequenza (tipico di OLTP enterprise: stessa query, migliaia di volte/sec), l'assenza di una cache a livello di connessione significa ripetere `Prepare`/`Finalize` per ogni `SqliteCommand` "usa e getta" creato da un micro-ORM — esattamente il pattern più comune in produzione.

**Raccomandazione (P1)**: LRU cache (`Dictionary<string,Statement>` + lista di uso) sulla `SqliteSession`, chiave = testo SQL normalizzato, dimensione configurabile via connection string (`Statement Cache Size`, default es. 32), con invalidazione su `DDL`/`ALTER`/`sqlite3_reset` fallito. Da esporre come opt-in per non alterare la semantica attuale per chi già dipende dal comportamento corrente.

---

### 2.5 Transazioni: savepoint, nesting, distribuited transactions

**Stato attuale**: `SqliteTransaction` supporta transazioni singole (`BEGIN`/`COMMIT`/`ROLLBACK`), senza savepoint. Non c'è enlistment in `System.Transactions.TransactionScope`.

**Come fanno gli altri**: da .NET 8, `DbTransaction` espone `Save(string)`, `Rollback(string)`, `Release(string)` come metodi virtuali nativi del framework — SqlClient e Npgsql li implementano entrambi mappandoli su `SAVE TRANSACTION`/`SAVEPOINT`. SQLite supporta nativamente `SAVEPOINT`/`RELEASE`/`ROLLBACK TO`, quindi qui il gap è puramente implementativo, non un limite del motore. Questo è particolarmente rilevante per **EF Core**, che usa i savepoint per implementare `SaveChanges` resiliente con retry senza dover riavviare l'intera transazione logica.

**Raccomandazione (P1, basso rischio/alto valore)**:
```csharp
public override void Save(string savepointName) =>
    Execute($"SAVEPOINT \"{EscapeIdentifier(savepointName)}\"");

public override void Rollback(string savepointName) =>
    Execute($"ROLLBACK TO SAVEPOINT \"{EscapeIdentifier(savepointName)}\"");

public override void Release(string savepointName) =>
    Execute($"RELEASE SAVEPOINT \"{EscapeIdentifier(savepointName)}\"");

public override bool SupportsSavepoints => true;
```
`TransactionScope`/`System.Transactions` enlistment è invece a **priorità bassa (P3)**: SQLite non ha un vero coordinatore a due fasi nativo, e la maggior parte degli scenari enterprise multi-database userebbe comunque SQLite come store locale non distribuito. Va documentato come limite di design esplicito (come già fatto per l'encryption), non implementato "a metà".

---

### 2.6 Resilienza: retry e classificazione errori transitori

**Stato attuale**: `SqliteException` esiste ma non risulta esposta una classificazione "errore transitorio vs permanente" (es. `IsTransient`), né un hook di retry pipeline.

**Come fanno gli altri**: SqlClient espone `SqlException.Errors` con codici e si integra con `Microsoft.Data.SqlClient.Retry` (retry provider configurabile per errori transitori come deadlock/connessione). Npgsql non ha un retry provider built-in ma documenta chiaramente quali `SqlState` sono transitori. Per SQLite l'equivalente naturale è `SQLITE_BUSY`/`SQLITE_LOCKED`: oggi il provider gestisce `busy_timeout` a livello nativo, ma non espone al consumer un modo standard per sapere "questo errore, se ritentato, potrebbe risolversi" — informazione preziosa per una policy Polly/`Microsoft.Extensions.Resilience` lato applicativo.

**Raccomandazione (P2)**:
```csharp
public sealed class SqliteException : DbException
{
    public bool IsTransient => ResultCode is Result.Busy or Result.Locked or Result.Interrupt;
    public Result ResultCode { get; }
    public Result ExtendedResultCode { get; }
}
```
Basta esporre `IsTransient` in modo che diventi immediatamente componibile con `Microsoft.Extensions.Resilience`/Polly (`.Handle<SqliteException>(e => e.IsTransient)`), senza che il provider debba implementare esso stesso una retry pipeline (scelta corretta: la retry policy è responsabilità applicativa, non del driver — coerente con quanto fanno Npgsql/MySqlConnector).

---

### 2.7 Bulk operations

**Stato attuale**: nessun equivalente di `SqlBulkCopy`/Npgsql `Import`/`Export` (`COPY`).

**Come fanno gli altri**: SqlClient (`SqlBulkCopy`), Npgsql (`NpgsqlBinaryImporter` via `COPY ... FROM STDIN BINARY`), Oracle (`OracleBulkCopy`) offrono un percorso a bypass del comando singolo per caricamenti massivi, con throughput ordini di grandezza superiori a INSERT riga-per-riga.

**Raccomandazione (P2)**: per SQLite l'equivalente più efficace non è un binary wire protocol (non esiste, è in-process) ma un helper che avvolge un pattern già ottimale a mano: singola transazione + statement preparato riusato + `sqlite3_bind_*` per riga, esposto come API di alto livello (`SqliteBulkInsert.WriteAsync(IAsyncEnumerable<T> rows, ...)`), così il consumer enterprise non deve reinventare il pattern "prepare once, bind N times" ogni volta. Valore soprattutto documentale/ergonomico più che di performance pura (che dipende dal motore SQLite stesso).

---

### 2.8 `GetSchema` e metadata di catalogo

**Stato attuale**: `GetSchemaTable()` a livello di reader è implementato (`ADONET_GAP_ANALYSIS.md` lo segnala risolto). Non risulta invece l'implementazione delle collection standard di `DbConnection.GetSchema(string collectionName)` (`MetaDataCollections`, `DataSourceInformation`, `DataTypes`, `Restrictions`, `ReservedWords`, `Tables`, `Columns`, `Indexes`, `ForeignKeys`).

**Come fanno gli altri**: sia SqlClient sia Npgsql sia Microsoft.Data.Sqlite implementano l'intero set standard: è quello che permette a tool come Visual Studio Server Explorer, DBeaver (via ADO.NET bridge), o generatori di codice (T4/scaffolding EF Core) di introspezionare lo schema senza query SQL ad-hoc.

**Raccomandazione (P1)**: implementare almeno `MetaDataCollections`, `DataSourceInformation`, `Tables`, `Columns`, `Indexes`, `ForeignKeys`, interrogando `sqlite_master`/`pragma_table_info`/`pragma_foreign_key_list`. È un lavoro meccanico ma indispensabile per l'integrazione con tooling e scaffolding — oggi è probabilmente il gap che più silenziosamente blocca l'adozione da parte di terzi (un ORM scaffolder non funziona senza).

---

### 2.9 Integrazione `Microsoft.Extensions.*` / DI / Health Checks

**Stato attuale**: nessun pacchetto companion `CiccioSoft.Data.Sqlite.Extensions.DependencyInjection` o simile.

**Come fanno gli altri**: `Npgsql.DependencyInjection`, `Microsoft.Data.SqlClient` (via `AddSqlServer` di Aspire), `MySqlConnector` con `AddMySqlDataSource` offrono `IServiceCollection.AddXxxDataSource(connectionString)` + `AspNetCore.HealthChecks.*` package dedicato che fa un `SELECT 1`/ping periodico esposto su `/health`.

**Raccomandazione (P2, ma alta visibilità "enterprise")**: pacchetto satellite leggero:
```csharp
services.AddSqliteDataSource(connectionString)
        .AddHealthChecks().AddCheck<SqliteHealthCheck>("sqlite");
```
Una volta introdotto `DbDataSource` (§2.1), questo diventa quasi gratuito.

---

### 2.10 Sicurezza: encryption at rest

**Stato attuale**: dichiarato esplicitamente come non supportato ("No encryption support" nel README). Scelta di design legittima, ma va confrontata col panorama enterprise.

**Come fanno gli altri**: Microsoft.Data.Sqlite si appoggia a `SQLCipher`/`SEE` se il binario nativo la include; molti provider enterprise SQLite (System.Data.SQLite) offrono `Password=` con AES. Nel contesto di `CiccioSoft.Data.Sqlite`, che ha già `Password` in `SqliteConnectionSettings` ma non collegata a un motore di cifratura reale (verificare), questo è un **gap di sicurezza esplicito da documentare come limite noto**, non necessariamente da colmare subito: dipende dal target (se il file `.db` non lascia mai un perimetro fidato, l'encryption at rest a livello applicativo/filesystem può bastare). Da chiarire nella threat model del prodotto piuttosto che implementare a metà.

---

## 3. Roadmap prioritizzata

| Priorità | Item | Motivazione | Sforzo stimato |
|---|---|---|---|
| **P0** | Fix `SqliteConnectionPool.Return` (Semaphore.Release mancante) | Deadlock in produzione sotto carico | Basso |
| **P0** | `SqliteDataSource : DbDataSource` + rimozione pool statico globale | Prerequisito per DI, multi-tenant, testabilità | Medio-Alto |
| **P0/P1** | `ActivitySource` + `EventSource`/`EventCounter` per pool e comandi | Senza questo, bug come il deadlock di §1 sono invisibili finché non esplodono | Medio |
| **P1** | `SqliteBatch : DbBatch` | Abilita ottimizzazioni EF Core SaveChanges e riduce round-trip logici | Medio |
| **P1** | Savepoint (`Save`/`Rollback`/`Release` su `DbTransaction`) | Nativo in SQLite, richiesto da EF Core resilient SaveChanges | Basso |
| **P1** | `GetSchema` standard collections | Blocca tooling/scaffolding di terze parti se assente | Medio |
| **P1** | Statement cache a livello di connessione (LRU) | Costo `Prepare` ripetuto in pattern ORM "usa e getta" | Medio |
| **P2** | `SqliteException.IsTransient` | Abilita retry policy applicative (Polly/Resilience) senza reinventare classificazione errori | Basso |
| **P2** | Pacchetto DI + Health Check | Adozione enterprise "a scaffale" (Aspire-style) | Basso (dopo P0 DataSource) |
| **P2** | Helper bulk insert (pattern prepare-once/bind-N) | Ergonomia per ETL/seeding massivo | Basso-Medio |
| **P3** | `TransactionScope`/`System.Transactions` enlistment | Valore incerto per SQLite locale; da valutare solo su richiesta esplicita | Alto |
| **P3** | Encryption at rest | Da trattare come decisione di threat-model, non come backlog item generico | — |

---

## 4. Conclusione

Sul piano della **correttezza del contratto ADO.NET**, `CiccioSoft.Data.Sqlite` è già maturo (vedi `ADONET_GAP_ANALYSIS.md`). Il salto verso "enterprise-grade" richiesto ora non è più nel contratto sincrono `DbCommand`/`DbDataReader`, ma in tre assi che i provider di riferimento (SqlClient, Npgsql, MySqlConnector, Oracle) hanno tutti convergentemente adottato negli ultimi anni:

1. **Possesso esplicito delle risorse** (`DbDataSource` al posto di pool statici globali) — abilita DI, isolamento nei test, multi-tenancy.
2. **Osservabilità nativa** (`Activity`/OpenTelemetry, `EventCounter`) — senza la quale bug di concorrenza come quello di §1 restano latenti fino al post-mortem in produzione.
3. **Superficie API allineata alle evoluzioni recenti di ADO.NET** (`DbBatch`, savepoint, `GetSchema` completo) — necessaria perché ORM e tooling di terze parti (in primis EF Core) possano trattare il provider come un pari dei provider maggiori, non come un'implementazione minimale del contratto.

Il fix del bug di pooling (§1) resta bloccante e va prima di tutto il resto; il resto della roadmap è ordinabile per priorità come in tabella, ma **`DbDataSource` è l'elemento abilitante** da cui gran parte degli altri item (telemetria per-istanza, DI, health check) dipende architetturalmente — conviene sequenziarlo presto anche se il suo sforzo individuale è più alto.
