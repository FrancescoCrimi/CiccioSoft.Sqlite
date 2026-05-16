// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace CiccioSoft.Data.Sqlite;

/// <summary>
/// Builds SQLite connection strings with intelligent defaults:
/// - WAL enabled by default (better concurrency)
/// - Foreign Keys enabled by default (referential integrity)
/// - For in-memory databases: Shared cache by default, WAL disabled
/// </summary>
public class SqliteConnectionStringBuilder : DbConnectionStringBuilder
{
    private const string DataSourceKey = "Data Source";
    private const string DataSourceNoSpaceKeyword = "DataSource";
    private const string ModeKey = "Mode";
    private const string CacheKey = "Cache";
    private const string ForeignKeysKey = "Foreign Keys";
    private const string ForeignKeysPragmaKey = "foreign_keys";
    private const string RecursiveTriggersKey = "Recursive Triggers";
    private const string RecursiveTriggersPragmaKey = "recursive_triggers";
    private const string BusyTimeoutKey = "Busy Timeout";
    private const string BusyTimeoutPragmaKey = "busy_timeout";
    private const string PoolingKey = "Pooling";
    private const string MaxPoolSizeKey = "Max Pool Size";
    private const string JournalModeKey = "Journal Mode";
    private const string JournalModePragmaKey = "journal_mode";

    private static readonly string[] CanonicalKeys =
    [
        DataSourceKey,
        ModeKey,
        CacheKey,
        //PasswordKey,
        ForeignKeysKey,
        RecursiveTriggersKey,
        BusyTimeoutKey,
        PoolingKey,
        //VfsKey,
        MaxPoolSizeKey,
        JournalModeKey,
    ];

    /// <summary>
    /// Initializes a new instance with intelligent defaults:
    /// - Journal Mode: WAL (better concurrency)
    /// - Foreign Keys: ON (referential integrity)
    /// </summary>
    public SqliteConnectionStringBuilder()
    {
        base[DataSourceKey] = string.Empty;
        base[PoolingKey] = true;
        base[MaxPoolSizeKey] = 100;
        base[BusyTimeoutKey] = 30000;
        // No default for JournalMode and ForeignKeys here; defaults will be applied by SqliteConnection
    }

    public SqliteConnectionStringBuilder(string connectionString) : this()
    {
        ConnectionString = connectionString;
    }

    [Browsable(false)]
    [AllowNull]
    public override object this[string keyword]
    {
        get
        {
            if (keyword is null)
            {
                throw new ArgumentNullException(nameof(keyword));
            }

            return keyword switch
            {
                DataSourceKey => DataSource,
                PoolingKey => Pooling,
                MaxPoolSizeKey => MaxPoolSize,
                BusyTimeoutKey or BusyTimeoutPragmaKey => BusyTimeout,
                JournalModeKey or JournalModePragmaKey => JournalMode,
                ForeignKeysKey or ForeignKeysPragmaKey => ForeignKeys ?? false,
                RecursiveTriggersKey or RecursiveTriggersPragmaKey => RecursiveTriggers ?? false,
                ModeKey => Mode,
                CacheKey => Cache,
                _ => base[keyword]
            };
        }
        set
        {
            if (keyword is null)
            {
                throw new ArgumentNullException(nameof(keyword));
            }

            switch (keyword)
            {
                case DataSourceKey:
                    DataSource = Convert.ToString(value) ?? string.Empty;
                    break;
                case PoolingKey:
                    Pooling = Convert.ToBoolean(value);
                    break;
                case MaxPoolSizeKey:
                    MaxPoolSize = Convert.ToInt32(value);
                    break;
                case BusyTimeoutKey:
                case BusyTimeoutPragmaKey:
                    BusyTimeout = Convert.ToInt32(value);
                    break;
                case JournalModeKey:
                case JournalModePragmaKey:
                    JournalMode = Convert.ToString(value) ?? string.Empty;
                    break;
                case ForeignKeysKey:
                case ForeignKeysPragmaKey:
                    ForeignKeys = ConvertToNullableBoolean(value);
                    break;
                case RecursiveTriggersKey:
                case RecursiveTriggersPragmaKey:
                    RecursiveTriggers = ConvertToNullableBoolean(value);
                    break;
                case ModeKey:
                    Mode = Convert.ToString(value) ?? string.Empty;
                    break;
                case CacheKey:
                    Cache = ConvertToEnum<SqliteCacheMode>(value);
                    break;
                default:
                    base[keyword] = value;
                    break;
            }
        }
    }

    public override ICollection Keys
    {
        get
        {
            var keys = new List<string>();
            foreach (string key in base.Keys)
            {
                keys.Add(key);
            }

            foreach (string key in CanonicalKeys)
            {
                if (!keys.Exists(existingKey => string.Equals(existingKey, key, StringComparison.OrdinalIgnoreCase)))
                {
                    keys.Add(key);
                }
            }

            return keys;
        }
    }

    public override ICollection Values
    {
        get
        {
            var values = new List<object>();
            foreach (string key in Keys)
            {
                values.Add(this[key]);
            }

            return values;
        }
    }

    /// <summary>
    /// Gets or sets the connection string with intelligent defaults applied.
    /// For in-memory databases, automatically sets:
    /// - Cache = Shared (if not specified)
    /// - Journal Mode = DELETE (WAL not supported)
    /// </summary>
    // public override string ConnectionString
    // {
    //     get
    //     {
    //         // Apply intelligent defaults for in-memory databases
    //         if (IsInMemoryMode())
    //         {
    //             // WAL is not supported for in-memory databases
    //             if (!HasJournalMode)
    //             {
    //                 this[JournalModeKey] = "DELETE";
    //             }

