// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Text;
using Xunit;

namespace CiccioSoft.Sqlite.Native.Tests;

public unsafe sealed class StatementBindAndColumnTests
{
    [Fact]
    public void BindAndRead_AllScalarTypes_RoundTrip()
    {
        using var connection = TestDatabase.OpenMemory();
        connection.Execute("""
            CREATE TABLE sample (
                i INTEGER,
                l INTEGER,
                d REAL,
                t TEXT,
                b BLOB,
                n TEXT
            );
            """);

        using (var insert = connection.Prepare(
                   "INSERT INTO sample (i, l, d, t, b, n) VALUES (?, ?, ?, ?, ?, ?);"))
        {
            insert.BindInt(1, 42);
            insert.BindInt64(2, long.MaxValue);
            insert.BindDouble(3, 3.141592653589793);
            insert.BindText(4, "hello");
            insert.BindBlob(5, new byte[] { 0x01, 0x02, 0xFF });
            insert.BindNull(6);
            Assert.False(insert.Step());
        }

        using var select = connection.Prepare("SELECT i, l, d, t, b, n FROM sample;");
        Assert.True(select.Step());

        Assert.Equal(SqliteType.Integer, select.ColumnType(0));
        Assert.Equal(42, select.ColumnInt(0));
        Assert.Equal(long.MaxValue, select.ColumnInt64(1));
        Assert.Equal(3.141592653589793, select.ColumnDouble(2), precision: 10);
        Assert.Equal("hello", select.ColumnText(3));
        Assert.Equal(SqliteType.Text, select.ColumnType(3));

        ReadOnlySpan<byte> blob = select.ColumnBlob(4);
        Assert.Equal(new byte[] { 0x01, 0x02, 0xFF }, blob.ToArray());
        Assert.Equal(SqliteType.Blob, select.ColumnType(4));

        Assert.Equal(SqliteType.Null, select.ColumnType(5));
        Assert.Null(select.ColumnText(5));
        Assert.True(select.ColumnTextUtf8(5).IsEmpty);
        Assert.Equal(0, select.ColumnTextUtf8(5).Length);
        Assert.True(select.ColumnTextUtf8(5).IsNull);
        Assert.True(select.ColumnBlob(5).IsEmpty);
    }

    [Fact]
    public void BindText_Null_String()
    {
        using var connection = TestDatabase.OpenMemory();
        connection.Execute("CREATE TABLE t (v TEXT);");

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindText(1, (string)null!);
            insert.Step();
        }

        using var select = connection.Prepare("SELECT v IS NULL, typeof(v), length(v), v FROM t;");
        Assert.True(select.Step());

        Assert.Equal(1, select.ColumnInt(0));                              // SELECT IS NULL 1 (true)
        Assert.Equal("null", select.ColumnText(1), ignoreCase: true);      // SELECT typeof(v)
        Assert.Equal(0, select.ColumnInt(2));                              // SELECT length(v)

        Assert.Null(select.ColumnText(3));                                 // SELECT v
        Assert.Equal(SqliteType.Null, select.ColumnType(3));

