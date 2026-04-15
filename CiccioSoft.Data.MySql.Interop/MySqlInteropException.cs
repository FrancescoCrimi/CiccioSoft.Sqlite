// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.Serialization;

namespace CiccioSoft.Data.MySql.Interop
{
    [Serializable]
    public sealed class MySqlInteropException : Exception
    {
        public MySqlInteropException()
        {
        }

        public MySqlInteropException(string message)
            : base(message)
        {
        }

        public MySqlInteropException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private MySqlInteropException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
