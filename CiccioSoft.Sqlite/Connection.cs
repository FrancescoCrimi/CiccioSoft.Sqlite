// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using CiccioSoft.Sqlite.Native;
using NativeConnection = CiccioSoft.Sqlite.Native.Connection;
using NativeStatement = CiccioSoft.Sqlite.Native.Statement;

namespace CiccioSoft.Sqlite;

public sealed class Connection : IDisposable
{
    private NativeConnection _native;

    public Connection(NativeConnection connection)
    {
        _native = connection;
    }

    public static Connection Open(string filename, OpenFlags flags, string? vfs = null)
    {
        NativeConnection connection = NativeConnection.Open(filename, flags, vfs);
        return new Connection(connection);
    }

    public void Execute(string sql)
    {
        _native.Execute(sql);
    }

    public Statement Prepare(string sql, PrepareFlags prepareFlags = PrepareFlags.None)
    {
        NativeStatement stmt = _native.Prepare(sql, prepareFlags);
        return new Statement(stmt, this);
    }

    public Statement? Prepare(string sql, int sqlByteOffset, out int nextSqlByteOffset, PrepareFlags prepareFlags = PrepareFlags.None)
    {
        NativeStatement? stmt = _native.Prepare(sql, sqlByteOffset, out nextSqlByteOffset, prepareFlags);
        return new Statement(stmt!, this);
    }

    public long LastInsertRowId()
    {
        return _native.LastInsertRowId();
    }

    public int Changes()
    {
        return _native.Changes();
    }

    public long TotalChanges()
    {
        return _native.TotalChanges();
    }

    public bool GetAutoCommit()
    {
        return _native.GetAutoCommit();
    }

    public int Limit(LimitCategory id, int newVal)
    {
        return _native.Limit(id, newVal);
    }

    public TransactionState TransactionState(string? schemaName = null)
    {
        return _native.TransactionState(schemaName);
    }

    public bool DbReadOnly(string databaseName = "main")
    {
        return _native.DbReadOnly(databaseName);
    }

    public ResultCode ErrorCode()
    {
        return _native.ErrorCode();
    }

    public ResultCode ExtendedErrorCode()
    {
        return _native.ExtendedErrorCode();
    }

    public string ErrorMessage()
    {
        return _native.ErrorMessage();
    }

    public static string ErrorString(ResultCode resultCode)
    {
        return NativeConnection.ErrorString(resultCode);
    }

    public int ErrorOffset()
    {
        return _native.ErrorOffset();
    }

    public void BusyTimeout(int milliseconds)
    {
        _native.BusyTimeout(milliseconds);
    }

    public void Interrupt()
    {
        _native.Interrupt();
    }

    public static string? LibVersion()
    {
        return NativeConnection.LibVersion();
    }

    public static int LibVersionNumber()
    {
        return NativeConnection.LibVersionNumber();
    }

    public Backup BackupInit(Connection destination,
                             string destinationDatabaseName = "main",
                             string sourceDatabaseName = "main")
    {
        var native = _native.BackupInit(destination._native, destinationDatabaseName, sourceDatabaseName);
        return new Backup(native);
    }

    public Blob BlobOpen(string tableName,
                         string columnName,
                         long rowId,
                         bool readWrite = false,
                         string databaseName = "main")
    {
        var native = _native.BlobOpen(tableName, columnName, rowId, readWrite, databaseName);
        return new Blob(native);
    }

    public void Dispose()
    {
        _native.Dispose();
    }
}