# CiccioSoft.Data.Sqlite

![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET Version](https://img.shields.io/badge/.NET-10.0-purple.svg)
![Language](https://img.shields.io/badge/language-C%23-brightgreen.svg)

The ADO.NET provider layer for CiccioSoft.Data.Sqlite, inspired by `Microsoft.Data.Sqlite`, providing idiomatic C# wrappers over raw SQLite interop bindings. It focuses on ADO.NET semantics, true async operations, and default WAL journaling for file-backed databases.

## Overview

This project contains the high-level provider abstractions built on top of `CiccioSoft.Sqlite.Interop`. It provides:

- **Idiomatic C# APIs**: Modern .NET patterns for database operations
- **ADO.NET Provider**: Implementation of `DbConnection`, `DbCommand`, `DbDataReader`, and related ADO.NET types
- **Microsoft.Data.Sqlite-inspired design**: Similar scope and provider semantics without legacy `DataAdapter` support
- **Async Support**: Truly asynchronous methods (no `Task.Run` wrappers) with cancellation support
- **WAL Journaling**: Automatic WAL mode for file-backed databases
- **Connection Pooling**: Efficient connection reuse
- **Transaction Management**: Full ACID transaction support

## Key Classes

- `SqliteConnection`: Database connection with intelligent defaults
- `SqliteCommand`: SQL command execution with parameter binding
- `SqliteDataReader`: Forward-only data reader
- `SqliteParameter`: Parameter binding for queries
- `SqliteTransaction`: Transaction management

## Features

- **Truly Asynchronous**: All async methods complete cooperatively without blocking threads
- **Cancellation Support**: `CancellationToken` propagation with native interrupt
- **Timeout Enforcement**: Command timeouts via native `sqlite3_interrupt`
- **WAL by Default**: File-backed databases use WAL journaling automatically
- **Parameter Binding**: Named and positional parameter support
- **Schema Discovery**: `GetSchema()` and `GetSchemaTable()` implementations
- **Batch Execution**: Multi-statement SQL support
- **Provider Scope**: Does not implement `DbDataAdapter`, matching Microsoft.Data.Sqlite's modern provider focus
- **Api drop-in replacement for `Microsoft.Data.Sqlite`**:

## Limitation
- **No support encryption**:
- **Create function not supported**:

## Usage Example

```csharp
using var connection = new SqliteConnection("Data Source=app.db");
await connection.OpenAsync();

using var command = new SqliteCommand("SELECT * FROM Users WHERE Age > @age", connection);
command.Parameters.AddWithValue("@age", 18);

using var reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    Console.WriteLine(reader.GetString("Name"));
}
```

## Dependencies

- `CiccioSoft.Sqlite.Interop`: Raw P/Invoke bindings targeting `net10.0`
- .NET 10.0 or later; this provider targets `net10.0` as the minimum supported TFM and does not provide `net9.0`, `netstandard`, or earlier builds

## Architecture

This layer sits atop the interop bindings, providing:
- Exception translation from native errors to .NET exceptions
- Connection state management
- Command lifecycle handling
- Data type mapping between SQLite and .NET
- Thread safety through serialization
- ADO.NET provider semantics inspired by Microsoft.Data.Sqlite
- Intentional omission of legacy DataAdapter/CommandBuilder APIs