// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Diagnostics;

namespace CiccioSoft.Data.MySql.Interop.Native
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
    [Conditional("DEBUG")]
    internal sealed partial class NativeAnnotationAttribute : Attribute
    {
        private readonly string _annotation;

        public NativeAnnotationAttribute(string annotation)
        {
            _annotation = annotation;
        }

        public string Annotation => _annotation;
    }
}
