// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.IO;

namespace CiccioSoft.Data.Sqlite;

internal sealed class SqliteConnectionSettings
{
    public const int DefaultTimeoutSeconds = 30;
    public const int DefaultMaxPoolSize = 100;
    public const bool DefaultPooling = true;
    public const string DefaultJournalMode = "WAL";
    public const bool DefaultForeignKeys = true;

    public string DataSource { get; }
    public SqliteOpenMode Mode { get; }
    public SqliteCacheMode Cache { get; }
    public string Password { get; }
    public bool ForeignKeys { get; }
    public bool? RecursiveTriggers { get; }
    public int DefaultTimeout { get; }
    public bool Pooling { get; }
    public int MaxPoolSize { get; }
    public string JournalMode { get; }
    public string? Vfs { get; }

    public bool IsInMemoryMode { get; }
    public bool HasJournalMode { get; }

    public SqliteConnectionSettings(SqliteConnectionStringBuilder builder)
    {
        DataSource = builder.DataSource ?? string.Empty;
        Mode = builder.Mode ?? SqliteOpenMode.ReadWriteCreate;
        Cache = builder.Cache ?? SqliteCacheMode.Default;
        Password = builder.Password ?? string.Empty;
        
        ForeignKeys = builder.ForeignKeys ?? DefaultForeignKeys;
        RecursiveTriggers = builder.RecursiveTriggers;
        DefaultTimeout = builder.DefaultTimeout ?? DefaultTimeoutSeconds;
        Pooling = builder.Pooling ?? DefaultPooling;
        MaxPoolSize = builder.MaxPoolSize ?? DefaultMaxPoolSize;
        Vfs = builder.Vfs;

        IsInMemoryMode = DetermineInMemoryMode(DataSource, builder.Mode);
        
        HasJournalMode = builder.JournalMode != null;
        JournalMode = IsInMemoryMode
            ? "DELETE"
            : (builder.JournalMode ?? DefaultJournalMode);
    }

    private static bool DetermineInMemoryMode(string dataSource, SqliteOpenMode? mode)
    {
        if (dataSource == ":memory:") return true;
        if (mode == SqliteOpenMode.Memory) return true;
        if (dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase) &&
            dataSource.Contains("mode=memory", StringComparison.OrdinalIgnoreCase))
            return true;
        return string.IsNullOrWhiteSpace(dataSource);
    }
}
