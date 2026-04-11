// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2026 Francesco Crimi
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using Xunit;

namespace CiccioSoft.Data.Sqlite.Tests;

public class SqliteFactoryTests
{
    [Fact]
    public void CreateConnection_works()
        => Assert.IsType<SqliteConnection>(SqliteFactory.Instance.CreateConnection());

    [Fact]
    public void CreateConnectionStringBuilder_works()
        => Assert.IsType<SqliteConnectionStringBuilder>(SqliteFactory.Instance.CreateConnectionStringBuilder());

    [Fact]
    public void CreateCommand_works()
        => Assert.IsType<SqliteCommand>(SqliteFactory.Instance.CreateCommand());

    [Fact]
    public void CreateParameter_works()
        => Assert.IsType<SqliteParameter>(SqliteFactory.Instance.CreateParameter());
}
