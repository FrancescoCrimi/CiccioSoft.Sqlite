// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Frozen;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CiccioSoft.Data.Sqlite.Properties;

namespace CiccioSoft.Data.Sqlite;

/// <summary>
/// Builds SQLite connection strings with intelligent defaults:
/// - WAL enabled by default (better concurrency)
/// - Foreign Keys enabled by default (referential integrity)
/// - For in-memory databases: Shared cache by default, WAL disabled
/// </summary>
public class SqliteConnectionStringBuilder : DbConnectionStringBuilder
{
    /// <summary>
    /// Initializes a new instance with intelligent defaults:
    /// - Journal Mode: WAL (better concurrency)
    /// - Foreign Keys: ON (referential integrity)
    /// </summary>
    public SqliteConnectionStringBuilder()
    {
        DataSource = string.Empty;
        Pooling = true;
        MaxPoolSize = 100;
        DefaultTimeout = 30;
        // No default for JournalMode and ForeignKeys here; defaults will be applied by SqliteConnection
    }

    public SqliteConnectionStringBuilder(string connectionString) : this()
    {
        ConnectionString = connectionString;
    }

#pragma warning disable CS8764
    public override object? this[string keyword]
    {
        get => SqliteConnectionStringOption.GetOptionForKey(keyword).GetObject(this);
        set
        {
            var option = SqliteConnectionStringOption.GetOptionForKey(keyword);
            if (value is null)
            {
                base.Remove(option.Key);
                foreach (var synonym in option.Keys)
                {
                    base.Remove(synonym);
                }
            }
            else
            {
                option.SetObject(this, value);
            }
        }
    }
#pragma warning restore CS8764

    public override ICollection Keys
        // => SqliteConnectionStringOption.OptionNames;
        => SqliteConnectionStringOption.OptionNames.AsReadOnly();
        // => new ReadOnlyCollection<string>(SqliteConnectionStringOption.OptionNames);

    public override ICollection Values
    {
        get
        {
            var values = new List<object?>();
            foreach (string key in Keys)
            {
                values.Add(this[key]);
            }
            // return values;
            return values.AsReadOnly();
        }
    }

    public override bool ContainsKey(string keyword) =>
        SqliteConnectionStringOption.TryGetOptionForKey(keyword) is { } option && ContainsExplicitKey(option);

    public override bool Remove(string keyword)
    {
        if (SqliteConnectionStringOption.TryGetOptionForKey(keyword) is { } option)
        {
            bool removed = false;
            foreach (var key in option.Keys)
            {
                if (base.Remove(key))
                {
                    removed = true;
                }
            }
            return removed;
        }
        return false;
    }

    public override bool ShouldSerialize(string keyword)
        => SqliteConnectionStringOption.TryGetOptionForKey(keyword) is { } option
            && base.ShouldSerialize(option.Key)
            && option.HasNonEmptyValue(this);

    public override void Clear()
    {
        base.Clear();
    }

#pragma warning disable CS8765
    public override bool TryGetValue(string keyword, out object? value)
    {
        if (SqliteConnectionStringOption.TryGetOptionForKey(keyword) is { } option)
        {
            foreach (var alias in option.Keys)
            {
                if (base.TryGetValue(alias, out value))
                {
                    return true;
                }
            }
            // If not present in dictionary, return false and null
        }
        value = null;
        return false;
    }
#pragma warning restore CS8765

    public string DataSource
    {
        get => SqliteConnectionStringOption.DataSource.GetValue(this);
        set => SqliteConnectionStringOption.DataSource.SetValue(this, value);
    }

    public virtual SqliteOpenMode Mode
    {
        get => SqliteConnectionStringOption.Mode.GetValue(this);
        set => SqliteConnectionStringOption.Mode.SetValue(this, value);
    }

    public virtual SqliteCacheMode Cache
    {
        get => SqliteConnectionStringOption.Cache.GetValue(this);
        set => SqliteConnectionStringOption.Cache.SetValue(this, value);
    }

    /// <summary>
    /// Fake Property only for compatibility.
    /// At the moment encryption is not supported
    /// </summary>
    [AllowNull]
    public string Password
    {
        get => SqliteConnectionStringOption.Password.GetValue(this);
        set => SqliteConnectionStringOption.Password.SetValue(this, value);
    }

    public bool? ForeignKeys
    {
        get => SqliteConnectionStringOption.ForeignKeys.GetValue(this);
        set => SqliteConnectionStringOption.ForeignKeys.SetValue(this, value);
    }

    public bool? RecursiveTriggers
    {
        get => SqliteConnectionStringOption.RecursiveTriggers.GetValue(this);
        set => SqliteConnectionStringOption.RecursiveTriggers.SetValue(this, value);
    }

