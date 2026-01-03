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
