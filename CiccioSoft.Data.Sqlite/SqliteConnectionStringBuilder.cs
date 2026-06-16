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
/// Builds SQLite connection strings.
/// This acts as a pure configuration parser. Defaults are handled by SqliteConnection.
/// </summary>
public class SqliteConnectionStringBuilder : DbConnectionStringBuilder
{
    public SqliteConnectionStringBuilder()
    {
    }

    public SqliteConnectionStringBuilder(string connectionString) : this()
    {
        if (!string.IsNullOrEmpty(connectionString))
        {
            ConnectionString = connectionString;
        }
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
                    base.Remove(synonym);
            }
            else
            {
                option.SetObject(this, value);
            }
        }
    }
#pragma warning restore CS8764

    public override ICollection Keys
        => SqliteConnectionStringOption.OptionNames.AsReadOnly();

    public override ICollection Values
    {
        get
        {
            var values = new List<object?>();
            foreach (string key in Keys)
                values.Add(this[key]);
            return values.AsReadOnly();
        }
    }

    public override bool ContainsKey(string keyword)
    {
        if (SqliteConnectionStringOption.TryGetOptionForKey(keyword) is not { } option)
            return false;
        foreach (var key in option.Keys)
            if (base.ContainsKey(key)) return true;
        return false;
    }

    public override bool Remove(string keyword)
    {
        if (SqliteConnectionStringOption.TryGetOptionForKey(keyword) is not { } option)
            return false;
        bool removed = false;
        foreach (var key in option.Keys)
            if (base.Remove(key)) removed = true;
        return removed;
    }

    public override bool ShouldSerialize(string keyword)
        => SqliteConnectionStringOption.TryGetOptionForKey(keyword) is { } option
            && base.ShouldSerialize(option.Key)
            && option.GetObject(this) is { } value
            && value is not string { Length: 0 };

    public override void Clear() => base.Clear();

#pragma warning disable CS8765
    public override bool TryGetValue(string keyword, out object? value)
    {
        if (SqliteConnectionStringOption.TryGetOptionForKey(keyword) is { } option)
        {
            foreach (var alias in option.Keys)
                if (base.TryGetValue(alias, out value)) return true;
        }
        value = null;
        return false;
    }
#pragma warning restore CS8765

    public string? DataSource
    {
        get => SqliteConnectionStringOption.DataSource.GetValue(this);
        set => SqliteConnectionStringOption.DataSource.SetValue(this, value);
    }

    public virtual SqliteOpenMode? Mode
    {
        get => SqliteConnectionStringOption.Mode.GetValue(this);
        set => SqliteConnectionStringOption.Mode.SetValue(this, value);
    }

    public virtual SqliteCacheMode? Cache
    {
        get => SqliteConnectionStringOption.Cache.GetValue(this);
        set => SqliteConnectionStringOption.Cache.SetValue(this, value);
    }

    [AllowNull]
    public string? Password
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

    public int? DefaultTimeout
    {
        get => SqliteConnectionStringOption.DefaultTimeout.GetValue(this);
        set => SqliteConnectionStringOption.DefaultTimeout.SetValue(this, value);
    }

    public bool? Pooling
    {
        get => SqliteConnectionStringOption.Pooling.GetValue(this);
        set => SqliteConnectionStringOption.Pooling.SetValue(this, value);
    }

    public string? Vfs
    {
        get => SqliteConnectionStringOption.Vfs.GetValue(this);
        set => SqliteConnectionStringOption.Vfs.SetValue(this, value);
    }

    public int? MaxPoolSize
    {
        get => SqliteConnectionStringOption.MaxPoolSize.GetValue(this);
        set => SqliteConnectionStringOption.MaxPoolSize.SetValue(this, value);
    }

    public string? JournalMode
    {
        get => SqliteConnectionStringOption.JournalMode.GetValue(this);
        set => SqliteConnectionStringOption.JournalMode.SetValue(this, value);
    }

    // Bridge to protected DbConnectionStringBuilder members for internal option access.
    internal void DoSetValue(string key, object? value) => base[key] = value;
    internal void DoRemove(string key) => base.Remove(key);
}

// ---------------------------------------------------------------------------
// Registry of all known connection-string keys and their typed accessors.
// Two-class design is structurally required: the non-generic base allows a
// single Dictionary<string, SqliteConnectionStringOption> dispatch table,
// while the generic subclass owns type-safe conversion & coercion logic.
// ---------------------------------------------------------------------------

internal abstract class SqliteConnectionStringOption(string[] keys)
{
    // Ordered list of canonical (first-alias) names – used for Keys/Values.
    public static List<string> OptionNames { get; } = [];

    private static readonly FrozenDictionary<string, SqliteConnectionStringOption> s_options;

