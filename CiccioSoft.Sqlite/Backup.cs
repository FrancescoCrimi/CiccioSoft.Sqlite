// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using NativeBackup = CiccioSoft.Sqlite.Native.Backup;

namespace CiccioSoft.Sqlite;

public class Backup : IDisposable
{
    NativeBackup _native;

    public Backup(NativeBackup native)
    {
        _native = native;
    }

    public void Dispose()
    {
        _native.Dispose();
    }
}