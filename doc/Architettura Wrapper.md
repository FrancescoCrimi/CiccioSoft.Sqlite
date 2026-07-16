# 📋 Linee Guida Architetturali: Wrapper SQLite Multithread & Cross-Platform (.NET / C)

Questo documento riassume le decisioni di design prese per garantire le massime prestazioni, l'azzeramento delle allocazioni di memoria non necessarie e la totale portabilità del database tra diversi Sistemi Operativi (Windows, Linux, macOS).

---

## 1. Codifica Nativa del Database (Storage)
* **Decisione:** Il file del database SQLite sarà memorizzato su disco **esclusivamente in formato UTF-8**.
* **Motivazione:** UTF-8 è lo standard universale del software moderno (Rust, Python, Go, sistemi Linux/macOS). Riduce lo spazio su disco per i caratteri occidentali rispetto all'UTF-16 e garantisce che il file `.db` sia nativamente portabile su qualsiasi piattaforma senza conversioni di file.
* **Implementazione:** All'apertura/creazione del database si utilizzerà esclusivamente l'API `sqlite3_open()` e verrà forzato il comando `PRAGMA encoding = "UTF-8";` (se il DB è vuoto). **Le API UTF-16 di SQLite (`sqlite3_*16`) non verranno utilizzate.**

---

## 2. Separazione dei Ruoli e Performance (Threading)
* **Decisione:** SQLite non deve occuparsi delle conversioni di codifica del testo. Qualsiasi conversione di stringhe deve avvenire interamente all'interno dell'applicazione gestita (C#).
* **Motivazione:** 
  1. Facendo fare la conversione a SQLite, quest'ultimo dovrebbe allocare memoria extra nella Heap interna, impattando sulle performance.
  2. Le conversioni effettuate nel codice gestito (C#) possono essere eseguite in parallelo su più thread dell'applicazione. In questo modo, quando i dati arrivano a SQLite, sono già pronti per la scrittura, riducendo al minimo il tempo di blocco dei *mutex* interni del database in un'ottica concorrente.

---

## 3. Dove implementare la conversione dei tipi (.NET vs C/C++)
* **Decisione:** La conversione tra UTF-16 (stringhe native .NET) e UTF-8 verrà eseguita interamente sul lato **Managed (C#)**, e non nel layer intermedio in C/C++.
* **Motivazione:** Il framework .NET gestisce Unicode in modo nativo ed estremamente ottimizzato (con istruzioni hardware vettoriali SIMD). Al contrario, il C standard non ha funzioni cross-platform performanti per questo scopo, e il C++ moderno ha deprecato i vecchi convertitori (`std::codecvt`), rendendo la gestione cross-platform in C/C++ complessa e incline a bug o memory leak.

---

## 4. Design dell'Interfaccia del Wrapper (Pattern di Overload)
* **Decisione:** Per ogni funzione di interazione con il database che accetta testo, verranno forniti **due overload speculari**: uno basato su `ReadOnlySpan<byte>` (UTF-8 nativo) e uno basato su `String` (UTF-16 di comodità).

### Schema logico del codice:

1. **Il metodo CORE (`ReadOnlySpan<byte>`):**
   * Accetta testo in formato UTF-8 nativo.
   * Rappresenta la via ad alte prestazioni (zero allocazioni nella Heap, zero copie).
   * Ideale per dati che arrivano già in UTF-8 (es. parsing JSON, file di log, configurazioni, stringhe letterali con suffisso `u8`).
   * Passa direttamente il puntatore di byte al layer C tramite P/Invoke.

2. **L'metodo di COMODITÀ (`String`):**
   * Accetta le stringhe standard di .NET (UTF-16).
   * Calcola la dimensione necessaria e converte il testo in un buffer UTF-8 allocato temporaneamente nello **Stack** (`stackalloc byte[]`) per stringhe di piccole/medie dimensioni, azzerando l'impatto sul Garbage Collector.
   * Invoca internamente il metodo CORE passando il buffer ottenuto.

---

## 5. Ruolo del Layer in C
* **Decisione:** Il layer nativo in C/C++ agirà puramente come un **passacarte sottile** (*thin wrapper*).
* **Motivazione:** Riducendo al minimo la logica nel codice nativo, si semplifica drasticamente la manutenzione del codice. Il layer C riceverà esclusivamente puntatori a caratteri UTF-8 (`const char*`) pronti all'uso e li girerà direttamente alle funzioni standard di SQLite (`sqlite3_bind_text`, `sqlite3_prepare_v2`, ecc.).
