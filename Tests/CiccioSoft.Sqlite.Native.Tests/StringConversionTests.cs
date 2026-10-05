// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace CiccioSoft.Sqlite.Native.Tests;

public unsafe sealed class StringConversionTests
{
    [Fact]
    public void Test1()
    {
        ReadOnlySpan<byte> span1 = new ReadOnlySpan<byte>(null, 0);
        ReadOnlySpan<byte> span2 = ReadOnlySpan<byte>.Empty;
        ReadOnlySpan<byte> span3 = ""u8;

        ReadOnlySpan<byte> span4 = new ReadOnlySpan<byte>(new byte[1], 0, 1);
        ReadOnlySpan<byte> span5 = "\0"u8;

        ReadOnlySpan<byte> span6 = "x"u8;
        ReadOnlySpan<byte> span7 = "hello"u8;

        fixed (byte* ptr1 = span1)
        fixed (byte* ptr2 = span2)
        fixed (byte* ptr3 = span3)
        fixed (byte* ptr4 = span4)
        fixed (byte* ptr5 = span5)
        fixed (byte* ptr6 = span6)
        fixed (byte* ptr7 = span7)
        {
            string? string1 = Marshal.PtrToStringUTF8((nint)ptr1);
            string? string2 = Marshal.PtrToStringUTF8((nint)ptr2);
            string? string3 = Marshal.PtrToStringUTF8((nint)ptr3);
            string? string4 = Marshal.PtrToStringUTF8((nint)ptr4);
            string? string5 = Marshal.PtrToStringUTF8((nint)ptr5);
            string? string6 = Marshal.PtrToStringUTF8((nint)ptr6);
            string? string7 = Marshal.PtrToStringUTF8((nint)ptr7);

            Assert.Null(string1);
            Assert.Null(string2);
            Assert.Null(string3);
            Assert.Equal(string.Empty, string4);
            Assert.Equal(string.Empty, string5);
            Assert.Equal("x", string6);
            Assert.Equal("hello", string7);

            // string? yyyy1 = Marshal.PtrToStringUTF8((nint)ptr1, 0);      // Error
            // string? yyyy2 = Marshal.PtrToStringUTF8((nint)ptr2, 0);      // Error
            // string? yyyy3 = Marshal.PtrToStringUTF8((nint)ptr3, 0);      // Error
            string? yyyy40 = Marshal.PtrToStringUTF8((nint)ptr4, 0);        // Like SQLite p/invoke
            string? yyyy41 = Marshal.PtrToStringUTF8((nint)ptr4, 1);
            string? yyyy5 = Marshal.PtrToStringUTF8((nint)ptr5, 1);
            string? yyyy6 = Marshal.PtrToStringUTF8((nint)ptr6, 1);
            string? yyyy7 = Marshal.PtrToStringUTF8((nint)ptr7, 5);

            Assert.Equal(string.Empty, yyyy40);
            Assert.Equal("\0", yyyy41);
            Assert.Equal("\0", yyyy5);
            Assert.Equal("x", yyyy6);
            Assert.Equal("hello", yyyy7);
        }
        string xxx1 = Encoding.UTF8.GetString(span1);
        string xxx2 = Encoding.UTF8.GetString(span2);
        string xxx3 = Encoding.UTF8.GetString(span3);
        string xxx4 = Encoding.UTF8.GetString(span4);
        string xxx5 = Encoding.UTF8.GetString(span5);
        string xxx6 = Encoding.UTF8.GetString(span6);
        string xxx7 = Encoding.UTF8.GetString(span7);

        Assert.Equal("", xxx1);
        Assert.Equal("", xxx2);
        Assert.Equal("", xxx3);
        Assert.Equal("\0", xxx4);
        Assert.Equal("\0", xxx5);
        Assert.Equal("x", xxx6);
        Assert.Equal("hello", xxx7);
    }
}
