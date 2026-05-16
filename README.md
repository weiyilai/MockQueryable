# MockQueryable

Extensions for mocking [Entity Framework Core](https://github.com/dotnet/efcore) async queries like `ToListAsync`, `FirstOrDefaultAsync`, and more using popular mocking libraries such as **Moq**, **NSubstitute**, and **FakeItEasy** — all without hitting the database.

❤️ If you really like the tool, please
👉 [Support the project](https://github.com/sponsors/ramantsitou) or
☕ [Buy me a coffee](https://buymeacoffee.com/romant).

---

## 📦 NuGet Packages

| Package | Latest Version | Install via Package Manager |
|-------- |----------------|-----------------------------|
|[![Download](https://img.shields.io/nuget/dt/MockQueryable.Core.svg?label=MockQueryable.Core)](https://www.nuget.org/packages/MockQueryable.Core/)|[![Version](https://img.shields.io/nuget/v/MockQueryable.Core.svg)](https://www.nuget.org/packages/MockQueryable.Core/)|`Install-Package MockQueryable.Core` |
|[![Download](https://img.shields.io/nuget/dt/MockQueryable.EntityFrameworkCore.svg?label=MockQueryable.EntityFrameworkCore)](https://www.nuget.org/packages/MockQueryable.EntityFrameworkCore/)|[![Version](https://img.shields.io/nuget/v/MockQueryable.EntityFrameworkCore.svg)](https://www.nuget.org/packages/MockQueryable.EntityFrameworkCore/)|`Install-Package MockQueryable.EntityFrameworkCore` |
|[![Download](https://img.shields.io/nuget/dt/MockQueryable.Moq.svg?label=MockQueryable.Moq)](https://www.nuget.org/packages/MockQueryable.Moq/)|[![Version](https://img.shields.io/nuget/v/MockQueryable.Moq.svg)](https://www.nuget.org/packages/MockQueryable.Moq/)|`Install-Package MockQueryable.Moq` |
|[![Download](https://img.shields.io/nuget/dt/MockQueryable.NSubstitute.svg?label=MockQueryable.NSubstitute)](https://www.nuget.org/packages/MockQueryable.NSubstitute/)|[![Version](https://img.shields.io/nuget/v/MockQueryable.NSubstitute.svg)](https://www.nuget.org/packages/MockQueryable.NSubstitute/)|`Install-Package MockQueryable.NSubstitute` |
|[![Download](https://img.shields.io/nuget/dt/MockQueryable.FakeItEasy.svg?label=MockQueryable.FakeItEasy)](https://www.nuget.org/packages/MockQueryable.FakeItEasy/)|[![Version](https://img.shields.io/nuget/v/MockQueryable.FakeItEasy.svg)](https://www.nuget.org/packages/MockQueryable.FakeItEasy/)|`Install-Package MockQueryable.FakeItEasy` |

---

## ✅ Build & Status

[![codecov](https://codecov.io/github/romantitov/MockQueryable/graph/badge.svg?token=dtiYMUNHUo)](https://codecov.io/github/romantitov/MockQueryable)
[![build](https://github.com/romantitov/MockQueryable/workflows/build/badge.svg)](https://github.com/ramantsitou/MockQueryable/actions/workflows/build.yml)
[![release](https://github.com/romantitov/MockQueryable/workflows/release/badge.svg)](https://github.com/ramantsitou/MockQueryable/actions/workflows/release.yml)
[![License](https://img.shields.io/github/license/romantitov/MockQueryable.svg)](https://github.com/romantitov/MockQueryable/blob/master/LICENSE)

---

## ⭐ GitHub Stats

![Stars](https://img.shields.io/github/stars/romantitov/MockQueryable)
![Contributors](https://img.shields.io/github/contributors/romantitov/MockQueryable)
![Last Commit](https://img.shields.io/github/last-commit/romantitov/MockQueryable)
![Commit Activity](https://img.shields.io/github/commit-activity/m/romantitov/MockQueryable)
![Open Issues](https://img.shields.io/github/issues/romantitov/MockQueryable)

---

## 💡 Why Use MockQueryable?

Avoid hitting the real database in unit tests when querying via `IQueryable`:

```csharp
var query = _userRepository.GetQueryable();

await query.AnyAsync(x => ...);
await query.FirstOrDefaultAsync(x => ...);
await query.ToListAsync();
// etc.
```

---

## 🚀 Getting Started

### 1. Create Test Data

```csharp
var users = new List<UserEntity>
{
    new UserEntity { LastName = "Smith", DateOfBirth = new DateTime(2012, 1, 20) },
    // More test data...
};
```

### 2. Build the Mock

```csharp
var mock = users.BuildMock(); // for IQueryable
```

### 3. Set Up in Your favorite Mocking Framework

#### Moq
```csharp
_userRepository.Setup(x => x.GetQueryable()).Returns(mock);
```

#### NSubstitute
```csharp
_userRepository.GetQueryable().Returns(mock);
```

#### FakeItEasy
```csharp
A.CallTo(() => userRepository.GetQueryable()).Returns(mock);
```

---

## 🗃️ Mocking `DbSet<T>`

```csharp
var mockDbSet = users.BuildMockDbSet();

// Moq
var repo = new TestDbSetRepository(mockDbSet.Object);

// NSubstitute / FakeItEasy
var repo = new TestDbSetRepository(mockDbSet);
```

---

## 🔧 Adding Custom Logic

### Example: Custom `FindAsync`

```csharp
mock.Setup(x => x.FindAsync(userId)).ReturnsAsync((object[] ids) =>
{
    var id = (Guid)ids[0];
    return users.FirstOrDefault(x => x.Id == id);
});
```

### Example: Custom Expression Visitor 
Build a mock with the custom [SampleLikeExpressionVisitor](src/MockQueryable/MockQueryable.Sample/SampleLikeExpressionVisitor.cs) for testing `EF.Functions.Like`

```csharp
var mockDbSet = users.BuildMockDbSet<UserEntity, SampleLikeExpressionVisitor>();
```

---

## 🧩 Extend for Other Frameworks

You can even create your own extensions. Check the [example here](https://github.com/romantitov/MockQueryable/blob/master/src/MockQueryable/MockQueryable.Moq/MoqExtensions.cs).

---

## 🔍 Sample Project

See the [sample project](https://github.com/romantitov/MockQueryable/tree/master/src/MockQueryable/MockQueryable.Sample) for working examples.

---






