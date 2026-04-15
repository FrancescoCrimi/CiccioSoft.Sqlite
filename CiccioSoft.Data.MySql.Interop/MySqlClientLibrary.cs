// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Data.MySql.Interop
{
    internal static class MySqlClientLibrary
    {
#if WINDOWS
        public const string Name = "libmysql";
#elif OSX
        public const string Name = "libmysqlclient";
#else
        public const string Name = "libmysqlclient";
#endif
    }
}
