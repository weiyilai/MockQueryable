# MockQueryable Architecture Guide

## Overview

MockQueryable is a lightweight testing library that enables mocking of `IQueryable<T>` and Entity Framework Core asynchronous query operations without requiring a real database.

The library allows unit tests to execute LINQ expressions and EF Core async extensions such as:

* `ToListAsync()`
* `FirstOrDefaultAsync()`
* `SingleOrDefaultAsync()`
* `AnyAsync()`
* `CountAsync()`
* `ContainsAsync()`

using in-memory collections while preserving the query semantics expected by production code.

---

# Problem Statement

When testing services that depend on Entity Framework Core repositories, developers often encounter one of the following approaches:

### Option 1: Real Database

Pros:

* Highest fidelity

Cons:

* Slow
* Infrastructure dependencies
* Harder test isolation

### Option 2: EF Core InMemory Provider

Pros:

* Easy setup

Cons:

* Query behavior may differ from relational providers
* Not ideal for pure unit testing

### Option 3: Mocking IQueryable

Pros:

* Fast
* Extendable
* Deterministic
* No infrastructure required

Cons:

* Native mocking frameworks do not support EF Core async queries

MockQueryable addresses the third option by providing a fully asynchronous query provider implementation.

---

# High-Level Architecture

```text
┌─────────────────────────────────────┐
│ Test Data Collection                │
│ List<User>                          │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│ BuildMock()                         │
│ BuildMockDbSet()                    │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│ TestAsyncEnumerableEfCore           │
│ IAsyncEnumerable<T>                 │
│ IAsyncQueryProvider                 │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│ TestQueryProvider                   │
│ IQueryable<T>                       │
│ IQueryProvider                      │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│ Expression Visitor Pipeline         │
│ Expression Rewriting                │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│ LINQ Execution Against Memory       │
└─────────────────────────────────────┘
```

---

# Solution Components

## MockQueryable.Core

Core abstraction layer.

Contains:

### TestQueryProvider

Responsible for:

* Implementing `IQueryable<T>`
* Implementing `IQueryProvider`
* Creating nested query providers
* Executing expression trees
* Compiling LINQ expressions against in-memory collections

Key responsibilities:

```csharp
public abstract class TestQueryProvider<T, TExpressionVisitor>
```

This class acts as the engine of the framework.

Execution flow:

1. Receive expression tree
2. Apply expression visitor
3. Build lambda expression
4. Compile expression
5. Execute against in-memory data

---

### TestExpressionVisitor

Default implementation:

```csharp
public class TestExpressionVisitor : ExpressionVisitor
```

Acts as a no-op visitor.

Can be replaced with custom visitors to simulate provider-specific behavior.

---

## MockQueryable.EntityFrameworkCore

EF Core integration layer.

Provides support for:

* `IAsyncEnumerable<T>`
* `IAsyncQueryProvider`
* EF Core async LINQ operators

---

### TestAsyncEnumerableEfCore

```csharp
public class TestAsyncEnumerableEfCore<T, TExpressionVisitor>
```

Implements:

```csharp
IAsyncEnumerable<T>
IAsyncQueryProvider
```

Responsibilities:

* Async query execution
* Async enumeration
* EF Core compatibility
* Delegating synchronous execution to TestQueryProvider

This class is the bridge between LINQ and EF Core async APIs.

---

### TestAsyncEnumerator

Provides asynchronous iteration support.

```csharp
public class TestAsyncEnumerator<T>
```

Implements:

```csharp
IAsyncEnumerator<T>
```

Used by:

```csharp
await foreach(...)
```

and EF Core async operators.

---

# Mocking Framework Integrations

The architecture separates query execution from mocking frameworks.

```text
                 Core Engine
                       │
       ┌───────────────┼───────────────┐
       │               │               │
       ▼               ▼               ▼
 MockQueryable.Moq  FakeItEasy  NSubstitute
```

This design allows:

* Framework independence
* Consistent behavior
* Easy future extensions

---

## MockQueryable.Moq

Provides:

```csharp
BuildMock()
BuildMockDbSet()
```

extensions for Moq.

Example:

```csharp
var users = TestData.Users();

var mock = users.BuildMock();

repository
    .Setup(x => x.GetQueryable())
    .Returns(mock);
```

