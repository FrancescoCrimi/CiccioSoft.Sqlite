// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Diagnostics.CodeAnalysis;
using CiccioSoft.Data.Common;

namespace CiccioSoft.Data.Sqlite;

/// <summary>
/// Builds SQLite connection strings using CommonDbConnectionStringBuilder.
/// This acts as a pure configuration parser. Defaults are handled by SqliteConnection.
/// </summary>
public class SqliteConnectionStringBuilder : CommonDbConnectionStringBuilder
{
    // Typed option singletons – one per connection-string property.
    private readonly ConnectionStringOption<string?> DataSourceOption;
    private readonly ConnectionStringOption<SqliteOpenMode?> ModeOption;
    private readonly ConnectionStringOption<SqliteCacheMode?> CacheOption;
    private readonly ConnectionStringOption<string?> PasswordOption;
    private readonly ConnectionStringOption<bool?> ForeignKeysOption;
    private readonly ConnectionStringOption<bool?> RecursiveTriggersOption;
    private readonly ConnectionStringOption<int?> DefaultTimeoutOption;
    private readonly ConnectionStringOption<bool?> PoolingOption;
    private readonly ConnectionStringOption<int?> MaxPoolSizeOption;
    private readonly ConnectionStringOption<string?> JournalModeOption;
    private readonly ConnectionStringOption<string?> VfsOption;


    #region Ctor

    public SqliteConnectionStringBuilder() : base()
    {
        DataSourceOption = Register<string?>(["Data Source", "DataSource", "Filename"]);
        ModeOption = Register<SqliteOpenMode?>(["Mode"]);
        CacheOption = Register<SqliteCacheMode?>(["Cache"]);
        PasswordOption = Register<string?>(["Password"]);
        ForeignKeysOption = Register<bool?>(["Foreign Keys", "foreign_keys"]);
        RecursiveTriggersOption = Register<bool?>(["Recursive Triggers", "recursive_triggers"]);
        DefaultTimeoutOption = Register<int?>(["Default Timeout", "default_timeout", "Command Timeout"], val => val.HasValue ? Math.Max(0, val.Value) : null);
        PoolingOption = Register<bool?>(["Pooling"]);
        MaxPoolSizeOption = Register<int?>(["Max Pool Size", "MaxPoolSize"], val => val.HasValue ? Math.Max(1, val.Value) : null);
        JournalModeOption = Register<string?>(["Journal Mode", "journal_mode"]);
        VfsOption = Register<string?>(["Vfs"]);
    }

    public SqliteConnectionStringBuilder(string connectionString) : this()
    {
        if (!string.IsNullOrEmpty(connectionString))
        {
            ConnectionString = connectionString;
        }
    }

    #endregion


    #region Builder Property

    public string? DataSource
    {
        get => DataSourceOption.GetValue();
        set => DataSourceOption.SetValue(value);
    }

    public virtual SqliteOpenMode? Mode
    {
        get => ModeOption.GetValue();
        set => ModeOption.SetValue(value);
    }

    public virtual SqliteCacheMode? Cache
    {
        get => CacheOption.GetValue();
        set => CacheOption.SetValue(value);
    }

    [AllowNull]
    public string? Password
    {
        get => PasswordOption.GetValue();
        set => PasswordOption.SetValue(value);
    }

    public bool? ForeignKeys
    {
        get => ForeignKeysOption.GetValue();
        set => ForeignKeysOption.SetValue(value);
    }

    public bool? RecursiveTriggers
    {
        get => RecursiveTriggersOption.GetValue();
        set => RecursiveTriggersOption.SetValue(value);
    }

    public int? DefaultTimeout
    {
        get => DefaultTimeoutOption.GetValue();
        set => DefaultTimeoutOption.SetValue(value);
    }

    public bool? Pooling
    {
        get => PoolingOption.GetValue();
        set => PoolingOption.SetValue(value);
    }

    public string? Vfs
    {
        get => VfsOption.GetValue();
        set => VfsOption.SetValue(value);
    }

    public int? MaxPoolSize
    {
        get => MaxPoolSizeOption.GetValue();
        set => MaxPoolSizeOption.SetValue(value);
    }

    public string? JournalMode
    {
        get => JournalModeOption.GetValue();
        set => JournalModeOption.SetValue(value);
    }

    #endregion
}
