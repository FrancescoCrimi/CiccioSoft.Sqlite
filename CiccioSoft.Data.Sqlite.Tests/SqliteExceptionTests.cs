// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2026 Francesco Crimi
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using Xunit;

namespace CiccioSoft.Data.Sqlite;

public class SqliteExceptionTests
{
    [Fact]
    public void Ctor_sets_message_and_errorCode()
    {
        var ex = new SqliteException("test", 1);

        Assert.Equal("test", ex.Message);
        Assert.Equal(1, ex.SqliteErrorCode);
        Assert.Equal(1, ex.SqliteExtendedErrorCode);
    }

    [Fact]
    public void Ctor_sets_extendedErrorCode()
    {
        var ex = new SqliteException("test", 1, 2);

        Assert.Equal("test", ex.Message);
        Assert.Equal(1, ex.SqliteErrorCode);
        Assert.Equal(2, ex.SqliteExtendedErrorCode);
    }

    // [Theory, InlineData(SQLITE_OK), InlineData(SQLITE_ROW), InlineData(SQLITE_DONE)]
    // public void ThrowExceptionForRC_does_nothing_when_non_error(int rc)
    //     => SqliteException.ThrowExceptionForRC(rc, null);
}
