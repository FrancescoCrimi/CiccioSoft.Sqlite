// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;

namespace CiccioSoft.Data.Sqlite;

public class SqliteConnectionStringBuilder : DbConnectionStringBuilder
{
    private const string DataSourceKey = "Data Source";
    private const string PoolingKey = "Pooling";
    private const string MaxPoolSizeKey = "Max Pool Size";
    private const string BusyTimeoutKey = "Busy Timeout";
    private const string BusyTimeoutPragmaKey = "busy_timeout";
    private const string ForeignKeysKey = "Foreign Keys";
    private const string ForeignKeysPragmaKey = "foreign_keys";
    private const string JournalModeKey = "Journal Mode";
    private const string JournalModePragmaKey = "journal_mode";
    private const string RecursiveTriggersKey = "Recursive Triggers";
    private const string RecursiveTriggersPragmaKey = "recursive_triggers";

    private static readonly string[] CanonicalKeys =
    [
        DataSourceKey,
        PoolingKey,
        MaxPoolSizeKey,
        BusyTimeoutKey,
        JournalModeKey,
        ForeignKeysKey,
        RecursiveTriggersKey
    ];

    public SqliteConnectionStringBuilder()
    {
        base[DataSourceKey] = string.Empty;
        base[PoolingKey] = true;
        base[MaxPoolSizeKey] = 100;
        base[BusyTimeoutKey] = 30000;
        base[JournalModeKey] = string.Empty;
    }

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
                if (!keys.Contains(key, StringComparer.OrdinalIgnoreCase))
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

    public string DataSource
    {
        get => TryGetValue(DataSourceKey, out object? v) ? Convert.ToString(v) ?? string.Empty : string.Empty;
        set => base[DataSourceKey] = value ?? string.Empty;
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

    public bool? ForeignKeys
    {
        get => GetNullableBoolean(ForeignKeysKey, ForeignKeysPragmaKey);
        set => SetNullableBoolean(ForeignKeysKey, ForeignKeysPragmaKey, value);
    }

    public string JournalMode
    {
        get
        {
            if (TryGetValue(JournalModeKey, out object? value) || TryGetValue(JournalModePragmaKey, out value))
            {
                return Convert.ToString(value) ?? string.Empty;
            }

            return string.Empty;
        }
        set
        {
            base[JournalModeKey] = value ?? string.Empty;
            Remove(JournalModePragmaKey);
        }
    }

    public bool? RecursiveTriggers
    {
        get => GetNullableBoolean(RecursiveTriggersKey, RecursiveTriggersPragmaKey);
        set => SetNullableBoolean(RecursiveTriggersKey, RecursiveTriggersPragmaKey, value);
    }

    internal bool HasBusyTimeout
        => TryGetValue(BusyTimeoutKey, out _) || TryGetValue(BusyTimeoutPragmaKey, out _);

    internal bool HasForeignKeys
        => TryGetValue(ForeignKeysKey, out _) || TryGetValue(ForeignKeysPragmaKey, out _);

    internal bool HasJournalMode
        => TryGetValue(JournalModeKey, out _) || TryGetValue(JournalModePragmaKey, out _);

    internal bool HasRecursiveTriggers
        => TryGetValue(RecursiveTriggersKey, out _) || TryGetValue(RecursiveTriggersPragmaKey, out _);

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
