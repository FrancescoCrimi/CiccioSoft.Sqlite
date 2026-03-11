// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;

namespace CiccioSoft.Sqlite.Interop;

[Flags]
public enum SqliteOpenFlags
{
    ReadOnly = 0x00000001,
    ReadWrite = 0x00000002,
    Create = 0x00000004,
    Uri = 0x00000040,
    Memory = 0x00000080,
    NoMutex = 0x00008000,
    FullMutex = 0x00010000,
    SharedCache = 0x00020000,
    PrivateCache = 0x00040000
}
