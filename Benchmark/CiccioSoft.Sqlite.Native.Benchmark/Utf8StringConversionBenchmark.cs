// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace CiccioSoft.Sqlite.Native.Benchmark;

[MemoryDiagnoser] // Fondamentale per vedere le allocazioni di memoria (es. l'array del Test 1)
public unsafe class Utf8StringConversionBenchmark
{
    // Stringa di test simulata (240 caratteri)
    private static ReadOnlySpan<byte> Utf8Source => "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.\0"u8;

    private byte* _pointer;       // Simula il puntatore restituito da SQLite
    private int _length;          // Simula il valore di sqlite3_column_bytes

    [GlobalSetup]
    public void Setup()
    {
        _length = Utf8Source.Length - 1; // Escludiamo il terminatore nullo

        // Allochiamo memoria nativa non gestita (esattamente come fa SQLite)
        _pointer = (byte*)Marshal.AllocHGlobal(Utf8Source.Length);

        // Copiamo i dati di test nella memoria nativa
        fixed (byte* pSource = Utf8Source)
        {
            Buffer.MemoryCopy(pSource, (void*)_pointer, Utf8Source.Length, Utf8Source.Length);
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // Liberiamo la memoria nativa
        if (_pointer != null)
        {
            Marshal.FreeHGlobal((nint)_pointer);
        }
    }

    // ==========================================
    // BASELINE (Vecchio metodo pre-.NET Core)
    // ==========================================

    [Benchmark(Baseline = true)]
    public string OldWay_Allocating()
    {
        // 1. Alloca un array gestito temporaneo
        byte[] managedArray = new byte[_length];
        // 2. Copia i dati dal puntatore nativo all'array gestito
        Marshal.Copy((nint)_pointer, managedArray, 0, _length);
        // 3. Converte l'array UTF-8 nella stringa UTF-16 di .NET
        string text = Encoding.UTF8.GetString(managedArray);
        return text;
    }

    // ==========================================
    // SCENARI A LUNGHEZZA NOTA (es. sqlite3_column_bytes disponibile)
    // ==========================================

    [Benchmark]
    public string Encoding_Pointer()
    {
        // Converte direttamente dalla memoria puntata usando la codifica UTF-8
        string text = Encoding.UTF8.GetString(_pointer, _length);
        return text;
    }

    [Benchmark]
    public string Marshal_PtrToString_Length()
    {
        // Converte il puntatore in stringa UTF-8 passando la lunghezza
        string text = Marshal.PtrToStringUTF8((nint)_pointer, _length);
        return text;
    }

    [Benchmark]
    public string Encoding_Span()
    {
        // 1. Crea una vista di tipo Span sulla memoria puntata (Zero allocazioni)
        ReadOnlySpan<byte> utf8Span = new ReadOnlySpan<byte>(_pointer, _length);

        // 2. Decodifica lo span UTF-8 direttamente nella stringa .NET
        string text = Encoding.UTF8.GetString(utf8Span);
        return text;
    }

    // ==========================================
    // SCENARI A LUNGHEZZA IGNOTA (Null-Terminated, es. sqlite3_errmsg)
    // ==========================================

    [Benchmark]
    public string Marshal_PtrToString_AutoLength()
    {
        // Converte il puntatore in stringa UTF-8.
        string text = Marshal.PtrToStringUTF8((nint)_pointer);
        return text;
    }

    [Benchmark]
    public string Span_AutoLength()
    {
        // Sfrutta le ottimizzazioni di .NET moderno per creare uno span da un puntatore null-terminated
        ReadOnlySpan<byte> utf8Span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(_pointer);
        string text = Encoding.UTF8.GetString(utf8Span);
        return text;
    }
}
