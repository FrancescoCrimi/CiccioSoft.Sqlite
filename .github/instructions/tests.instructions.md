---
name: tests-instructions
description: "File-scoped instructions for test projects (CiccioSoft.Data.Sqlite.Tests/ and Tests.Extra/). Apply to unit tests, integration tests, async tests, and concurrency tests."
applyTo: "**/CiccioSoft.Data.Sqlite.Tests*/**/*.cs"
---

# Test Instructions

This file applies to all C# source files in `CiccioSoft.Data.Sqlite.Tests/` and `CiccioSoft.Data.Sqlite.Tests.Extra/`.

## Test Organization

### Contract Tests (`*Test.cs`)

Test ADO.NET interface compliance:
- **File**: `SqliteConnectionTest.cs`
- **Focus**: `DbConnection` interface behavior
- **Patterns**: Connection state, pooling, transactions

Example:
```csharp
public class SqliteConnectionTest : IDisposable
{
    private readonly SqliteConnection _connection;
    
    public SqliteConnectionTest()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
    }
    
    public void Dispose() => _connection?.Dispose();
    
    [Fact]
    public void State_InitiallyClosedBeforeOpen()
    {
        Assert.Equal(ConnectionState.Closed, _connection.State);
    }
}
```

### Behavior Tests (`*Tests.cs`)

Test semantics and edge cases:
- **File**: `AsyncConcurrencyTests.cs`, `SqliteBehaviorTests.cs`
- **Focus**: Async/concurrency, WAL mode, timeouts
- **Patterns**: Task coordination, semaphores, multiple connections

## XUnit Framework

- **Framework**: XUnit 2.9+
- **Coverage**: coverlet.collector
- **Tipi Test**: `[Fact]` (semplici), `[Theory]` (parametrizzati), `[InlineData]` (parametri)

## Test Patterns

### Simple Unit Test

```csharp
[Fact]
public void MethodName_Condition_ExpectedResult()
{
    // Arrange
    var connection = new SqliteConnection("Data Source=:memory:");
    connection.Open();
    
    // Act
    var result = connection.GetSchema("Tables");
    
    // Assert
    Assert.NotNull(result);
}
```

### Parameterized Test (Theory)

```csharp
[Theory]
[InlineData("DELETE", "DELETE")]
[InlineData("WAL", "WAL")]
public void JournalMode_CanBeConfigured(string input, string expected)
{
    var conn = new SqliteConnection($"Data Source=:memory:;Journal Mode={input}");
    conn.Open();
    
    // Verify journal mode...
}
```

### Async Test with Cancellation

```csharp
[Fact]
public async Task OpenAsync_RespectsUseCancellationToken()
{
    var connection = new SqliteConnection("Data Source=:memory:");
    
    var cts = new CancellationTokenSource();
    cts.CancelAfter(TimeSpan.FromMilliseconds(100));
    
    await Assert.ThrowsAsync<OperationCanceledException>(
        () => connection.OpenAsync(cts.Token)
    );
}
```

### Concurrent Operations Test

```csharp
[Fact]
public async Task WALMode_AllowsConcurrentReadWrite()
{
    var connRead = new SqliteConnection("Data Source=test.db;Journal Mode=WAL");
    var connWrite = new SqliteConnection("Data Source=test.db;Journal Mode=WAL");
    
    connRead.Open();
    connWrite.Open();
    
    // Use SemaphoreSlim to coordinate if needed
    var readComplete = new SemaphoreSlim(0);
    
    var readTask = Task.Run(async () =>
    {
        using var cmd = connRead.CreateCommand();
        cmd.CommandText = "SELECT * FROM data";
        await cmd.ExecuteReaderAsync();
        
        readComplete.Release();
    });
    
    var writeTask = Task.Run(async () =>
    {
        await readComplete.WaitAsync();  // Wait for read to start
        
        using var cmd = connWrite.CreateCommand();
        cmd.CommandText = "INSERT INTO data VALUES (1)";
        await cmd.ExecuteNonQueryAsync();
    });
    
    await Task.WhenAll(readTask, writeTask);
}
```

## Resource Cleanup (IDisposable)

```csharp
public class SqliteConnectionTest : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteConnectionTest()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection?.Dispose();  // Always clean up
    }

    [Fact]
    public void Test_UsesCleanConnection()
    {
        // _connection is ready for use
    }
}
```

## Async Test Requirements