    //             // Shared cache is the intelligent default for in-memory
    //             if (!ContainsKey(CacheKey))
    //             {
    //                 this[CacheKey] = "Shared";
    //             }
    //         }

    //         return base.ConnectionString;
    //     }
    //     set => base.ConnectionString = value;
    // }

    public string DataSource
    {
        get => TryGetValue(DataSourceKey, out object? v) ? Convert.ToString(v) ?? string.Empty : string.Empty;
        set => base[DataSourceKey] = value ?? string.Empty;
    }

    public string Mode
    {
        get => TryGetValue(ModeKey, out object? v) ? Convert.ToString(v) ?? string.Empty : string.Empty;
        set => base[ModeKey] = value;
    }

    public virtual SqliteCacheMode Cache
    {
        get => TryGetValue(CacheKey, out object? v) ? ConvertToEnum<SqliteCacheMode>(v) : SqliteCacheMode.Default;
        set => base[CacheKey] = value;
    }

    /// <summary>
    /// Fake Property only for compatibility.
    /// At the moment encryption is not supported
    /// </summary>
    [AllowNull]
    public string Password { get; set; }

    public bool? ForeignKeys
    {
        get => GetNullableBoolean(ForeignKeysKey, ForeignKeysPragmaKey);
        set => SetNullableBoolean(ForeignKeysKey, ForeignKeysPragmaKey, value);
    }

    public bool? RecursiveTriggers
    {
        get => GetNullableBoolean(RecursiveTriggersKey, RecursiveTriggersPragmaKey);
        set => SetNullableBoolean(RecursiveTriggersKey, RecursiveTriggersPragmaKey, value);
    }

    public int BusyTimeout
    {
        get
        {
            if (TryGetValue(BusyTimeoutKey, out object? value) || TryGetValue(BusyTimeoutPragmaKey, out value))
            {
                return Math.Max(0, Convert.ToInt32(value));
            }

            return 30000;
        }
        set
        {
            base[BusyTimeoutKey] = Math.Max(0, value);
            Remove(BusyTimeoutPragmaKey);
        }
    }

    /// <summary>
    /// Gets or sets the busy timeout in seconds (default: 30 seconds).
    /// Internally, this is converted to/from milliseconds for <see cref="BusyTimeout"/>.
    /// </summary>
    public int DefaultTimeout
    {
        get => BusyTimeout / 1000;
        set => BusyTimeout = value * 1000;
    }

    public bool Pooling
    {
        get => TryGetValue(PoolingKey, out object? v) ? Convert.ToBoolean(v) : true;
        set => base[PoolingKey] = value;
    }

    public int MaxPoolSize
    {
        get => TryGetValue(MaxPoolSizeKey, out object? v) ? Convert.ToInt32(v) : 100;
        set => base[MaxPoolSizeKey] = Math.Max(1, value);
    }

    public string JournalMode
    {
        get
        {
            if (TryGetValue(JournalModeKey, out object? value) || TryGetValue(JournalModePragmaKey, out value))
            {
                return Convert.ToString(value) ?? string.Empty;
            }

            return "WAL"; // Default: WAL for better concurrency
        }
        set
        {
            base[JournalModeKey] = value ?? string.Empty;
            Remove(JournalModePragmaKey);
        }
    }

    internal bool HasBusyTimeout
        => TryGetValue(BusyTimeoutKey, out _) || TryGetValue(BusyTimeoutPragmaKey, out _);

    internal bool HasForeignKeys
        => TryGetValue(ForeignKeysKey, out _) || TryGetValue(ForeignKeysPragmaKey, out _);

    internal bool HasJournalMode
        => TryGetValue(JournalModeKey, out _) || TryGetValue(JournalModePragmaKey, out _);

    internal bool HasRecursiveTriggers
        => TryGetValue(RecursiveTriggersKey, out _) || TryGetValue(RecursiveTriggersPragmaKey, out _);

    /// <summary>
    /// Determines if the connection is for an in-memory database.
    /// </summary>
    internal bool IsInMemoryMode()
    {
        // Check Data Source
        if (DataSource == ":memory:")
            return true;

        // Check explicit Mode
        if (string.Equals(Mode, "Memory", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check URI format for shared memory
        if (DataSource?.StartsWith("file:", StringComparison.OrdinalIgnoreCase) == true &&
            DataSource.Contains("mode=memory", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private bool? GetNullableBoolean(string key, string pragmaKey)
    {
        if (!TryGetValue(key, out object? value) && !TryGetValue(pragmaKey, out value))
        {
            return null;
        }

        if (value is bool typed)
        {
            return typed;
        }

        string text = Convert.ToString(value) ?? string.Empty;
        if (string.Equals(text, "1", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(text, "0", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return Convert.ToBoolean(value);
    }

    private void SetNullableBoolean(string key, string pragmaKey, bool? value)
    {
        if (value.HasValue)
        {
            base[key] = value.Value;
            Remove(pragmaKey);
            return;
        }

        Remove(key);
        Remove(pragmaKey);
    }


    private static TEnum ConvertToEnum<TEnum>(object value)
        where TEnum : struct
    {
        if (value is string stringValue)
        {
            return (TEnum)Enum.Parse(typeof(TEnum), stringValue, ignoreCase: true);
        }

        if (value is TEnum enumValue)
        {
            return enumValue;
        }

        return (TEnum)Enum.ToObject(typeof(TEnum), value);
    }

    private static bool? ConvertToNullableBoolean(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is bool typed)
        {
            return typed;
        }

        string text = Convert.ToString(value) ?? string.Empty;
        if (string.Equals(text, "1", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(text, "0", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return Convert.ToBoolean(value);
    }
}
