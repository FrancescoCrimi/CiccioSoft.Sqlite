# Argomenti da Approfondire



## SkipLocalsInit

Questo attributo per classi o i metodi P/Invoke evita che .NET azzeri la memoria dello stack ad ogni chiamata, risparmiando preziosi cicli di clock.

Prima di applicare [SkipLocalsInit] effettuare una revisione chirurgica del codice.
La regola fondamentale è: ogni singolo byte allocato nello stack deve essere scritto prima di essere letto.

Ecco la checklist definitiva di cosa verificare nel tuo codice prima di attivare l'attributo:

##### 1. Verifica i blocchi stackalloc (Il punto più critico)
Assicurati che lo spazio allocato per le stringhe (percorsi file, query) venga interamente sovrascritto o che l'accesso ai dati sia rigorosamente limitato alla porzione scritta.

- ❌ **ERRORE (Lettura di memoria sporca):**
```csharp
byte* buffer = stackalloc byte[100]; // Contiene spazzatura
int scritti = Encoding.UTF8.GetBytes(sql, new Span<byte>(buffer, 100));
// Se passi 'buffer' direttamente a una funzione che si aspetta 100 byte, 
// leggerà la query + la spazzatura rimasta dopo i byte scritti!
```

- ✔️ **CORRETTO (Isolamento tramite Span):**
```csharp
byte* buffer = stackalloc byte[100];
int scritti = Encoding.UTF8.GetBytes(sql, new Span<byte>(buffer, 100));
// Crei uno Span limitato SOLO ai byte che hai effettivamente scritto.
// La spazzatura oltre l'indice 'scritti' viene totalmente ignorata.
ReadOnlySpan<byte> queryPulita = new ReadOnlySpan<byte>(buffer, scritti);
```

##### 2. Controlla i terminatori di stringa (\0) per il C

Le stringhe in C (come quelle richieste dalle API di SQLite) devono finire con il carattere null 0. Senza [SkipLocalsInit], la memoria rimanente è zero, quindi il terminatore c'è sempre. Con [SkipLocalsInit], devi metterlo tu esplicitamente a mano nel punto esatto.

- ✔️ **CORRETTO:**
```csharp
int byteCount = Encoding.UTF8.GetByteCount(sql);
byte* buffer = stackalloc byte[byteCount + 1]; // +1 per il terminatore
Encoding.UTF8.GetBytes(sql, new Span<byte>(buffer, byteCount));

buffer[byteCount] = 0; // <-- OBBLIGATORIO: devi scriverlo esplicitamente!
```

##### 3. Inizializzazione immediata di tutte le variabili locali
Controlla tutte le variabili primitive (int, nint, bool, puntatori) dichiarate nei metodi sotto l'effetto di [SkipLocalsInit]. Non devi mai dichiarare una variabile senza assegnarle un valore iniziale sulla stessa riga.

- ❌ **ERRORE:**
```csharp
int risultato; // Contiene un numero casuale residuo dello stack
if (condizione) {
    risultato = sqlite3_open(...);
}
return risultato; // Se 'condizione' era falsa, ritorni spazzatura
```

- ✔️ **CORRETTO:**
```csharp
int risultato = 0; // Inizializzazione esplicita immediata
if (condizione) {
    risultato = sqlite3_open(...);
}
return risultato;
```

##### 4. Attenzione ai parametri out delle P/Invoke native
Quando passi una variabile come out a una P/Invoke (es. out NativeNew.sqlite3* ppDb), la funzione C scrive dentro quella memoria. Tuttavia, se la funzione C fallisce prima di scrivere nel puntatore, quel valore potrebbe rimanere inalterato.
- ✔️ **CORRETTO:**  
Inizializza la variabile prima di passarla se usi out in contesti ad alto rischio, oppure assicurati che la P/Invoke modifichi sempre il valore in ogni ramo di esecuzione del codice C (cosa che SQLite fa, ma è buona norma verificare).



## SuppressGCTransition
 
Visto che punti alle performance assolute, hai già preso in considerazione l'uso dell'attributo [SuppressGCTransition] sulle tue P/Invoke?
 Per funzioni ultra-rapide come quelle di SQLite può raddoppiare la velocità della chiamata. Ti interessa approfondire questo dettaglio?



## Puntatori opachi

Il modo standard e più elegante per risolvere il problema dei puntatori opachi nel mondo gestito è creare una readonly struct vuote con lo stesso nome del puntatore opaco non gestito che wrappano il puntatore.
