using System;

namespace CiccioSoft.Sqlite.Interop;

[Flags]
public enum SqlitePrepareFlags
{
    None = 0,
    Persistent = 0x01,
    Normalize = 0x02,
    NoVtab = 0x04
}