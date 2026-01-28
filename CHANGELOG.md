# Changelog

All notable changes to Skugga will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.4.0] - 2026-01-28

### Added
- **100% Moq Feature Parity Achieved** - Skugga now supports all major Moq features while maintaining Native AOT compatibility
- **LINQ to Mocks (`Mock.Of<T>`)** - Functional-style mock initialization using expression trees
  - One-line property configuration with predicates
  - Supports equality expressions (`x.Id == 1`)
  - Supports boolean properties (`x.IsActive`)
  - Supports AND conditions (`x.A && x.B`)
  - Compile-time interception via source generator
- **Ref/Out Parameter Support** - Full support for methods with `ref` and `out` parameters
  - `OutValue(index, value)` for configuring out parameter values
  - `RefValue(index, value)` for configuring ref parameter values
  - `OutValueFunc(index, func)` for dynamic out values
  - `RefValueFunc(index, func)` for dynamic ref values
  - `CallbackRefOut(delegate)` for complex ref/out scenarios
  - Works with TryParse patterns, swap operations, and multi-value returns
- **MockRepository** - Centralized mock management and verification
  - `Create<T>()` for creating mocks attached to repository
  - `Register(mock)` for adding existing mocks
  - `VerifyAll()` for batch verification
  - `VerifyNoOtherCalls()` for ensuring no unexpected interactions
  - Shared `MockBehavior` configuration across mocks
- **Protected Member Mocking** - Mock protected methods and properties on abstract classes
  - `Protected().Setup<T>(methodName, args)` for protected methods
  - `Protected().SetupGet<T>(propertyName)` for protected property getters
  - `Protected().SetupSet<T>(propertyName)` for protected property setters
  - Supports base class member overriding
- **Recursive Mocks** - Automatic mocking of nested properties
  - `DefaultValue.Mock` strategy for auto-mocking complex types
  - Infinite depth support for property chains
  - Lazy initialization of nested mocks
- **Additional Argument Matchers**
  - `It.IsIn<T>(params T[])` - Match values in a set
  - `It.IsNotNull<T>()` - Match any non-null value
  - `It.IsRegex(pattern)` - Match strings against regex patterns
- **Advanced Setup Features**
  - `SetupSet(x => x.Property = value)` - Configure property setter behavior
  - `VerifySet(x => x.Property = value, times)` - Verify property assignments
  - `Throws<TException>()` for regular (non-async) setups
  - `CallBase` property for partial mocks
- **Sample Projects**
  - Added `samples/AdvancedFeatures` demonstrating LINQ to Mocks, Ref/Out, MockRepository, and Protected mocking
  - Comprehensive README with usage examples

### Changed
- **Documentation Updates**
  - Updated `README.md` with accurate feature comparison table
  - Added LINQ to Mocks and Ref/Out parameter examples
  - Updated `docs/API_REFERENCE.md` with complete documentation for all new features
  - Added Protected Members section to API reference
- **Code Quality**
  - Fixed CA2211 suppression for `It.Ref<T>.IsAny` (must be mutable for ref/out syntax)
  - Added proper justification comments for analyzer suppressions

### Fixed
- **Major Test Stability Pass**: Resolved 450+ test failures from recent refactoring
- **Complex Expression Evaluation**: 
  - Added support for ternary operators (`ConditionalExpression`) in setup arguments
  - Added support for array indexing and complex indexers in setup arguments
  - Fixed nullability evaluation errors in complex property chains
- **Event Generation and Verification**:
  - Fixed `MockGenerator` to emit explicit `add`/`remove` event accessors instead of fields
  - Corrected `VerifyAdd` and `VerifyRemove` to use `ArgumentMatcher<Delegate>` for flexible subscription verification
  - Removed redundant invocation recording preventing double-counting in verification
- **Recursive Mocking (DefaultValue.Mock)**:
  - Fixed property persistence to ensure nested mocks are cached and returned consistently
  - Resolved `NullReferenceException` in nested property chains
- **Async Setup Stability**:
  - Implemented automatic completed `Task` and `Task<T>` returns for un-setup methods in Loose mode
- **Exception Alignment**:
  - Standardized all verification failures to throw `MockException` for xUnit compatibility
- **Precedence Logic**:
  - Re-aligned setup evaluation to prioritize the first matching setup added (Moq-compatible)
- Corrected `README.md` claim that `Mock.Of<T>` was unsupported due to AOT limitations
- Updated feature comparison table to reflect MockRepository support

## [1.3.1] - 2026-01-07

### Fixed
- **Nuget Packaging Conflict**:
  - Removed `Skugga.Core.Generators.dll` from the nuget package to prevent duplicate analyzer errors (`CS0111`) for consumers.
- **Reference Management**:
  - `Skugga.Core` now correctly manages its own internal generator references without exposing them to consuming projects.