---

## MockQueryable.NSubstitute

Provides identical functionality for NSubstitute.

```csharp
repository
    .GetQueryable()
    .Returns(mock);
```

---

## MockQueryable.FakeItEasy

Provides identical functionality for FakeItEasy.

```csharp
A.CallTo(() => repository.GetQueryable())
    .Returns(mock);
```

---

# Expression Visitor Pipeline

One of the most powerful features of MockQueryable is expression rewriting.

Custom expression visitors allow developers to emulate behavior normally provided by a database provider.

Example:

```csharp
BuildMockDbSet<UserEntity, SampleLikeExpressionVisitor>()
```

Execution flow:

```text
Original Expression
          │
          ▼
Expression Visitor
          │
          ▼
Rewritten Expression
          │
          ▼
Compiled Lambda
          │
          ▼
Execution
```

---

## Example: EF.Functions.Like

Production query:

```csharp
.Where(x =>
    EF.Functions.Like(
        x.Name,
        "%john%"
    ))
```

Normally this requires SQL translation.

Using a custom visitor:

```csharp
public class SampleLikeExpressionVisitor
    : ExpressionVisitor
{
}
```

the expression can be rewritten to:

```csharp
x.Name.Contains("john")
```

allowing execution in memory.

---

# Query Lifecycle

## Step 1

Create data:

```csharp
var users = new List<User>();
```

---

## Step 2

Build mock:

```csharp
var mock = users.BuildMock();
```

---

## Step 3

Inject into repository:

```csharp
repository.Setup(
    x => x.GetQueryable()
)
.Returns(mock);
```

---

## Step 4

Execute application code:

```csharp
await service.GetUsers();
```

---

## Step 5

Expression evaluation:

```text
Service
  ↓
Repository
  ↓
MockQueryable
  ↓
Expression Visitor
  ↓
Compiled Query
  ↓
In-Memory Collection
```

---

# Design Principles

## Separation of Concerns

Core query execution is isolated from:

* EF Core
* Moq
* NSubstitute
* FakeItEasy

---

## Extensibility

Custom providers can be added without changing the core engine.

Examples:

* CosmosDB-like operators
* PostgreSQL-specific functions
* Custom domain-specific LINQ extensions

---

## Zero Infrastructure

Tests run:

* Without SQL Server
* Without PostgreSQL
* Without Docker
* Without EF Core InMemory provider

---

# Performance Characteristics

Because queries execute against in-memory collections:

### Advantages

* Very fast execution
* Deterministic tests
* No I/O
* No network latency

### Limitations

* Does not execute SQL
* Does not validate SQL translation
* Does not reproduce query plans
* Cannot detect provider-specific runtime SQL issues

For those scenarios, integration tests should be used.

---

# Recommended Testing Strategy

| Test Type        | Tool          |
| ---------------- | ------------- |
| Domain Logic     | MockQueryable |
| Service Layer    | MockQueryable |
| Repository Logic | Real Provider |
| SQL Translation  | Real Provider |
| Migrations       | Real Database |
| End-to-End Tests | Real Database |

---

# Extension Points

## Custom Expression Visitor

```csharp
public class MyVisitor : ExpressionVisitor
{
}
```

Usage:

```csharp
var mock = users
    .BuildMock<User, MyVisitor>();
```

---

## Custom DbSet Behavior

Additional behavior can be configured for methods such as:

```csharp
FindAsync()
AddAsync()
Remove()
```

Example:

```csharp
mockDbSet
    .Setup(x => x.FindAsync(id))
    .ReturnsAsync(entity);
```

---

# Architecture Summary

MockQueryable is built around a simple but powerful architecture:

1. In-memory collections act as the data source.
2. LINQ expressions are captured as expression trees.
3. Expression visitors optionally rewrite provider-specific operations.
4. Expressions are compiled and executed in memory.
5. Async EF Core APIs are emulated through custom implementations of:

   * `IAsyncEnumerable<T>`
   * `IAsyncQueryProvider`
   * `IAsyncEnumerator<T>`

The result is a lightweight, extensible, and framework-agnostic solution for testing applications that rely on Entity Framework Core query semantics without requiring a real database.