        Utf8String utf8 = select.ColumnTextUtf8(3);                        // SELECT v as Span
        Assert.True(utf8.IsEmpty);
        Assert.Equal(0, utf8.Length);
        Assert.True(utf8.IsNull);
        Assert.Null(utf8.ToStringOrNull());
    }

    [Fact]
    public void BindText_Empty_String()
    {
        using var connection = TestDatabase.OpenMemory();
        connection.Execute("CREATE TABLE t (v TEXT);");

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindText(1, string.Empty);
            insert.Step();
        }

        using var select = connection.Prepare("SELECT v IS NULL, typeof(v), length(v), v FROM t;");
        Assert.True(select.Step());

        Assert.Equal(0, select.ColumnInt(0));                              // SELECT IS NULL 0 (false)
        Assert.Equal("text", select.ColumnText(1), ignoreCase: true);      // SELECT typeof(v)
        Assert.Equal(0, select.ColumnInt(2));                              // SELECT length(v)

        Assert.Equal(string.Empty, select.ColumnText(3));                  // SELECT v
        Assert.Equal(SqliteType.Text, select.ColumnType(3));

        Utf8String utf8 = select.ColumnTextUtf8(3);                        // SELECT v as Span
        Assert.True(utf8.IsEmpty);
        Assert.Equal(0, utf8.Length);
        Assert.False(utf8.IsNull);
        Assert.Equal(string.Empty, utf8.ToStringOrNull());
    }

    [Fact]
    public void BindText_Empty_Span()
    {
        using var connection = TestDatabase.OpenMemory();
        connection.Execute("CREATE TABLE t (v TEXT);");

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindText(1, ReadOnlySpan<byte>.Empty);
            insert.Step();
        }

        using var select = connection.Prepare("SELECT v IS NULL, typeof(v), length(v), v FROM t;");
        Assert.True(select.Step());

        Assert.Equal(0, select.ColumnInt(0));                              // SELECT IS NULL 0 (false)
        Assert.Equal("text", select.ColumnText(1), ignoreCase: true);      // SELECT typeof(v)
        Assert.Equal(0, select.ColumnInt(2));                              // SELECT length(v)

        Assert.Equal(string.Empty, select.ColumnText(3));                  // SELECT v
        Assert.Equal(SqliteType.Text, select.ColumnType(3));

        Utf8String utf8 = select.ColumnTextUtf8(3);                        // SELECT v as Span
        Assert.True(utf8.IsEmpty);
        Assert.Equal(0, utf8.Length);
        Assert.False(utf8.IsNull);
        Assert.Equal(string.Empty, utf8.ToStringOrNull());
    }

    // [Fact]
    // public void BindText_Span_With_Null()
    // {
    //     // ReadOnlySpan<T>.Empty is alias of default: GetReference is a null ref, so the API
    //     // cannot distinguish "missing" from "Empty". Enterprise contract: both → SQL NULL.
    //     // Real zero-length payloads must come from a non-default empty span (see next test).
    //     using var connection = ConnectionFactory.OpenMemory();
    //     connection.Execute("CREATE TABLE t (v TEXT);");

    //     using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
    //     {
    //         ReadOnlySpan<byte> aaaa = "\0"u8;
    //         insert.BindText(1, aaaa);
    //         insert.Step();
    //     }

    //     using var select = connection.Prepare("SELECT v IS NULL, typeof(v), length(v), v FROM t;");
    //     Assert.True(select.Step());

    //     Assert.Equal(0, select.GetInt(0));                              // 0 IS NOT NULL
    //     Assert.Equal("text", select.GetText(1), ignoreCase: true);      // typeof(v)
    //     Assert.Equal(0, select.GetInt(2));                              // length(v)

    //     Assert.Equal(string.Empty, select.GetText(3));                  // v
    //     Assert.Equal(SqliteType.Text, select.GetColumnType(3));         // v

    //     Assert.True(select.GetTextUtf8(3).IsEmpty);                     // v as Span
    //     Assert.Equal(0, select.GetTextUtf8(3).Length);                  // v as Span
    //     Assert.False(select.GetTextUtf8(3).IsNull);                     // v as Span
    // }

    // [Fact]
    // public void BindText_RealEmptySpan_BindsEmptyTextNotSqlNull()
    // {
    //     using var connection = ConnectionFactory.OpenMemory();
    //     connection.Execute("CREATE TABLE t (v TEXT);");

    //     Span<byte> scratch = stackalloc byte[1];
    //     ReadOnlySpan<byte> realEmpty = scratch[..0];

    //     using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
    //     {
    //         insert.BindText(1, realEmpty);
    //         insert.Step();
    //     }

    //     using var select = connection.Prepare("SELECT v IS NULL, typeof(v), length(v), v FROM t;");
    //     Assert.True(select.Step());

    //     Assert.Equal(1, select.GetInt(0));
    //     Assert.Equal("null", select.GetText(1), ignoreCase: true);
    //     Assert.Equal(0, select.GetInt(2));

    //     Assert.Null(select.GetText(3));
    //     Assert.Equal(SqliteType.Null, select.GetColumnType(3));

    //     Assert.True(select.GetTextUtf8Span(3).IsEmpty);
    //     Assert.Equal(0, select.GetTextUtf8Span(3).Length);
    // }

    [Fact]
    public void BindBlob_DefaultSpan_BindsSqlNull()
    {
        using var connection = TestDatabase.OpenMemory();
        connection.Execute("CREATE TABLE t (v BLOB);");

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindBlob(1, default);
            insert.Step();
        }

        using var select = connection.Prepare("SELECT v IS NULL FROM t;");
        Assert.True(select.Step());
        Assert.Equal(1, select.ColumnInt(0));
    }

    [Fact]
    public void BindBlob_EmptyStaticSpan_BindsSqlNull_BecauseIndistinguishableFromDefault()
    {
        using var connection = TestDatabase.OpenMemory();
        connection.Execute("CREATE TABLE t (v BLOB);");

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindBlob(1, ReadOnlySpan<byte>.Empty);
            insert.Step();
        }

        using var select = connection.Prepare("SELECT v IS NULL FROM t;");
        Assert.True(select.Step());
        Assert.Equal(1, select.ColumnInt(0));
    }

    [Fact]
    public void BindBlob_RealEmptySpan_BindsEmptyBlobNotSqlNull()
    {
        using var connection = TestDatabase.OpenMemory();
        connection.Execute("CREATE TABLE t (v BLOB);");

        Span<byte> scratch = stackalloc byte[1];
        ReadOnlySpan<byte> realEmpty = scratch[..0];

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindBlob(1, realEmpty);
            insert.Step();
        }

        using var select = connection.Prepare("SELECT v IS NULL, typeof(v), length(v), v FROM t;");
        Assert.True(select.Step());
        Assert.Equal(1, select.ColumnInt(0));
        Assert.Equal("null", select.ColumnText(1), ignoreCase: true);
        Assert.Equal(0, select.ColumnInt(2));
        Assert.Equal(SqliteType.Null, select.ColumnType(3));
        Assert.True(select.ColumnBlob(3).IsEmpty);
    }

    [Fact]
    public void BindText_EmptyThenNonEmpty_RoundTripsWithoutCrossContamination()
    {
        using var connection = TestDatabase.OpenMemory();
        connection.Execute("CREATE TABLE t (v TEXT);");

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindText(1, string.Empty);
            insert.Step();
            insert.Reset();
            insert.ClearBindings();

            insert.BindText(1, "after-empty");
            insert.Step();
        }

        using var select = connection.Prepare("SELECT v FROM t ORDER BY rowid;");
        Assert.True(select.Step());
        Assert.Equal(string.Empty, select.ColumnText(0));
        Assert.True(select.Step());
        Assert.Equal("after-empty", select.ColumnText(0));
        Assert.False(select.Step());
    }

    [Fact]
    public void BindText_NonEmptyUtf8Span_RoundTrips()
    {
        using var connection = TestDatabase.OpenMemory();
        connection.Execute("CREATE TABLE t (v TEXT);");

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindText(1, "ok"u8);
            insert.Step();
        }

        using var select = connection.Prepare("SELECT v FROM t;");
        Assert.True(select.Step());
        Assert.Equal("ok", select.ColumnText(0));
    }

    [Fact]
    public void NamedParameters_ResolveByNameAndIndex()
    {
        using var connection = TestDatabase.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER, name TEXT);");

        using var insert = connection.Prepare("INSERT INTO t (id, name) VALUES (@id, :name);");
        Assert.Equal(2, insert.BindParameterCount());
        Assert.Equal("@id", insert.BindParameterName(1));
        Assert.Equal(":name", insert.BindParameterName(2));

        Assert.Equal(1, insert.BindParameterIndex("@id"));
        Assert.Equal(2, insert.BindParameterIndex(":name"));
        Assert.Equal(0, insert.BindParameterIndex(":missing"));

        ReadOnlySpan<byte> nameBytes = insert.BindParameterNameUtf8(1);
        Assert.Equal("@id"u8, nameBytes);

        insert.BindInt(insert.BindParameterIndex("@id"), 9);
        insert.BindText(insert.BindParameterIndex(":name"), "Ada");
        insert.Step();

        using var select = connection.Prepare("SELECT id, name FROM t;");
        Assert.True(select.Step());
        Assert.Equal(9, select.ColumnInt(0));
        Assert.Equal("Ada", select.ColumnText(1));
    }

    [Fact]
    public void AnonymousParameter_GetParameterName_ReturnsEmptyOrNull()
    {
        using var connection = TestDatabase.OpenMemory();
        using var stmt = connection.Prepare("SELECT ?;");

        Assert.Equal(1, stmt.BindParameterCount());
        Assert.Null(stmt.BindParameterName(1));
        Assert.True(stmt.BindParameterNameUtf8(1).IsEmpty);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Bind_InvalidIndex_ThrowsArgumentOutOfRange(int index)
    {
        using var connection = TestDatabase.OpenMemory();
        using var stmt = connection.Prepare("SELECT ?;");

        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.BindInt(index, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.BindInt64(index, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.BindDouble(index, 1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.BindText(index, "x"));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.BindBlob(index, new byte[] { 1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.BindNull(index));
    }

    [Theory]
    [InlineData(-1)]
    public void ColumnAccessors_NegativeIndex_Throw(int index)
    {
        using var connection = TestDatabase.OpenMemory();
        using var stmt = connection.Prepare("SELECT 1 AS n;");
        Assert.True(stmt.Step());

        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.ColumnInt(index));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.ColumnInt64(index));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.ColumnDouble(index));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.ColumnText(index));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.ColumnTextUtf8(index));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.ColumnBlob(index));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.ColumnType(index));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.ColumnName(index));
    }

    [Fact]
    public void ColumnMetadata_NamesAndOrigins_ArePopulated()
    {
        using var connection = TestDatabase.OpenMemory();
        connection.Execute("CREATE TABLE people (person_id INTEGER, full_name TEXT);");
        connection.Execute("INSERT INTO people VALUES (1, 'Grace');");

        using var stmt = connection.Prepare("SELECT person_id AS id, full_name FROM people;");
        Assert.Equal(2, stmt.ColumnCount());
        Assert.Equal("id", stmt.ColumnName(0));
        Assert.Equal("full_name", stmt.ColumnName(1));
        Assert.Equal("INTEGER", stmt.ColumnDeclType(0), ignoreCase: true);
        Assert.Equal("TEXT", stmt.ColumnDeclType(1), ignoreCase: true);
        Assert.Equal("main", stmt.ColumnDatabaseName(0), ignoreCase: true);
        Assert.Equal("people", stmt.ColumnTableName(0), ignoreCase: true);
        Assert.Equal("person_id", stmt.ColumnOriginName(0), ignoreCase: true);
        Assert.Equal("full_name", stmt.ColumnOriginName(1), ignoreCase: true);
    }

    [Fact]
    public void GetText_ReturnsUtf8SpanMatchingString()
    {
        using var connection = TestDatabase.OpenMemory();
        using var stmt = connection.Prepare("SELECT 'café ☕';");
        Assert.True(stmt.Step());

        string? asString = stmt.ColumnText(0);
        ReadOnlySpan<byte> asSpan = stmt.ColumnTextUtf8(0).Value;

        Assert.Equal("café ☕", asString);
        Assert.Equal(Encoding.UTF8.GetBytes("café ☕"), asSpan.ToArray());
    }

    [Fact]
    public void GetParameterIndex_EmptyName_Throws()
    {
        using var connection = TestDatabase.OpenMemory();
        using var stmt = connection.Prepare("SELECT @a;");

        Assert.Throws<ArgumentException>(() => stmt.BindParameterIndex(""));
        Assert.Throws<ArgumentException>(() => stmt.BindParameterIndex(null!));
    }

    [Fact]
    public void GetParameterName_InvalidIndex_Throws()
    {
        using var connection = TestDatabase.OpenMemory();
        using var stmt = connection.Prepare("SELECT ?;");

        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.BindParameterNameUtf8(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.BindParameterName(0));
    }
}
