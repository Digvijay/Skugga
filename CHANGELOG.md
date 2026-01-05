# Changelog

All notable changes to Skugga will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **Spectral-Inspired OpenAPI Linting** - Enhancement #10
  - 16 linting rules for OpenAPI quality enforcement at build time
  - Info section rules: info-contact, info-description, info-license
  - Operation rules: operationId, tags, description, summary, success-response, parameters
  - Path rules: path-parameters, no-identical-paths
  - Tag rules: tag-description, openapi-tags
  - Schema rules: typed-enum, schema-description
  - Component rules: no-unused-components
  - Configurable severity per rule via `LintingRules` attribute property
  - Format: `"rule1:off,rule2:error,rule3:warn"`
  - Diagnostics: SKUGGA_LINT_001 through SKUGGA_LINT_017
  - AOT-compatible, zero runtime overhead
  - 132 tests passing (5 skipped requiring generator harness)
- **Incremental Generation Cache** - Enhancement #11  
  - Automatic caching via IIncrementalGenerator (implemented in Enhancement #9)
  - Cache invalidation on spec/interface changes
  - < 1ms for cache hits, ~50-200ms for cache misses
  - 70% memory reduction on first builds (150MB vs 500MB)
  - 90% memory reduction on incremental builds (50MB vs 500MB)
  - See [INCREMENTAL_PERFORMANCE.md](docs/INCREMENTAL_PERFORMANCE.md) for details
- **Parallel Generation** - Enhancement #12
  - Automatic parallel processing via IIncrementalGenerator (implemented in Enhancement #9)
  - Multiple interfaces process concurrently
  - Linear scaling with CPU cores (2x speedup with 2 cores, 4x with 4 cores)
  - Thread-safe by design
  - See [INCREMENTAL_PERFORMANCE.md](docs/INCREMENTAL_PERFORMANCE.md) for benchmarks

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

### Changed
- Reorganized all project files into enterprise-grade structure with proper subdirectories
- Updated CI workflows to use latest GitHub Actions (checkout v6, setup-dotnet v5)
- Improved CI test discovery with `dotnet test Skugga.slnx` to ensure all 937 tests run

### Fixed
- System.Collections.Immutable version conflicts between .NET 8 and .NET 9 generators
- NuGet package now correctly includes both Skugga.Generator.dll and Skugga.Core.Generators.dll
- CI workflow paths after repository reorganization

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
