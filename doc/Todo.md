# Todo

## Nomi delle librerie

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
