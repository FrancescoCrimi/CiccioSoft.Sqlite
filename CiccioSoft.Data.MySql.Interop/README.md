# CiccioSoft.Data.MySql.Interop

![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET Version](https://img.shields.io/badge/.NET-10.0-purple.svg)
![Language](https://img.shields.io/badge/language-C%23-brightgreen.svg)

Raw P/Invoke-oriented interop layer for MySQL, modeled after `CiccioSoft.Data.Sqlite.Interop`.

## Current scope

This project introduces the first building blocks for a native MySQL interop package:

- native binding surface (`NativeMySqlClient`) for core C API entry points
- low-level managed wrapper (`MySqlClient`) around `MYSQL*`
- custom exception (`MySqlInteropException`) for native failures

## Example

```csharp
using var client = MySqlClient.Open("127.0.0.1", 3306, "root", "secret", "mydb");
client.Ping();
```

## Notes

- This is an initial scaffold and intentionally minimal.
- Runtime requires a compatible `libmysqlclient` (or equivalent MySQL client library) on the host system.