- **Accept CancellationToken**: All async methods must support cancellation
- **Check on entry**: `cancellationToken.ThrowIfCancellationRequested()`
- **Test timeout**: Verify `CommandTimeout` enforcement
- **Test cancellation**: Verify `OperationCanceledException` is thrown
- **No blocking**: Never use `.Result` or `.Wait()` in async tests

```csharp
[Fact]
public async Task ExecuteNonQueryAsync_EnforcesCommandTimeout()
{
    using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();
    
    using var cmd = connection.CreateCommand();
    cmd.CommandText = "SELECT * FROM large_table";
    cmd.CommandTimeout = 1;  // 1 second
    
    // This should timeout
    await Assert.ThrowsAsync<SqliteException>(
        () => cmd.ExecuteNonQueryAsync()
    );
}
```

## Concurrent Test Synchronization

Avoid `Thread.Sleep`; use synchronization primitives:

```csharp
// ✅ CORRECT: SemaphoreSlim for async coordination
var signal = new SemaphoreSlim(0);

var task1 = Task.Run(async () =>
{
    // Do work
    signal.Release();
});

var task2 = Task.Run(async () =>
{
    await signal.WaitAsync();  // Wait for task1
    // Continue work
});

await Task.WhenAll(task1, task2);

// ✅ CORRECT: Barrier for multiple threads
var barrier = new Barrier(3);
var tasks = Enumerable.Range(0, 3).Select(i => Task.Run(() =>
{
    barrier.SignalAndWait();
    // All 3 run together
})).ToArray();

await Task.WhenAll(tasks);

// ❌ WRONG: Thread.Sleep
// await Task.Delay(100);  // Don't use in tests!
```

## Assertions

Use xUnit assertions:

```csharp
Assert.Equal(expected, actual);           // Value equality
Assert.NotEqual(unexpected, actual);      // Value inequality
Assert.True(condition);                   // Boolean
Assert.False(condition);
Assert.Null(value);                       // Null check
Assert.NotNull(value);
Assert.Throws<Exception>(() => { });      // Sync exception
await Assert.ThrowsAsync<Exception>(async () => { });  // Async exception
Assert.Empty(collection);
Assert.NotEmpty(collection);
Assert.Contains(item, collection);
```

## Build & Run Tests

```bash
# All tests
dotnet test CiccioSoft.Data.Sqlite.slnx

# Specific test class
dotnet test CiccioSoft.Data.Sqlite.slnx --filter ClassName=AsyncConcurrencyTests

# With verbose output
dotnet test CiccioSoft.Data.Sqlite.slnx --verbosity detailed

# Coverage report
dotnet test CiccioSoft.Data.Sqlite.slnx /p:CollectCoverage=true /p:CoverageFormat=opencover
```

## Test Data Setup

- **In-memory**: `"Data Source=:memory:"` for isolated tests
- **File-based**: Use `Path.GetTempFileName()` or test fixtures
- **Cleanup**: Always dispose connections and delete temp files

```csharp
[Fact]
public void FileDatabase_PersistedBetweenConnections()
{
    string dbPath = Path.GetTempFileName();
    try
    {
        // Write data
        using (var conn = new SqliteConnection($"Data Source={dbPath}"))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE T (id INT); INSERT INTO T VALUES (1)";
            cmd.ExecuteNonQuery();
        }
        
        // Read data in new connection
        using (var conn = new SqliteConnection($"Data Source={dbPath}"))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM T";
            Assert.Equal(1L, cmd.ExecuteScalar());
        }
    }
    finally
    {
        File.Delete(dbPath);
    }
}
```

## Common Test Pitfalls

- **Forgetting async**: Use `Task` return type, `await`, not `.Result`
- **Not checking cancellation**: Test `cancellationToken.ThrowIfCancellationRequested()`
- **Hardcoded delays**: Use `SemaphoreSlim.WaitAsync()` or `CancellationTokenSource.CancelAfter()`
- **Ignoring disposal**: Always use `using` or `IDisposable` in test classes
- **Race conditions**: Use barriers or semaphores for multi-threaded tests
- **Ignoring timeout tests**: Always test `CommandTimeout` enforcement

## Examples to Follow

- [SqliteConnectionTest.cs](.github/../CiccioSoft.Data.Sqlite.Tests/SqliteConnectionTest.cs) — Contract tests
- [AsyncConcurrencyTests.cs](.github/../CiccioSoft.Data.Sqlite.Tests.Extra/AsyncConcurrencyTests.cs) — Async/WAL tests
- [SqliteParameterBindingParityTests.cs](.github/../CiccioSoft.Data.Sqlite.Tests.Extra/SqliteParameterBindingParityTests.cs) — Parameterized tests
