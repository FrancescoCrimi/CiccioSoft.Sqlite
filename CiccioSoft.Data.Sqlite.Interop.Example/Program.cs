// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Data.Sqlite.Interop.Example;

class Program
{
    static void Main(string[] args)
    {
        string? nomeDaInserire = null;
        if (args.Length != 0)
            nomeDaInserire = args[0];

        new UserRepository(nomeDaInserire);
        // new ImageRepository();
    }
}
