// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace CiccioSoft.Sqlite;

// Un "Value Type" a costo zero di allocazione che imita il Nullable<T> per gli Span
public readonly ref struct Utf8String
{
    private readonly ReadOnlySpan<byte> _value;

    /// <summary>
    /// Ottiene il valore come <see cref="ReadOnlySpan{Byte}"/>. 
    /// Lancia <see cref="InvalidOperationException"/> se il campo nel DB è NULL.
    /// </summary>
    public ReadOnlySpan<byte> Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (IsNull) ThrowEntityNull();
            return _value;
        }
    }

    /// <summary>
    /// Indica se il campo nel Database è NULL.
    /// </summary>
    public bool IsNull { get; }

    /// <summary>
    /// Indica se lo span è vuoto (lunghezza 0), sia che sia una stringa vuota "" sia che sia NULL.
    /// </summary>
    public bool IsEmpty => _value.IsEmpty;

    public int Length => _value.Length;

    /// <summary>
    /// Inizializza una nuova istanza della struttura per un valore valido o vuoto.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf8String(ReadOnlySpan<byte> value)
    {
        _value = value;
        IsNull = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf8String(ReadOnlySpan<byte> value, bool isNull)
    {
        _value = value;
        IsNull = isNull;
    }

    /// <summary>
    /// Istanza statica pre-allocata che rappresenta un valore DBNull.
    /// </summary>
    public static Utf8String Utf8StringNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ReadOnlySpan<byte>.Empty, isNull: true);
    }

    public static Utf8String Utf8StringEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ReadOnlySpan<byte>.Empty);
    }

    /// <summary>
    /// Converte in modo efficiente il buffer UTF-8 in una stringa .NET, gestendo il caso NULL.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string? ToStringOrNull()
    {
        if (IsNull) return null;
        if (_value.IsEmpty) return string.Empty;

        // Ultra-ottimizzata in .NET 10 per gli Span
        return Encoding.UTF8.GetString(_value);
    }

    /// <summary>
    /// Conversione esplicita a string per evitare conversioni implicite nascoste e costose.
    /// </summary>
    public static explicit operator string?(Utf8String text) => text.ToStringOrNull();

    /// <summary>
    /// Consente l'uso di List Patterns di C# 11+ direttamente sulla struct (es. se la struct incontra un pattern [0x41, ..])
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte>.Enumerator GetEnumerator() => _value.GetEnumerator();

    // Spostiamo il lancio dell'eccezione in un metodo separato non inlinabile
    // per mantenere il corpo della proprietà 'Value' estremamente piccolo e ottimizzabile dal JIT.
    [DoesNotReturn]
    private static void ThrowEntityNull() =>
        throw new InvalidOperationException("Il campo del database è NULL. Verifica 'IsNull' prima di accedere a 'Value'.");
}
