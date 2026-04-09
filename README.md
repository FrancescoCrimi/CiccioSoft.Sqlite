# CiccioSoft.Data.Sqlite

![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET Version](https://img.shields.io/badge/.NET-10.0-purple.svg)
![Language](https://img.shields.io/badge/language-C%23-brightgreen.svg)
![Status](https://img.shields.io/badge/status-Educational%20Project-orange.svg)

A lightweight, educational SQLite data access library for .NET 10, featuring a two-layer architecture with raw P/Invoke bindings and idiomatic C# abstractions.

## 📚 About This Project

**CiccioSoft.Data.Sqlite** is a **didactic project** built with the philosophy of **"just for fun"** and learning. It explores how to build SQLite data access layers from the ground up, providing a clear separation between low-level interoperability and high-level OOP abstractions.

Whether you're learning about database interop, P/Invoke bindings, or how to design a clean data access library, this project serves as an educational reference implementation.

## 🎯 Project Focus

This provider is designed with a strong emphasis on **truly asynchronous operations** (no `Task.Run` wrappers) and **WAL journaling enabled by default** for file-backed databases. These choices prioritize performance, concurrency, and modern async patterns in ADO.NET usage.

This repository is organized into multiple projects, each with its own README:

### 1. **CiccioSoft.Data.Sqlite.Interop** (Raw Binding Layer)
- Pure P/Invoke raw bindings to SQLite
- Low-level, unmanaged FFI (Foreign Function Interface)
- Minimal abstraction over native SQLite C library
- Direct SQLite API exposure for advanced use cases
- [📖 Project README](CiccioSoft.Data.Sqlite.Interop/README.md)

### 2. **CiccioSoft.Data.Sqlite** (OOP Abstraction Layer)
- Idiomatic C# object-oriented wrapper
- Higher-level abstractions built on top of `CiccioSoft.Data.Sqlite.Interop`
- Type-safe operations and modern C# patterns
- More accessible API for typical database tasks
- [📖 Project README](CiccioSoft.Data.Sqlite/README.md)

### 3. **CiccioSoft.Data.Sqlite.AsyncConcurrency.Tests** (Test Suite)
- Comprehensive tests for async and concurrency features
- Validation of WAL journaling benefits
- Cancellation token and timeout testing
- [📖 Project README](CiccioSoft.Data.Sqlite.AsyncConcurrency.Tests/README.md)

## ✨ Key Features

- � **Truly Asynchronous**: All async methods are cooperative (no `Task.Run`), supporting cancellation via `CancellationToken` and native interrupt for timeouts.
- 📊 **WAL Journaling by Default**: File-backed databases use WAL mode out-of-the-box for better reader/writer concurrency.
- �🔗 **Two-Layer Architecture**: Clear separation of concerns between raw bindings and managed abstractions
- 🎓 **Educational Focus**: Designed for learning and understanding database interop patterns
- ⚡ **Competitive Performance**: Performance comparable to [SQLitePCL.raw](https://github.com/ericsink/SQLitePCL.raw) and [Microsoft.Data.Sqlite](https://github.com/dotnet/efcore/tree/main/src/Microsoft.Data.Sqlite.Core)
- 🪶 **Lightweight**: Minimal dependencies, focused scope
- 🛡️ **Type-Safe**: Idiomatic C# patterns for modern development
- 🔮 **Future-Ready**: Potential integration with Entity Framework Core down the road

## ⚠️ Concurrency & Async Notes

- The provider serializes native SQLite access internally and is designed to be thread-safe by default for typical concurrent usage from async/sync ADO.NET APIs.
- Async methods are non-blocking for the caller and support cancellation via token-driven native interrupt where applicable.
- `SqliteCommand.CommandTimeout` is enforced on the full command execution scope (statement preparation + execution/reader lifecycle), with native interrupt on timeout. `0` means no timeout, values greater than `0` are interpreted as seconds.
- Connection PRAGMA-like settings are applied at open and can be configured using Microsoft.Data.Sqlite-compatible names: `Busy Timeout`/`busy_timeout`, `Foreign Keys`/`foreign_keys`, and `Journal Mode`/`journal_mode`.
- In WAL mode, you can explicitly trigger maintenance with `SqliteConnection.Checkpoint(...)` (default `PASSIVE`) and `SqliteConnection.Optimize()` / async counterparts to keep `-wal` growth under control in long-running workloads.

## 🔬 ADO.NET Provider Deep Dive (Defaults + Async)

This provider intentionally ships with **opinionated defaults** to reduce accidental misconfiguration in everyday ADO.NET usage.

### Default behaviors at `Open()`

When a connection is opened, the provider applies these defaults unless explicitly overridden in the connection string:

- **Foreign keys ON by default** (`PRAGMA foreign_keys=ON`), to enforce referential integrity.
- **WAL journal mode by default** (`PRAGMA journal_mode=WAL`) for file-backed databases, to improve reader/writer concurrency.
- **Busy timeout default = 30s** (`Busy Timeout=30000` ms), to reduce transient lock failures under contention.

For in-memory scenarios, behavior is intentionally adapted:

- WAL is not used for in-memory databases.
- The provider forces `journal_mode=DELETE` for in-memory connections.
- In `Mode=Memory` configurations, the provider favors shared-cache URI behavior for named memory databases.

### How to override defaults

You can override these defaults explicitly in the connection string:

```ini
Data Source=app.db;Foreign Keys=False;Journal Mode=DELETE;Busy Timeout=5000
```

Aliases compatible with common SQLite conventions are also accepted:

- `foreign_keys` (alias of `Foreign Keys`)
- `journal_mode` (alias of `Journal Mode`)
- `busy_timeout` (alias of `Busy Timeout`)

### Async model: what “true async” means here

SQLite itself is a native embedded engine with synchronous stepping semantics. In this provider, async support is implemented as **cooperative ADO.NET async** with:

- asynchronous ADO.NET method surface (`OpenAsync`, command async methods via DbCommand, `ReadAsync`, `CheckpointAsync`, `OptimizeAsync`);
- cancellation token propagation;
- native `sqlite3_interrupt` usage to stop ongoing work on timeout/cancellation.

In practical terms:

- async calls are cancellable and integrate correctly with `CancellationToken`;
- `CommandTimeout` is enforced across full command lifecycle and translated into native interrupt;
- execution on the same connection is still serialized by design (safety-first over unsafe parallel stepping on a single native handle).

### WAL lifecycle operations exposed by the provider

Beyond enabling WAL, the provider exposes explicit maintenance APIs:

- `SqliteConnection.Checkpoint()` / `CheckpointAsync(...)` with selectable modes (`PASSIVE`, `FULL`, `RESTART`, `TRUNCATE`);
- `SqliteConnection.Optimize()` / `OptimizeAsync()` for lightweight planner/statistics maintenance.

These are useful in long-running applications where you want deterministic WAL file management and periodic planner optimization.

## 🚀 Quick Start

### Requirements

- **.NET 9.0** or later
- SQLite 3.x (embedded with the library)

### Installation

```bash
# Clone the repository
git clone https://github.com/FrancescoCrimi/CiccioSoft.Data.Sqlite.git
cd CiccioSoft.Data.Sqlite

# Build the project
dotnet build
```

### Basic Usage

#### Using the Raw Interop Layer (CiccioSoft.Data.Sqlite.Interop)

```csharp
using CiccioSoft.Data.Sqlite.Interop;

// Direct SQLite API access
using var db = Sqlite3.Open("mydata.db");
// ... raw SQLite operations
// disposed automatically at end of scope
```

#### Using the OOP Layer (CiccioSoft.Data.Sqlite)

```csharp
using CiccioSoft.Data.Sqlite;

// High-level, idiomatic C# interface
using var connection = new SqliteConnection("mydata.db");
connection.Open();

using var command = connection.CreateCommand();
command.CommandText = "SELECT * FROM users WHERE id = @id";
command.Parameters.AddWithValue("@id", 1);

using var reader = command.ExecuteReader();
while (reader.Read())
{
    Console.WriteLine($"Name: {reader["name"]}");
}
```

## 📁 Repository Structure

```
CiccioSoft.Data.Sqlite/
├── CiccioSoft.Data.Sqlite.Interop/           # Raw P/Invoke bindings
│   ├── NativeSqlite3.cs                 # Native function declarations
│   └── *.cs                             # Interop helpers
├── CiccioSoft.Data.Sqlite/              # OOP abstraction layer
│   ├── SqliteConnection.cs              # Connection management
│   ├── SqliteCommand.cs                 # Command execution
│   ├── SqliteDataReader.cs              # Result reading
│   └── *.cs                             # Additional abstractions
├── CiccioSoft.Data.Sqlite.Interop.Example/   # Example applications
│   └── Program.cs                       # Usage examples
├── CiccioSoft.Data.Sqlite.slnx          # Solution file
├── LICENSE                              # MIT License
└── README.md                            # This file
```

## 🔍 Comparison with Related Projects

| Feature | CiccioSoft.Data.Sqlite | [SQLitePCL.raw](https://github.com/ericsink/SQLitePCL.raw) | [Microsoft.Data.Sqlite](https://github.com/dotnet/efcore/tree/main/src/Microsoft.Data.Sqlite.Core) |
|---------|:---------------------:|:------------------------------------------:|:------------------------------------------:|
| Raw Bindings | ✅ | ✅ | ✅ |
| OOP Abstractions | ✅ | ⚠️ (Limited) | ✅ |
| Educational Focus | ✅ | ❌ | ❌ |
| EF Core Integration | 🔮 (Planned) | ✅ | ✅ |
| Performance | ✅ Comparable | ✅ Excellent | ✅ Excellent |
| License | MIT | Apache 2.0 | MIT |
| Purpose | Learning | Production | Production |

## 🧭 Provider Scope Policy

- This project follows an ORM-first ADO.NET provider scope: full ADO.NET core compliance first, then minimal cross-provider extras that are broadly useful for mainstream ADO.NET providers.
- See [PROVIDER_SCOPE.md](./PROVIDER_SCOPE.md) for the formal architecture policy and acceptance criteria.

## 📖 Examples

For practical examples and use cases, see the [CiccioSoft.Data.Sqlite.Interop.Example](./CiccioSoft.Data.Sqlite.Interop.Example/) project.

### Example: Creating a Table

```csharp
using var connection = new SqliteConnection("example.db");
connection.Open();

const string createTableSql = @"
    CREATE TABLE IF NOT EXISTS Users (
        Id INTEGER PRIMARY KEY,
        Name TEXT NOT NULL,
        Email TEXT UNIQUE,
        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
    )";

using var command = connection.CreateCommand();
command.CommandText = createTableSql;
command.ExecuteNonQuery();
```

### Example: Inserting Data

```csharp
const string insertSql = @"
    INSERT INTO Users (Name, Email) 
    VALUES (@name, @email)";

using var command = connection.CreateCommand();
command.CommandText = insertSql;
command.Parameters.AddWithValue("@name", "John Doe");
command.Parameters.AddWithValue("@email", "john@example.com");
int affectedRows = command.ExecuteNonQuery();
```

## 🎓 Learning Resources

This project demonstrates:

- **P/Invoke in C#**: How to call native C functions from managed code
- **SQLite C API**: Understanding low-level database operations
- **Architecture Patterns**: Two-layer abstraction design
- **API Design**: Creating user-friendly database abstractions
- **Interoperability**: Bridging managed and unmanaged code

## 🙏 References & Acknowledgements

This library was inspired by and built thanks to ideas, tooling, and examples from the following open-source projects:

- [SourceGear.sqlite3](https://sqlite.sourcegear.com/) for the SQLite native integration approach and packaging references.
- [ClangSharp](https://github.com/dotnet/ClangSharp) for C/C++ interop generation workflows and ecosystem tooling around .NET bindings.

Many thanks to the maintainers and contributors of these projects for their valuable work.

## 🛣️ Roadmap

- [x] Raw P/Invoke bindings layer
- [x] Basic OOP abstractions
- [x] Example applications
- [ ] Connection pooling
- [ ] Transaction support enhancements
- [x] Async ADO.NET surface with cancellation/interrupt semantics
- [ ] Entity Framework Core integration (future consideration)

## 🤝 Contributing

This is an educational project, but contributions are welcome! If you'd like to:

- Improve the code
- Add examples
- Fix bugs
- Suggest enhancements

Please feel free to open an issue or submit a pull request.

## 📝 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

## 💭 Philosophy

> "Just for fun" and learning. Exploring how modern SQLite data access libraries work under the hood.

This project is not intended for production use, but rather as an educational tool to understand:
- How database libraries are architected
- The relationship between P/Invoke and managed abstractions
- Building clean, idiomatic C# APIs

## 🙋 Questions & Support

- 📚 Check out the [CiccioSoft.Data.Sqlite.Interop.Example](./CiccioSoft.Data.Sqlite.Interop.Example/) for practical examples
- 📬 Open an [Issue](https://github.com/FrancescoCrimi/CiccioSoft.Data.Sqlite/issues) for questions or problems
- 💬 Start a [Discussion](https://github.com/FrancescoCrimi/CiccioSoft.Data.Sqlite/discussions) for ideas and feedback

## 📊 Technical Stack

- **Language**: C# (.NET 9.0)
- **Database**: SQLite 3.x
- **Interop**: P/Invoke
- **Architecture**: Two-layer pattern (Raw Bindings + OOP Abstractions)
- **License**: MIT

---

**Built with ❤️ for learning by [Francesco Crimi](https://github.com/FrancescoCrimi)**

*If you find this project helpful for your learning journey, consider starring ⭐ the repository!*
