# Changelog

All notable changes to Skugga will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.0] - 2026-01-03

### Added
- **Enterprise-Grade Repository Structure** - Professional organization for maintainability
  - Top-level directories: `/src`, `/tests`, `/perf`, `/samples`, `/eng`, `/artifacts`
  - Organized test files by feature: Core/, Advanced/, Verification/, Setup/, Matchers/
  - Organized source files: Generator/, Analyzers/, Core/
  - Microsoft-style conventions throughout
  - All 937 tests passing after reorganization
- **Dual Generator NuGet Packaging** - Both generators included as analyzers
  - Skugga.Generator.dll packaged in `analyzers/dotnet/cs/`
  - Skugga.Core.Generators.dll packaged in `analyzers/dotnet/cs/`
  - Automatic analyzer registration when package installed
  - Build targets auto-configure interceptors
- **CI/CD Improvements** - Updated to latest GitHub Actions
  - actions/checkout: v4 → v6
  - actions/setup-dotnet: v4 → v5
  - actions/upload-artifact: v4 → v6
  - gittools/actions: v1.1.1 → v4.2.0
  - Fixed workflow paths after repository reorganization
  - Explicit generator build steps before NuGet packing
- **Dependency Fixes** - Resolved all version conflicts
  - System.Collections.Immutable version conflicts resolved
  - Added explicit v9.0.0 references to affected test projects
  - Clean builds with TreatWarningsAsErrors enabled
  - 0 warnings, 0 errors across all 18 projects
- **Advanced Chaos Engineering** - Enhanced resilience testing capabilities
  - Timeout simulation: `policy.TimeoutMilliseconds = 100` simulates slow responses or network delays
  - Configurable random seed: `policy.Seed = 12345` for reproducible chaos scenarios in tests
  - Chaos statistics tracking: Monitor `ChaosStatistics` for total invocations, failure count, and actual failure rate
  - Reset statistics: `handler.ChaosStatistics.Reset()` to clear counters between test scenarios
  - Timeout tracking: Separate counter for timeout/delay triggers
  - Backwards compatible: Existing chaos tests continue to work unchanged
- **Enhanced AutoScribe** - Extended recording and replay capabilities
  - Timing capture: `RecordedCall.DurationMilliseconds` tracks execution time for async methods
  - Export to JSON: `AutoScribe.ExportToJson(recordings)` for structured data analysis
  - Export to CSV: `AutoScribe.ExportToCsv(recordings)` for spreadsheet analysis
  - Replay context: `AutoScribe.CreateReplayContext(recordings)` for verifying behavior matches recordings
  - Replay verification: `context.VerifyNextCall(methodName, args)` validates call sequences
  - Replay reset: `context.Reset()` to restart sequence validation
  - Timestamp tracking: All recorded calls include `Timestamp` for temporal analysis
- **Advanced Performance Monitoring** - Comprehensive allocation testing
  - Allocation thresholds: `AssertAllocations.AtMost(action, maxBytes)` for setting memory limits
  - Detailed reports: `AssertAllocations.Measure(action, name)` returns `AllocationReport` with bytes, duration, and GC stats
  - Performance thresholds: `AssertAllocations.Threshold(name, maxBytes, maxMs)` for configurable limits
  - Threshold validation: `AssertAllocations.MeetsThreshold(action, threshold)` validates performance constraints
  - GC collection tracking: Reports Gen0, Gen1, Gen2 collection counts during execution
  - Formatted reports: `AllocationReport.ToString()` provides human-readable summaries
  - Zero-allocation testing: Original `AssertAllocations.Zero()` still available for strictest validation
- 21 new tests for advanced features (201 total tests, up from 180)
  - 7 new Chaos Engineering tests covering seed, timeout, and statistics
  - 8 new AutoScribe tests for export and replay functionality
  - 6 new AssertAllocations tests for thresholds and detailed reports

- **Comprehensive Documentation** - Production-ready documentation for developers
  - Complete API reference guide (API_REFERENCE.md) with all features documented
  - Troubleshooting section in README with common issues and solutions
  - Moq migration guide with API comparison table and code examples
  - Best practices with do/don't examples for proper mock usage
  - Advanced scenarios including complex type matching and timeout handling
  - Generator diagnostics documentation (SKUGGA001, SKUGGA002)
