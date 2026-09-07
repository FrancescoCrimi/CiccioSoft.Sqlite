# Todo

## CiccioSoft.Sqlite.Native
1) Implementare classe Transaction wrapper verso native
2) CiccioSoft.Sqlite.Native.Statement.Step() non deve restituire bool ma resultCode e implementare 
   Step() che ritorna bool in CiccioSoft.Sqlite.Statement
3) Creare overload per tutti i metodi che prendono string come parametro con equivalenti che accettano
   span<byte> direttamente in in utf8.

## CiccioSoft.Sqlite
1) Verificare funzionalita ed helper dell'eccezzione
2) Implementare CiccioSoft.Sqlite.Statement.Step() che ritorni bool e ad errore generi eccezzione
3) CiccioSoft.Sqlite.Execute non deve usare sqlite3_exec ma deve usare Statement prepare e step
   per intercettare attravero statement se il comando sql è readonly o meno

## CiccioSoft.Data.Sqlite
1)	Aumentare la compatibilita a livello Api con Microsoft.Data.Sqlite
	per fare in modo che CiccioSoft.Data.Sqlite possa diventare drop-in replacement per 
	Microsoft.Data.Sqlite.
2)	Agiungere funzionalità CreateFunction e CreateAggregateCore magari copiando quello che serve 
	da Microsoft.Data.Sqlite anche perche tutti e due usano la stessa licenza MIT.
3)	Portare il livello di CiccioSoft.Data.Sqlite a livello di un provider adone di classe Enterprice
	e renderlo migliore di Microsoft.Data.Sqlite
4) 	Verificare funzionalità del SqliteConnectionPool e SqliteConnectionStringBuilder
5) 	Threading e async

## Creare CiccioSoft.Data.Sqlite.Bencmarck
1) Bencmarck tra Creare CiccioSoft.Data e Microsoft.Data.Sqlite
