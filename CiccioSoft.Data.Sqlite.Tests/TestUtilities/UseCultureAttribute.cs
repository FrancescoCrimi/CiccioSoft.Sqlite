// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2026 Francesco Crimi
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Globalization;
using System.Reflection;
// using Xunit.Sdk;
using Xunit.v3;

namespace CiccioSoft.Data.Sqlite.TestUtilities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class UseCultureAttribute(string culture, string uiCulture) : BeforeAfterTestAttribute
{
    private CultureInfo? _originalCulture;
    private CultureInfo? _originalUICulture;

    public UseCultureAttribute(string culture)
        : this(culture, culture)
    {
    }

    public CultureInfo Culture { get; } = new(culture);
    public CultureInfo UICulture { get; } = new(uiCulture);

    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        _originalCulture = CultureInfo.CurrentCulture;
        _originalUICulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = Culture;
        CultureInfo.CurrentUICulture = UICulture;
    }

    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        CultureInfo.CurrentCulture = _originalCulture!;
        CultureInfo.CurrentUICulture = _originalUICulture!;
    }
}