- **Generator Performance Optimizations** - Faster build times and reduced memory usage
  - Stable hash generation using FNV-1a algorithm for consistent class names across builds
  - String allocation optimizations with constants and StringBuilder pre-allocation (2048 chars)
  - Replaced string interpolation with direct Append calls to reduce allocations
  - Leverages IncrementalGenerator caching and parallel processing built into Roslyn
- **Generator Enhancements** - Improved code generation quality and developer experience
  - XML documentation comments on all generated mock classes and constructors
  - Proper formatting with multi-line methods and properties for better readability
  - Generator diagnostics: `SKUGGA001` error for sealed classes, `SKUGGA002` warning for classes without virtual members
  - Fixed double spaces in generated method signatures (was "public  string", now "public string")
  - Improved "Go to Definition" experience with well-formatted generated code
- **SetupSequence** - Configure methods to return different values on consecutive calls
  - `mock.SetupSequence(x => x.GetNext()).Returns(1).Returns(2).Returns(3)` - Returns 1, then 2, then 3, then repeats last value
  - `.Throws(exception)` - Throw exception on specific invocation in sequence
  - Works with methods and properties
  - 9 comprehensive tests covering sequences, exceptions, and edge cases
  - Perfect for testing retry logic, pagination, and stateful scenarios
- **Additional Matchers** - Flexible argument matching for Setup and Verify
  - `It.Is<T>(Func<T, bool> predicate)` - Match values based on custom predicate logic
  - `It.IsIn<T>(params T[] values)` - Match values present in specified set
  - `It.IsNotNull<T>()` - Match only non-null values
  - `It.IsRegex(string pattern)` - Match strings using regex patterns
  - All matchers work seamlessly with Setup and Verify
  - 20 comprehensive tests covering all matcher types and combinations
  - Examples: `mock.Setup(x => x.Process(It.Is<int>(n => n > 0))).Returns("positive")`
  - Examples: `mock.Setup(x => x.Handle(It.IsIn("red", "green", "blue"))).Returns("color")`
  - Examples: `mock.Setup(x => x.ValidateEmail(It.IsRegex(@"^[\w\.-]+@[\w\.-]+\.\w+$"))).Returns(true)`
- **AutoScribe** - Self-writing test feature using compile-time recording proxies
  - `AutoScribe.Capture<T>(realImpl)` creates recording proxy that wraps real implementations
  - Automatically generates test setup code by logging method calls and return values
  - Zero reflection - uses source generator for compile-time proxy generation
  - Outputs ready-to-use mock setup code to console: `[AutoScribe] mock.Setup(x => x.GetData(101)).Returns("Real_Data_101");`
  - Supports all method types: sync, async, void, generic methods, complex return types
  - 18 comprehensive tests covering generics, nullable types, collections, async/await, tuples
  - Works with interface implementations to capture real behavior for test scaffolding
  - Unique differentiator - no other mocking framework has this feature
- Comprehensive unit test suite with 170 tests covering core functionality
- FluentAssertions for expressive test assertions
- CONTRIBUTING.md with detailed contribution guidelines
- Enhanced NuGet package metadata with source link support
- Code coverage collection in CI pipeline
- Matrix testing strategy for .NET 8.0 and 10.0
- Build summaries in GitHub Actions
- XML documentation comments for Setup API methods
- Unified Setup method that handles both methods and properties
- **Verify API** for call verification with `mock.Verify(x => x.Method(), Times.Once())`
- **Times helper class** with `Once()`, `Never()`, `Exactly(n)`, `AtLeast(n)`, `AtMost(n)`, `Between(m, n)`
- **Invocation tracking** in MockHandler for verification support
- **Callback support** for Setup API with fluent chaining
  - Callbacks for void methods: `mock.Setup(x => x.Execute()).Callback(() => ...)`
  - Callbacks with arguments: `mock.Setup(x => x.Process(42)).Callback((int value) => ...)`
  - Support for 1-3 argument callbacks
  - Fluent chaining: `.Callback(...).Returns(...)` or `.Returns(...).Callback(...)`
  - Separate `VoidSetupContext` for void methods to ensure type safety