## [1.3.0] - 2026-01-07

### Added
- **Project Governance & Support**:
  - Added `CODE_OF_CONDUCT.md` (Contributor Covenant v2.1)
  - Added `SUPPORT.md` with clear support channels
  - Added `.editorconfig` enforcing high-quality coding standards (aligned with Sannr)
  - Added `docs/AOT_COMPATIBILITY_ANALYSIS.md` deeply analyzing AOT constraints and solutions
- **CI/CD & Security Enhancements**:
  - Integrated **GitHub CodeQL** for automated security and quality analysis
  - Integrated **CycloneDX SBOM** generation into release pipeline for supply chain security
  - Added `IsAotCompatible` metadata to NuGet packages for SDK-level AOT verification
  - Added automated **Source Formatting Validation** in CI pipeline
  - Added **Code Coverage Artifacts** upload in CI for better visibility

### Changed
- **Build & Quality Standards**:
  - Enabled rigorous code analysis (`AnalysisLevel=latest`, `EnforceCodeStyleInBuild=true`)
  - Suppressed legacy technical debt warnings to allow incremental improvements
  - Applied consistent formatting across the entire codebase
  - Updated `GitVersion.yml` for v1.3.0 release
- **Samples & Demos**:
  - Standardized all sample projects to use centralized package versions
  - Verified runtime execution for all 7 major demo categories

### Fixed
- Resolved multiple `CA1310` (StringComparison) violations for better globalization support
- Aligned repository structure with Sannr enterprise standards
- Fixed build failures in test projects related to naming conventions and localization

## [1.2.0] - 2026-01-05

### Added
- **Comprehensive Documentation Structure**
  - Created docs hub (`docs/README.md`) with complete table of contents
  - Added detailed feature guides:
    - `docs/DOPPELGANGER.md` - OpenAPI mock generation (21K)
    - `docs/AUTOSCRIBE.md` - Self-writing test code (11K)
    - `docs/CHAOS_ENGINEERING.md` - Resilience testing (13K)
    - `docs/ALLOCATION_TESTING.md` - Performance enforcement (15K)
  - Enhanced root README with improved navigation and demo links
  - Total: 60KB of new documentation across 4 feature guides

- **Doppelgänger Demo Project** (`samples/DoppelgangerDemo/`)
  - 3 working test scenarios demonstrating contract drift detection
  - Comparison table showing Manual Mocks vs Doppelgänger
  - ROI calculation ($23k-33k annual savings)
  - Competitive analysis (vs OpenAPI Generator, NSwag, Moq)
  - Example OpenAPI specs (v1 and v2 with breaking changes)

- **Enhanced Demo Projects**
  - Added comprehensive READMEs for all sample projects
  - `samples/AutoScribeDemo/` - 9-dependency controller example
  - `samples/ChaosEngineeringDemo/` - 4 resilience testing scenarios
  - `samples/AllocationTestingDemo/` - 6 before/after optimization examples

- **Spectral-Inspired OpenAPI Linting**
  - 16 linting rules for OpenAPI quality enforcement at build time
  - Configurable severity per rule via `LintingRules` attribute property
  - Format: `"rule1:off,rule2:error,rule3:warn"`
  - Diagnostics: SKUGGA_LINT_001 through SKUGGA_LINT_017
  - AOT-compatible, zero runtime overhead

- **Incremental Generation Cache**
  - Automatic caching via IIncrementalGenerator
  - < 1ms for cache hits, ~50-200ms for cache misses
  - 70% memory reduction on first builds (150MB vs 500MB)
  - 90% memory reduction on incremental builds (50MB vs 500MB)

- **Parallel Generation**
  - Automatic parallel processing via IIncrementalGenerator
  - Linear scaling with CPU cores (2x speedup with 2 cores, 4x with 4 cores)
  - Thread-safe by design

### Changed
- Updated all documentation links from "See Live Demo" to "Demo and Example Code"
- Updated navigation from "Live Demos" to "Example Code"
- Updated footer to welcome contributions
- Enhanced cross-linking between documentation and sample projects

### Infrastructure
- Added draft documentation files to .gitignore
  - `docs/DOPPELGANGER_ROADMAP.md`
  - `docs/V1.2.0_SUMMARY.md`
  - `docs/INCREMENTAL_PERFORMANCE.md`

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

[Unreleased]: https://github.com/Digvijay/Skugga/compare/v1.3.1...HEAD
[1.3.1]: https://github.com/Digvijay/Skugga/compare/v1.3.0...v1.3.1
[1.3.0]: https://github.com/Digvijay/Skugga/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/Digvijay/Skugga/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/Digvijay/Skugga/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/Digvijay/Skugga/releases/tag/v1.0.0