    /// <summary>
    /// Gets or sets the default timeout in seconds (default: 30 seconds).
    /// </summary>
    public int DefaultTimeout
    {
        get => SqliteConnectionStringOption.DefaultTimeout.GetValue(this);
        set => SqliteConnectionStringOption.DefaultTimeout.SetValue(this, value);
    }

    public bool Pooling
    {
        get => SqliteConnectionStringOption.Pooling.GetValue(this);
        set => SqliteConnectionStringOption.Pooling.SetValue(this, value);
    }

    /// <summary>
    /// Fake property only for compatibility.
    /// </summary>
    public string? Vfs
    {
        get => SqliteConnectionStringOption.Vfs.GetValue(this);
        set => SqliteConnectionStringOption.Vfs.SetValue(this, value);
    }

    public int MaxPoolSize
    {
        get => SqliteConnectionStringOption.MaxPoolSize.GetValue(this);
        set => SqliteConnectionStringOption.MaxPoolSize.SetValue(this, value);
    }

    public string JournalMode
    {
        get => SqliteConnectionStringOption.JournalMode.GetValue(this);
        set => SqliteConnectionStringOption.JournalMode.SetValue(this, value);
    }

    internal bool HasForeignKeys
        => ContainsExplicitKey(SqliteConnectionStringOption.ForeignKeys);

    internal bool HasJournalMode
        => ContainsExplicitKey(SqliteConnectionStringOption.JournalMode);