- **Returns with function callbacks** for computing return values dynamically
  - Parameterless functions: `mock.Setup(x => x.GetValue()).Returns(() => DateTime.Now.Ticks)`
  - Functions with 1-3 arguments: `mock.Setup(x => x.Double(5)).Returns((int x) => x * 2)`
  - Function results evaluated on each invocation (not cached)
  - Works with fluent chaining and callbacks
  - Can override function returns with static values and vice versa
- **Sequential returns (ReturnsInOrder)** for different values on successive calls
  - Return different values on each call: `mock.Setup(x => x.GetNext()).ReturnsInOrder("first", "second", "third")`
  - IEnumerable support: `mock.Setup(x => x.GetNext()).ReturnsInOrder(collection)`
  - Repeats last value when sequence exhausted
  - Works with all data types including nulls
  - Integrates with Callback and Verify APIs
  - Can override static Returns and Returns with functions
- **Argument matching with It.IsAny<T>()** for flexible Setup and Verify
  - Match any value: `mock.Setup(x => x.Process(It.IsAny<int>())).Returns("result")`
  - Works with Setup, Verify, and all return strategies (static, functions, sequential)
  - Mix matchers with specific values: `mock.Setup(x => x.Process(42, It.IsAny<string>()))`
  - Supports multiple arguments with matchers
  - Type-safe matching validates type compatibility
- 16 comprehensive tests for Verify API covering all Times scenarios
- 16 comprehensive tests for Callback API covering all scenarios
- 18 comprehensive tests for Returns with functions covering all argument counts
- 18 comprehensive tests for ReturnsInOrder covering sequential behavior and edge cases
- 17 comprehensive tests for It.IsAny<T>() covering all matching scenarios

### Changed
- Improved CI/CD workflows with better error handling and reporting
- Enhanced README with test coverage badge and contribution links
- Package description now emphasizes compile-time and AOT benefits
- Simplified property setup API - now uses `mock.Setup(x => x.Property)` instead of `mock.Setup<T, TReturn>(x => x.Property)`
- `MockSetup.Value` is now settable to support fluent callback/returns chaining
- `MockHandler.AddSetup` now returns the created `MockSetup` instance
- Setup context classes now track their associated `MockSetup` for fluent API
- `MockSetup` now has `ValueFactory` property for function-based returns
- `MockHandler.Invoke` evaluates `ValueFactory` at invocation time if present
- `Returns(value)` now clears `ValueFactory` when setting static value
- `MockSetup` now has `SequentialValues` array and index tracking for sequential returns
- `MockHandler.Invoke` prioritizes sequential values over factory/static values
- `ReturnsInOrder` clears other return configurations to prevent conflicts
- `GetArgumentValue` now detects `It.IsAny<T>()` calls and creates `ArgumentMatcher` instances
- `Invocation.Matches` and `MockSetup.Matches` now support `ArgumentMatcher` for flexible matching

### Fixed
- VERSION variable assignment in publish.yml workflow
- Test result artifact upload configuration
- Deterministic build support for reproducible packages
- Property setup method overload ambiguity - no longer requires explicit type parameters
- Nullable reference warnings in SkuggaCore.cs (lines 18, 151)

## [1.0.0] - Initial Release

### Added
- Core mocking functionality with `Mock.Create<T>()`
- Support for method and property setup via `Setup()` and `Returns()`
- Strict and Loose mock behaviors
- Chaos mode for resilience testing
- AutoScribe for self-writing tests
- AssertAllocations for zero-allocation validation
- TestHarness for simplified test setup
- Roslyn source generator for compile-time mock generation
- C# 12 interceptors for zero-overhead method replacement
- Native AOT compatibility
- Benchmarks comparing with Moq and NSubstitute

### Features
- Zero reflection - fully compile-time
- Native AOT compatible
- 4-7x faster than reflection-based alternatives
- Minimal memory footprint
- Distroless container support

[Unreleased]: https://github.com/Digvijay/Skugga/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/Digvijay/Skugga/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/Digvijay/Skugga/releases/tag/v1.0.0
