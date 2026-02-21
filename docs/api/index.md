# API Overview

Complete API reference for the Skugga mocking library.

## Mock Lifecycle

| Method | Description |
|--------|-------------|
| `Mock.Create<T>()` | Create a mock with `Loose` behavior |
| `Mock.Create<T>(MockBehavior.Strict)` | Create a strict mock |
| `Mock.Get<T>(object)` | Retrieve mock interface from a mocked object |
| `Mock.Of<T>()` | LINQ-style mock creation |

## Setup API

| Method | Description |
|--------|-------------|
| `.Setup(x => x.Method(args))` | Setup method behavior |
| `.Setup(x => x.Property)` | Setup property getter |
| `.SetupSet(x => x.Property = value)` | Setup property setter |
| `.SetupSequence(x => x.Method(args))` | Setup sequential returns |
| `.Protected().Setup<T>(name, args)` | Setup protected members |

## Returns API

| Method | Description |
|--------|-------------|
| `.Returns(value)` | Return a fixed value |
| `.ReturnsAsync(value)` | Return an async value |
| `.Throws(exception)` | Throw an exception |
| `.Throws<TException>()` | Throw a typed exception |
| `.Callback<T>(action)` | Execute callback |

## Verify API

| Method | Description |
|--------|-------------|
| `.Verify(x => x.Method(args))` | Verify method was called |
| `.Verify(expr, Times)` | Verify with call count |
| `.VerifyAll()` | Verify all verifiable setups |
| `.VerifyNoOtherCalls()` | No unexpected calls |
| `.VerifySet(x => x.Prop = val)` | Verify property setter |

## Argument Matchers

| Matcher | Description |
|---------|-------------|
| `It.IsAny<T>()` | Match any value |
| `It.Is<T>(predicate)` | Match with predicate |
| `It.IsIn<T>(values)` | Match value in set |
| `It.IsNotNull<T>()` | Match non-null |
| `It.IsRegex(pattern)` | Match regex |
| `It.Ref<T>.IsAny` | Match ref/out params |
| `Match.Create<T>(pred)` | Custom matcher |

## Times

| Value | Description |
|-------|-------------|
| `Times.Once` | Exactly once |
| `Times.Never` | Never called |
| `Times.Exactly(n)` | Exactly n times |
| `Times.AtLeast(n)` | At least n times |
| `Times.AtMost(n)` | At most n times |
| `Times.Between(min, max)` | Between min and max |

## Exclusive Features

| API | Description |
|-----|-------------|
| `mock.Chaos(policy => { ... })` | Configure chaos engineering |
| `mock.GetChaosStatistics()` | Get chaos metrics |
| `AssertAllocations.Zero(action)` | Assert zero allocations |
| `AssertAllocations.AtMost(action, bytes)` | Assert allocation budget |
| `AssertAllocations.Measure(action)` | Measure allocations |
| `AutoScribe.Capture<T>(impl)` | Record interactions |
| `[SkuggaFromOpenApi("spec")]` | Generate from OpenAPI |

## MockRepository

| Method | Description |
|--------|-------------|
| `new MockRepository(behavior)` | Create repository |
| `repo.Create<T>()` | Create attached mock |
| `repo.Register(mock)` | Register existing mock |
| `repo.VerifyAll()` | Verify all mocks |
| `repo.VerifyNoOtherCalls()` | No unexpected calls |

## Diagnostics

| Code | Description |
|------|-------------|
| `SKUGGA001` | Cannot mock sealed class |
| `SKUGGA002` | Class has no virtual members |
| `SKUGGA_OPENAPI_*` | OpenAPI generation errors |
| `SKUGGA_LINT_*` | OpenAPI linting warnings |