    private bool ContainsExplicitKey(SqliteConnectionStringOption option)
    {
        foreach (var key in option.Keys)
        {
            if (base.ContainsKey(key))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Determines if the connection is for an in-memory database.
    /// </summary>
    internal bool IsInMemoryMode()
    {
        // Check Data Source
        if (DataSource == ":memory:")
            return true;

        // Check explicit Mode
        if (Mode == SqliteOpenMode.Memory)
            return true;

        // Check URI format for shared memory
        if (DataSource?.StartsWith("file:", StringComparison.OrdinalIgnoreCase) == true &&
            DataSource.Contains("mode=memory", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    internal void DoSetValue(string key, object? value) => base[key] = value;
    internal void DoRemove(string key) => base.Remove(key);
}

/// <summary>
/// Abstract base for connection string options. Provides the static registry
/// of all known options and the <see cref="Register{T}"/> factory method.
/// </summary>
internal abstract class SqliteConnectionStringOption(string[] keys)
{
    public static List<string> OptionNames { get; } = [];
    private static readonly FrozenDictionary<string, SqliteConnectionStringOption> s_options;

    public static readonly SqliteConnectionStringOption<string> DataSource;
    public static readonly SqliteConnectionStringOption<SqliteOpenMode> Mode;
    public static readonly SqliteConnectionStringOption<SqliteCacheMode> Cache;
    public static readonly SqliteConnectionStringOption<string> Password;
    public static readonly SqliteConnectionStringOption<bool?> ForeignKeys;
    public static readonly SqliteConnectionStringOption<bool?> RecursiveTriggers;
    public static readonly SqliteConnectionStringOption<int> DefaultTimeout;
    public static readonly SqliteConnectionStringOption<bool> Pooling;
    public static readonly SqliteConnectionStringOption<int> MaxPoolSize;
    public static readonly SqliteConnectionStringOption<string> JournalMode;
    public static readonly SqliteConnectionStringOption<string?> Vfs;

    public string Key => keys[0];
    public IReadOnlyList<string> Keys => keys;

    public static SqliteConnectionStringOption? TryGetOptionForKey(string key) =>
        s_options.TryGetValue(key, out var option) ? option : null;

    public static SqliteConnectionStringOption GetOptionForKey(string key) =>
        TryGetOptionForKey(key) ?? throw new ArgumentException(Resources.KeywordNotSupported(key));

    public abstract object? GetObject(SqliteConnectionStringBuilder builder);
    public abstract void SetObject(SqliteConnectionStringBuilder builder, object? value);
    public abstract bool HasNonEmptyValue(SqliteConnectionStringBuilder builder);

    private static SqliteConnectionStringOption<T> Register<T>(
        Dictionary<string, SqliteConnectionStringOption> options,
        string[] keys,
        T defaultValue,
        Func<T, T>? coerce = null)
    {
        var option = new SqliteConnectionStringOption<T>(keys, defaultValue, coerce);
        foreach (var key in keys)
            options.Add(key, option);
        OptionNames.Add(keys[0]);
        return option;
    }

    static SqliteConnectionStringOption()
    {
        var options = new Dictionary<string, SqliteConnectionStringOption>(StringComparer.OrdinalIgnoreCase);

        DataSource        = Register(options, ["Data Source", "DataSource", "Filename"], string.Empty);
        Mode              = Register(options, ["Mode"], SqliteOpenMode.ReadWriteCreate);
        Cache             = Register(options, ["Cache"], SqliteCacheMode.Default);
        Password          = Register(options, ["Password"], string.Empty);
        ForeignKeys       = Register<bool?>(options, ["Foreign Keys", "foreign_keys"], null);
        RecursiveTriggers = Register<bool?>(options, ["Recursive Triggers", "recursive_triggers"], null);
        DefaultTimeout    = Register(options, ["Default Timeout", "default_timeout", "Command Timeout"], 30, static val => Math.Max(0, val));
        Pooling           = Register(options, ["Pooling"], true);
        MaxPoolSize       = Register(options, ["Max Pool Size", "MaxPoolSize"], 100, static val => Math.Max(1, val));
        JournalMode       = Register(options, ["Journal Mode", "journal_mode"], "WAL");
        Vfs               = Register<string?>(options, ["Vfs"], null);

        s_options = options.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Unified typed option — handles value types, nullable value types, and reference types
/// without separate subclasses. Uses a single <see cref="ConvertTo"/> method for all
/// type conversions with consistent boolean and enum parsing.
/// </summary>
internal sealed class SqliteConnectionStringOption<T>(
    string[] keys,
    T defaultValue,
    Func<T, T>? coerce = null)
    : SqliteConnectionStringOption(keys)
{
    public T GetValue(SqliteConnectionStringBuilder builder) =>
        builder.TryGetValue(Key, out var value) && value is not null
            ? ConvertTo(value)
            : defaultValue;

    public void SetValue(SqliteConnectionStringBuilder builder, [AllowNull] T value)
    {
        if (value is null)
            builder.DoRemove(Key);
        else
            builder.DoSetValue(Key, coerce is not null ? coerce(value) : value);
    }

    public override object? GetObject(SqliteConnectionStringBuilder builder) => GetValue(builder);

    public override void SetObject(SqliteConnectionStringBuilder builder, object? value)
    {
        if (value is null)
            builder.DoRemove(Key);
        else
            SetValue(builder, ConvertTo(value));
    }

    public override bool HasNonEmptyValue(SqliteConnectionStringBuilder builder)
        => GetValue(builder) switch
        {
            null => false,
            string value => value.Length != 0,
            _ => true
        };

    /// <summary>
    /// Unified type conversion with consistent handling of:
    /// - Boolean strings: "yes"/"no", "1"/"0", "true"/"false"
    /// - Enum: by name (case-insensitive) or numeric value
    /// - Nullable&lt;T&gt;: automatically unwraps to inner type for conversion
    /// </summary>
    private static T ConvertTo(object value)
    {
        var underlyingType = Nullable.GetUnderlyingType(typeof(T));
        var target = underlyingType ?? typeof(T);

        try
        {
            if (value is T typed)
            {
                if (target.IsEnum && !Enum.IsDefined(target, typed))
                    throw new ArgumentException($"The value '{value}' is not valid for enum {target.Name}.");
                return typed;
            }

            // For Nullable<> types, treat empty string as null (unset)
            if (value is string { Length: 0 } && underlyingType is not null)
                return default!;

            if (value is string text)
            {
                // Boolean: handle "yes"/"no" and "1"/"0" consistently
                if (target == typeof(bool))
                {
                    if (text.Equals("yes", StringComparison.OrdinalIgnoreCase) || text == "1")
                        return (T)(object)true;
                    if (text.Equals("no", StringComparison.OrdinalIgnoreCase) || text == "0")
                        return (T)(object)false;
                }

                // Enum: parse by name
                if (target.IsEnum)
                {
                    var parsed = Enum.Parse(target, text, ignoreCase: true);
                    if (!Enum.IsDefined(target, parsed))
                        throw new ArgumentException($"The value '{text}' is not valid for enum {target.Name}.");
                    return (T)parsed;
                }
            }
            else if (target.IsEnum)
            {
                // Prevent implicit conversion between different enum types
                if (value is Enum && value.GetType() != target)
                    throw new ArgumentException($"Cannot convert enum {value.GetType().Name} to {target.Name}.");

                var numeric = Convert.ChangeType(value, Enum.GetUnderlyingType(target), CultureInfo.InvariantCulture);
                var enumVal = Enum.ToObject(target, numeric);
                
                if (!Enum.IsDefined(target, enumVal))
                    throw new ArgumentException($"The value '{value}' is not valid for enum {target.Name}.");
                
                return (T)enumVal;
            }

            return (T)Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new ArgumentException($"Cannot convert value '{value}' to type {target.Name}.", ex);
        }
    }
}
