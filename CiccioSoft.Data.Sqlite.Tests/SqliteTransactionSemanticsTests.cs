// Copyright (c) CiccioSoft.
// Licensed under the MIT License.
using System;
using System.Data;
using CiccioSoft.Data.Sqlite;
using CiccioSoft.Data.Sqlite.Properties;
using Xunit;

namespace CiccioSoft.Data.Sqlite.Tests;

public class SqliteTransactionSemanticsTests
{
    [Fact]
    public void BeginTransaction_DefaultsToSerializable()
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        using SqliteTransaction transaction = (SqliteTransaction)connection.BeginTransaction();

        Assert.Equal(IsolationLevel.Serializable, transaction.IsolationLevel);
    }

    [Theory]
    [InlineData(IsolationLevel.ReadCommitted)]
    [InlineData(IsolationLevel.RepeatableRead)]
    [InlineData(IsolationLevel.Unspecified)]
    public void BeginTransaction_NormalizesIsolationLevel(IsolationLevel requested)
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        using SqliteTransaction transaction = (SqliteTransaction)connection.BeginTransaction(requested);

        Assert.Equal(IsolationLevel.Serializable, transaction.IsolationLevel);
    }

    [Fact]
    public void BeginTransaction_KeepsReadUncommitted()
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        using SqliteTransaction transaction = (SqliteTransaction)connection.BeginTransaction(IsolationLevel.ReadUncommitted);

        Assert.Equal(IsolationLevel.ReadUncommitted, transaction.IsolationLevel);
    }

    [Theory]
    [InlineData(IsolationLevel.Chaos)]
    [InlineData(IsolationLevel.Snapshot)]
    public void BeginTransaction_ThrowsForUnsupportedLevels(IsolationLevel isolationLevel)
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        ArgumentException ex = Assert.Throws<ArgumentException>(() => connection.BeginTransaction(isolationLevel));

        Assert.Equal(Resources.InvalidIsolationLevel(isolationLevel), ex.Message);
    }

    [Fact]
    public void Commit_ThrowsTransactionCompletedAfterDispose()
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        SqliteTransaction transaction = (SqliteTransaction)connection.BeginTransaction();
        transaction.Dispose();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => transaction.Commit());

        Assert.Equal(Resources.TransactionCompleted, ex.Message);
    }

    [Fact]
    public void Rollback_ThrowsTransactionCompletedAfterCommit()
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        SqliteTransaction transaction = (SqliteTransaction)connection.BeginTransaction();
        transaction.Commit();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => transaction.Rollback());

        Assert.Equal(Resources.TransactionCompleted, ex.Message);
    }
}
