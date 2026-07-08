# Todo

## readonly struct vuote per puntatori opachi

## Splittare CiccioSoft.Sqlite.Interop
Bisogna CiccioSoft.Sqlite.Interop in due singoli progetti  

##### Solo interop (nome da stabilire)  
Questo progetto deve solo contenere interoperabilità verso Sqlite ed si ispira ad SQLitePCL.raw 
ed non è pensato per 'luso diretto da parte dei programmatori, lo scopo di questa libreria e il solo ed esclusiono livello di interoperabilità minimo,
un sottilissimo wrapper per C# dell'Api di Sqlite ideomatico e OOP
- mantenere un design OOP,  
- La terminologia e i nomi sono il più possibile fedeli all'API di SQLite, ma adattati alle Best Practices del C#.  
- gestire e nascondere i puntatori sottostanti,  
- gestire il rilascio della memeria attraverso i puntatori e i safehandle.  
- no controllo errori, solo ritorno il numero di ritorno di Sqlite  
- no eccezzioni, se ne occuperanno i livelli superiori

Attualmente il progetto CiccioSoft.Sqlite.Interop.Light rispecchia queste caratteristiche ma il nome non è ancora deciso.
Decidere se toglire completamente funzionalità String e gestire solo ReadOnlySpan<Byte> e lasciare la gestione String aha Libelli supriori

##### Libreia per la gestione di Sqlite (CiccioSoft.Sqlite.Interop)
Questo progetto vuole essere una completa libreria per interfacciarsi nel migliore dei moddi e in maniera piu veloce possibile e performante
ad SQLite e si ispira a SQLitePCL.Ugly
- Questa libreia non è un provider dotnet, CiccioSoft.Data.Sqlite è un vero provider dotnet.
- Chi utilizza questa libreria non deve gestire puntatori, strutture opache o altri elementi tipici della programmazione in C.
- Per la gestione del test la libreria deve accettare restituire sia String che ReadOnlySpan<Byte>

##### Modifica di CiccioSoft.Data.Sqlite
Bisogna modificare CiccioSoft.Data.Sqlite per far in modo che non utilizzi piu CiccioSoft.Sqlite.Interop
ma utilizzi la nuova CiccioSoft.Sqlite.Interop.Light e quindi bisogna spostare tutti i controlli e la gestione delle eccezzioni
 da CiccioSoft.Sqlite.Interop a CiccioSoft.Data.Sqlite in modo da non lasciare alcuna logica in CiccioSoft.Sqlite.Interop.Light che deve essere solo Glue Interop verso SQlite
 

## Nomi delle librerie

##### Solo glue Api per Sqlite
CiccioSoft.Sqlite.Interop.Light  
maybe CiccioSoft.Sqlite.Native ??? CiccioSoft.Sqlite.Glue
##### Sqlite Interop
CiccioSoft.Sqlite.Interop
##### Sqlite AdoNet Provider
CiccioSoft.Data.Sqlite


## String, Span e puntatori

### Conversione da puntatore byte* a Span.

#### Se non si conosce la lunghezza

Da puntatore byte* a span **senza copiare** i dati (Wrapping):
```
var span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ptr);
```

Da puntatore byte* a span **copiando** i dati (Persistenza):
```
var temp = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ptr);
var copiaSpan = new byte[temp.Length].AsSpan();
temp.CopyTo(copiaSpan);
```

#### Se si conosce la lunghezza

Da puntatore a byte* a span **senza copiare** i dati (Wrapping):
```
var span = new ReadOnlySpan<byte>(ptr, length);
```

Da puntatore a byte* a span **copiando** i dati (Persistenza):
```
var copiaSpan = new byte[length].AsSpan();
new ReadOnlySpan<byte>(ptr, length).CopyTo(copiaSpan);
```


### Conversione da puntatore byte* a String.

#### Se non si conosce la lunghezza
```
string testo = Marshal.PtrToStringUTF8((IntPtr)ptr);
```

#### Se si conosce la lunghezza
```
var span = new ReadOnlySpan<byte>(ptr, length);
var string = new String(span);
```
