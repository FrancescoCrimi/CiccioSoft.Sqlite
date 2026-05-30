# CiccioSoft.Sqlite.Interop

![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET Version](https://img.shields.io/badge/.NET-10.0-purple.svg)
![Language](https://img.shields.io/badge/language-C%23-brightgreen.svg)

The raw P/Invoke binding layer for SQLite, exposing the native SourceGear.sqlite3 engine through idiomatic .NET wrapper types. It delivers a thin, object-oriented interop surface that is designed to be consumed by higher-level data access layers.

## Overview

This project contains the low-level interop layer that binds .NET to the native SQLite C library via SourceGear.sqlite3. It provides:

- **Raw P/Invoke Bindings**: Direct calls to the native SQLite binary
- **Idiomatic OOP Surface**: Managed wrapper types for cleaner .NET usage
- **High Performance**: Minimal overhead around native operations
- **Comprehensive Coverage**: Support for core SQLite connection, statement, and error APIs
- **Error Handling**: Native error code translation into .NET exceptions

## Key Components

- `Sqlite3`: Main database connection class
- `Sqlite3Stmt`: Prepared statement wrapper
- `NativeSqlite3`: P/Invoke declarations for SourceGear.sqlite3
- `SqliteErrorHelper`: Error code translation utilities
- `SqliteInteropException`: Native error exceptions

## Features

- **Direct Native Access**: Calls the SQLite engine with minimal abstraction
- **Statement Lifecycle**: Prepare, bind, step, and finalize statements
- **Parameter Binding**: Support for SQLite parameter types and binding modes
- **Result Retrieval**: Read column values by index and type
- **Transaction Control**: Begin, commit, and rollback transactions
- **Pragma Support**: Configure SQLite via PRAGMA statements
- **Interrupt Support**: Cancellation through `sqlite3_interrupt`

## Usage Example

```csharp
using var db = Sqlite3.Open("app.db", SqliteOpenFlags.ReadWrite | SqliteOpenFlags.Create);
using var stmt = db.Prepare("SELECT name FROM users WHERE age > ?");
stmt.BindInt(1, 18);

while (stmt.Step() == SqliteResult.Row)
{
    string name = stmt.ColumnText(0);
    Console.WriteLine(name);
}
```

## Architecture

This layer provides the foundation for higher-level abstractions:

- **Minimal Managed State**: Only thin wrappers and handle lifetimes are managed
- **Thread Safety**: Built on SQLite's configured threading mode
- **Resource Management**: Ensures native handles are disposed cleanly
- **Error Propagation**: Preserves native error metadata for upper layers

## Dependencies

- Native `SourceGear.sqlite3` binary (bundled or system-installed)
- .NET 10.0 or later; this interop project targets `net10.0` as the minimum supported TFM

## Build Process

The interop layer includes a custom build step (`generate.cmd`) that:
- Uses ClangSharp to parse SQLite headers and generate P/Invoke bindings
- Targets the SourceGear.sqlite3 native binary for runtime
- Produces idiomatic managed wrappers around raw SQLite APIs
- Embeds native binaries in the NuGet package

## Credits

This project gratefully acknowledges:

- [SourceGear.sqlite3](https://github.com/sqlite/sqlite) for the native SQLite binary distribution
- [ClangSharp](https://github.com/microsoft/ClangSharp) for generating P/Invoke bindings from C headers
- [sqlite.org](https://sqlite.org) for the SQLite engine, documentation, and reference material