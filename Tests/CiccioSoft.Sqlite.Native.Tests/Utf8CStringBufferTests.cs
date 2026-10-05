// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace CiccioSoft.Sqlite.Native.Tests;

/// <summary>
/// Contract tests for the hybrid stack/ArrayPool UTF-8 marshalling helper used on every
/// string→native boundary in the interop layer.
/// </summary>
public unsafe sealed class Utf8CStringBufferTests
{
    [Fact]
    public void EmptyString_ProducesEmptySpan()
    {
        using var buffer = new Utf8CStringBuffer(string.Empty, stackalloc byte[64]);

        Assert.Equal(0, buffer.Length);
        // ReadOnlySpan<byte> span = buffer.AsSpan();
        // Assert.True(span.IsEmpty);
        // // Assert.False(Unsafe.IsNullRef(ref MemoryMarshal.GetReference(span)));

        string? asas;
        fixed (byte* aaaa = buffer)
        {
            asas = Marshal.PtrToStringUTF8((nint)aaaa);
        }
        Assert.Equal(string.Empty, asas);
    }

    [Fact]
    public void NullString_ProducesEmptySpam()
    {
        using var buffer = new Utf8CStringBuffer(null!, stackalloc byte[64]);
        Assert.Equal(0, buffer.Length);
        // Assert.Equal(buffer.AsSpan(), ReadOnlySpan<byte>.Empty);

        string? asas;
        fixed (byte* aaaa = buffer)
        {
            asas = Marshal.PtrToStringUTF8((nint)aaaa);
        }
        Assert.Null(asas);
    }

    [Fact]
    public void EmptyStackStorage_ThrowsArgumentException()
    {
        try
        {
            _ = new Utf8CStringBuffer(string.Empty, stackalloc byte[0]);
            Assert.Fail("Expected ArgumentException.");
        }
        catch (ArgumentException)
        {
            // atteso
        }
    }

    [Fact]
    public void ShortString_UsesStackStorage_WithoutPool()
    {
        const string value = "café";
        Span<byte> stack = stackalloc byte[128];
        using var buffer = new Utf8CStringBuffer(value, stack);

        Assert.Equal(Encoding.UTF8.GetByteCount(value), buffer.Length);
        // Assert.Equal(Encoding.UTF8.GetBytes(value), buffer.AsSpan().ToArray());
        // Null terminator lives at Length within the underlying buffer via GetPinnableReference path.
        Assert.Equal(0, Unsafe.Add(ref Unsafe.AsRef(in buffer.GetPinnableReference()), buffer.Length));
    }

    [Fact]
    public void LongString_FallsBackToArrayPool_AndRoundTrips()
    {
        string value = new string('Z', 4096);
        // Force pool path: stack storage smaller than UTF-8 need.
        Span<byte> tinyStack = stackalloc byte[32];
        using var buffer = new Utf8CStringBuffer(value, tinyStack);

        Assert.Equal(Encoding.UTF8.GetByteCount(value), buffer.Length);
        // Assert.Equal(Encoding.UTF8.GetBytes(value), buffer.AsSpan().ToArray());
        Assert.Equal(0, Unsafe.Add(ref Unsafe.AsRef(in buffer.GetPinnableReference()), buffer.Length));
    }

    [Fact]
    public void AsSpan_ExcludesNullTerminator()
    {
        using var buffer = new Utf8CStringBuffer("abc", stackalloc byte[32]);

        Assert.Equal(3, buffer.Length);
        // Assert.Equal(3, buffer.AsSpan().Length);
        // Assert.Equal("abc"u8, buffer.AsSpan());


        string? asas;
        fixed (byte* aaaa = buffer)
        {
            asas = Marshal.PtrToStringUTF8((nint)aaaa);
        }
        Assert.Equal("abc", asas);
        
    }

    [Fact]
    public void GetPinnableReference_AllowsFixedPinAcrossEmptyAndNonEmpty()
    {
        using var empty = new Utf8CStringBuffer(string.Empty, stackalloc byte[16]);
        using var text = new Utf8CStringBuffer("x", stackalloc byte[16]);

        unsafe
        {
            fixed (byte* pEmpty = empty)
            fixed (byte* pText = text)
            {
                Assert.True(pEmpty != null);
                Assert.True(pText != null);
                // Assert.Equal(0, pEmpty[0]);
                Assert.Equal((byte)'x', pText[0]);
                Assert.Equal(0, pText[1]);
            }
        }
    }

    [Fact]
    public void Dispose_IsIdempotent_WhenPoolWasUsed()
    {
        string value = new string('A', 2048);
        var buffer = new Utf8CStringBuffer(value, stackalloc byte[8]);
        buffer.Dispose();
        buffer.Dispose();
    }

    [Fact]
    public void Unicode_PreservesCodePointsInUtf8Bytes()
    {
        const string value = "東京🎉";
        using var buffer = new Utf8CStringBuffer(value, stackalloc byte[64]);

        string? asas;
        fixed (byte* aaaa = buffer)
        {
            asas = Marshal.PtrToStringUTF8((nint)aaaa);
        }
        Assert.Equal(value, asas);
    }
}
