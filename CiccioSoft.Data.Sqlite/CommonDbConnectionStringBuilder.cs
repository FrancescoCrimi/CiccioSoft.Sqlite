// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CiccioSoft.Data.Sqlite.Properties;

namespace CiccioSoft.Data.Common;

/// <summary>
/// Provides a base class for strongly-typed connection string builders.
/// Handles keyword alias mapping, type conversion, and serialization.
/// </summary>
public abstract class CommonDbConnectionStringBuilder : DbConnectionStringBuilder
{
    // Ordered list of canonical (first-alias) names – used for Keys/Values.
    public List<string> OptionNames { get; } = [];

    protected readonly Dictionary<string, ConnectionStringOption> options = new(StringComparer.OrdinalIgnoreCase);

    private ReadOnlyCollection<string>? _readOnlyKeys;

    #region Ctor

    protected CommonDbConnectionStringBuilder()
    {
    }

    protected CommonDbConnectionStringBuilder(bool useOdbcRules) : base(useOdbcRules)
    {
    }

    #endregion

    #region Override

#pragma warning disable CS8764
    public override object? this[string keyword]
    {
        get => GetOptionForKey(keyword).GetObject();
        set
        {
            var option = GetOptionForKey(keyword);
            if (value is null)
            {
                base.Remove(option.Key);
                foreach (var synonym in option.Keys)
                    base.Remove(synonym);
            }
            else
            {
                option.SetObject(value);
            }
        }
    }
#pragma warning restore CS8764

    public override ICollection Keys
        => _readOnlyKeys ??= OptionNames.AsReadOnly();

    public override ICollection Values
    {
        get
        {
            var values = new List<object?>(OptionNames.Count);
            foreach (string key in Keys)
                values.Add(this[key]);
            return values.AsReadOnly();
        }
    }

    public override bool ContainsKey(string keyword)
    {
        if (TryGetOptionForKey(keyword) is not { } option)
            return false;
        foreach (var key in option.Keys)
            if (base.ContainsKey(key)) return true;
        return false;
    }

    public override bool Remove(string keyword)
    {
        if (TryGetOptionForKey(keyword) is not { } option)
            return false;
        bool removed = false;
        foreach (var key in option.Keys)
            if (base.Remove(key)) removed = true;
        return removed;
    }

    public override bool ShouldSerialize(string keyword)
    {
        return TryGetOptionForKey(keyword) is { } option
            && base.ShouldSerialize(option.Key)
            && option.GetObject() is { } value
            && value is not string { Length: 0 };
    }

    public override void Clear() => base.Clear();

#pragma warning disable CS8765
    public override bool TryGetValue(string keyword, out object? value)
    {
        if (TryGetOptionForKey(keyword) is { } option)
        {
            foreach (var alias in option.Keys)
                if (base.TryGetValue(alias, out value)) return true;
        }
        value = null;
        return false;
    }
#pragma warning restore CS8765

    #endregion

    #region Protected and Helper methods

    protected ConnectionStringOption? TryGetOptionForKey(string key) =>
        options.TryGetValue(key, out var option) ? option : null;

    protected ConnectionStringOption GetOptionForKey(string key) =>
        TryGetOptionForKey(key) ?? throw new ArgumentException(GetKeywordNotSupportedMessage(key));

    protected virtual string GetKeywordNotSupportedMessage(string keyword) =>
        Resources.KeywordNotSupported(keyword);

    protected ConnectionStringOption<T> Register<T>(
        string[] keys,
        Func<T, T>? coerce = null)
    {
        ArgumentNullException.ThrowIfNull(keys);
        if (keys.Length == 0)
            throw new ArgumentException("At least one key must be specified.", nameof(keys));

        var option = new ConnectionStringOption<T>(this, keys, coerce);
        foreach (var key in keys)
            options.Add(key, option);
        OptionNames.Add(keys[0]);
        _readOnlyKeys = null;
        return option;
    }

    // Bridge to protected DbConnectionStringBuilder members for internal option access.
    protected internal void SetBaseValue(string key, object? value) => base[key] = value;
    protected internal bool RemoveBaseValue(string key) => base.Remove(key);

    #endregion

    // ---------------------------------------------------------------------------
    // Registry of all known connection-string keys and their typed accessors.
    // Two-class design is structurally required: the non-generic base allows a
    // single Dictionary<string, ConnectionStringOption> dispatch table,
    // while the generic subclass owns type-safe conversion & coercion logic.
    // ---------------------------------------------------------------------------

    public abstract class ConnectionStringOption(CommonDbConnectionStringBuilder builder, string[] keys)
    {
        protected readonly CommonDbConnectionStringBuilder _builder = builder;

        public string Key => keys[0];
        public IReadOnlyList<string> Keys => keys;

        // Non-generic surface used by the builder's indexer and ShouldSerialize.
        public abstract object? GetObject();
        public abstract void SetObject(object? value);
    }

    public sealed class ConnectionStringOption<T>(CommonDbConnectionStringBuilder builder, string[] keys, Func<T, T>? coerce = null)
        : ConnectionStringOption(builder, keys)
    {
        // Typed get: returns default(T) (null for nullable types) when key is absent.
        public T? GetValue()
        {
            return _builder.TryGetValue(Key, out var raw) && raw is not null
                ? ConvertTo(raw)
                : default;
        }

        // Typed set: null removes the key, non-null stores the (optionally coerced) value.
        public void SetValue([AllowNull] T value)
        {
            if (value is null)
            {
                foreach (var synonym in Keys)
                    _builder.RemoveBaseValue(synonym);
            }
            else
            {
                foreach (var synonym in Keys)
                {
                    if (!string.Equals(synonym, Key, StringComparison.OrdinalIgnoreCase))
                        _builder.RemoveBaseValue(synonym);
                }
                _builder.SetBaseValue(Key, coerce is not null ? coerce(value) : value);
            }
        }

        // Non-generic bridge – delegates to the typed pair above.
        public override object? GetObject() => GetValue();

        public override void SetObject(object? value)
        {
            if (value is null)
            {
                foreach (var synonym in Keys)
                    _builder.RemoveBaseValue(synonym);
            }
            else
            {
                SetValue(ConvertTo(value));
            }
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
}
