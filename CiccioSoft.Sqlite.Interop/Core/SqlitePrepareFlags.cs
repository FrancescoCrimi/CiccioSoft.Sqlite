// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

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