    // Typed option singletons – one per connection-string property.
    public static readonly SqliteConnectionStringOption<string?> DataSource;
    public static readonly SqliteConnectionStringOption<SqliteOpenMode?> Mode;
    public static readonly SqliteConnectionStringOption<SqliteCacheMode?> Cache;
    public static readonly SqliteConnectionStringOption<string?> Password;
    public static readonly SqliteConnectionStringOption<bool?> ForeignKeys;
    public static readonly SqliteConnectionStringOption<bool?> RecursiveTriggers;
    public static readonly SqliteConnectionStringOption<int?> DefaultTimeout;
    public static readonly SqliteConnectionStringOption<bool?> Pooling;
    public static readonly SqliteConnectionStringOption<int?> MaxPoolSize;
    public static readonly SqliteConnectionStringOption<string?> JournalMode;
    public static readonly SqliteConnectionStringOption<string?> Vfs;

    public string Key => keys[0];
    public IReadOnlyList<string> Keys => keys;

    public static SqliteConnectionStringOption? TryGetOptionForKey(string key) =>
        s_options.TryGetValue(key, out var option) ? option : null;

    public static SqliteConnectionStringOption GetOptionForKey(string key) =>
        TryGetOptionForKey(key) ?? throw new ArgumentException(Resources.KeywordNotSupported(key));

    // Non-generic surface used by the builder's indexer and ShouldSerialize.
    public abstract object? GetObject(SqliteConnectionStringBuilder builder);
    public abstract void SetObject(SqliteConnectionStringBuilder builder, object? value);

    protected static SqliteConnectionStringOption<T> Register<T>(
        Dictionary<string, SqliteConnectionStringOption> options,
        string[] keys,
        Func<T, T>? coerce = null)
    {
        var option = new SqliteConnectionStringOption<T>(keys, coerce);
        foreach (var key in keys)
            options.Add(key, option);
        OptionNames.Add(keys[0]);
        return option;
    }

    static SqliteConnectionStringOption()
    {
        var options = new Dictionary<string, SqliteConnectionStringOption>(StringComparer.OrdinalIgnoreCase);

        DataSource        = Register<string?>         (options, ["Data Source", "DataSource", "Filename"]);
        Mode              = Register<SqliteOpenMode?> (options, ["Mode"]);
        Cache             = Register<SqliteCacheMode?>(options, ["Cache"]);
        Password          = Register<string?>         (options, ["Password"]);
        ForeignKeys       = Register<bool?>           (options, ["Foreign Keys", "foreign_keys"]);
        RecursiveTriggers = Register<bool?>           (options, ["Recursive Triggers", "recursive_triggers"]);
        DefaultTimeout    = Register<int?>            (options, ["Default Timeout", "default_timeout", "Command Timeout"], val => val.HasValue ? Math.Max(0, val.Value) : null);
        Pooling           = Register<bool?>           (options, ["Pooling"]);
        MaxPoolSize       = Register<int?>            (options, ["Max Pool Size", "MaxPoolSize"], val => val.HasValue ? Math.Max(1, val.Value) : null);
        JournalMode       = Register<string?>         (options, ["Journal Mode", "journal_mode"]);
        Vfs               = Register<string?>         (options, ["Vfs"]);

        s_options = options.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}

internal sealed class SqliteConnectionStringOption<T>(string[] keys, Func<T, T>? coerce = null)
    : SqliteConnectionStringOption(keys)
{
    // Typed get: returns default(T) (null for nullable types) when key is absent.
    public T? GetValue(SqliteConnectionStringBuilder builder) =>
        builder.TryGetValue(Key, out var raw) && raw is not null
            ? ConvertTo(raw)
            : default;

    // Typed set: null removes the key, non-null stores the (optionally coerced) value.
    public void SetValue(SqliteConnectionStringBuilder builder, [AllowNull] T value)
    {
        if (value is null)
            builder.DoRemove(Key);
        else
            builder.DoSetValue(Key, coerce is not null ? coerce(value) : value);
    }

    // Non-generic bridge – delegates to the typed pair above.
    public override object? GetObject(SqliteConnectionStringBuilder builder) => GetValue(builder);

    public override void SetObject(SqliteConnectionStringBuilder builder, object? value)
    {
        if (value is null) builder.DoRemove(Key);
        else SetValue(builder, ConvertTo(value));
    }

    // Converts an untyped value (from the base-class dictionary) to T.
    // Handles: exact-type match, nullable unwrapping, string parsing, enum coercion.
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

            // Empty string resets a nullable value.
            if (value is string { Length: 0 } && underlyingType is not null)
                return default!;

            if (value is string text)
            {
                if (target == typeof(bool))
                {
                    if (text.Equals("yes", StringComparison.OrdinalIgnoreCase) || text == "1")
                        return (T)(object)true;
                    if (text.Equals("no", StringComparison.OrdinalIgnoreCase) || text == "0")
                        return (T)(object)false;
                }

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
