# CiccioSoft.Data.Sqlite

![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET Version](https://img.shields.io/badge/.NET-9.0-purple.svg)
![Language](https://img.shields.io/badge/language-C%23-brightgreen.svg)
![Status](https://img.shields.io/badge/status-Educational%20Project-orange.svg)

A lightweight, educational SQLite data access library for .NET 9, featuring a two-layer architecture with raw P/Invoke bindings and idiomatic C# abstractions.

## 📚 About This Project

**CiccioSoft.Data.Sqlite** is a **didactic project** built with the philosophy of **"just for fun"** and learning. It explores how to build SQLite data access layers from the ground up, providing a clear separation between low-level interoperability and high-level OOP abstractions.

Whether you're learning about database interop, P/Invoke bindings, or how to design a clean data access library, this project serves as an educational reference implementation.

## 🎯 Project Structure

This repository is organized into two main layers:

### 1. **CiccioSoft.Sqlite.Interop** (Raw Binding Layer)
- Pure P/Invoke raw bindings to SQLite
- Low-level, unmanaged FFI (Foreign Function Interface)
- Minimal abstraction over native SQLite C library
- Direct SQLite API exposure for advanced use cases

### 2. **CiccioSoft.Data.Sqlite** (OOP Abstraction Layer)
- Idiomatic C# object-oriented wrapper
- Higher-level abstractions built on top of `CiccioSoft.Sqlite.Interop`
- Type-safe operations and modern C# patterns
- More accessible API for typical database tasks

## ✨ Key Features

- 🔗 **Two-Layer Architecture**: Clear separation of concerns between raw bindings and managed abstractions
- 🎓 **Educational Focus**: Designed for learning and understanding database interop patterns
- ⚡ **Competitive Performance**: Performance comparable to [SQLitePCL.raw](https://github.com/ericsink/SQLitePCL.raw) and [Microsoft.Data.Sqlite](https://github.com/dotnet/efcore/tree/main/src/Microsoft.Data.Sqlite.Core)
- 🪶 **Lightweight**: Minimal dependencies, focused scope
- 🛡️ **Type-Safe**: Idiomatic C# patterns for modern development
- 🔮 **Future-Ready**: Potential integration with Entity Framework Core down the road

## ⚠️ Concurrency & Async Notes

- SQLite does not provide true async I/O APIs: ADO.NET async members in this project are cooperative wrappers around synchronous native calls.
- A single SQLite connection should not be used concurrently for multiple active commands/readers.
- For parallel work, prefer one connection per worker/thread/task (optionally with pooling enabled).
- Enabling `WAL` mode (`Journal Mode=WAL` in connection string) improves read/write concurrency (many readers + one writer), but does **not** make one connection safe for concurrent use from multiple threads.

### Recommended pattern for real parallel work

If you need effective parallelism, open **one connection per task** (same connection string, pooling enabled) and use `WAL`.

```csharp
const string cs = "Data Source=app.db;Pooling=True;Journal Mode=WAL;";

Task[] jobs = Enumerable.Range(0, 8)
    .Select(_ => Task.Run(() =>
    {
        using var conn = new SqliteConnection(cs);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Users";
        _ = cmd.ExecuteScalar();
    }))
    .ToArray();

await Task.WhenAll(jobs);
```

This gives scalable concurrency at provider level without sharing one `SqliteConnection` instance across threads.

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

#### Using the Raw Interop Layer (CiccioSoft.Sqlite.Interop)

```csharp
using CiccioSoft.Sqlite.Interop;

// Direct SQLite API access
var db = sqlite3.open("mydata.db");
// ... raw SQLite operations
sqlite3.close(db);
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
├── CiccioSoft.Sqlite.Interop/           # Raw P/Invoke bindings
│   ├── sqlite3.cs                       # Native function declarations
│   └── *.cs                             # Interop helpers
├── CiccioSoft.Data.Sqlite/              # OOP abstraction layer
│   ├── SqliteConnection.cs              # Connection management
│   ├── SqliteCommand.cs                 # Command execution
│   ├── SqliteDataReader.cs              # Result reading
│   └── *.cs                             # Additional abstractions
├── CiccioSoft.Sqlite.Interop.Example/   # Example applications
│   └── Program.cs                       # Usage examples
├── CiccioSoft.Data.Sqlite.sln           # Solution file
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

## 📖 Examples

For practical examples and use cases, see the [CiccioSoft.Sqlite.Interop.Example](./CiccioSoft.Sqlite.Interop.Example/) project.

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

## 🛣️ Roadmap

- [x] Raw P/Invoke bindings layer
- [x] Basic OOP abstractions
- [x] Example applications
- [ ] Connection pooling
- [ ] Transaction support enhancements
- [ ] Async operations
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

- 📚 Check out the [CiccioSoft.Sqlite.Interop.Example](./CiccioSoft.Sqlite.Interop.Example/) for practical examples
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
