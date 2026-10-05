// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using NativeBlob = CiccioSoft.Sqlite.Native.Blob;

namespace CiccioSoft.Sqlite;

public class Blob : IDisposable
{
    NativeBlob _native;

    public Blob(NativeBlob blob)
    {
        _native = blob;
    }

    public void Dispose()
    {
        _native.Dispose();
    }
}