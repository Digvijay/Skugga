# Changelog

All notable changes to Skugga are documented here. The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [1.4.0] -- 2026-01-28

### Added
- **100% Moq Feature Parity** -- All major Moq features while maintaining Native AOT compatibility
- **LINQ to Mocks** (`Mock.Of<T>`) -- Functional-style mock initialization
- **Ref/Out Parameter Support** -- `OutValue`, `RefValue`, `CallbackRefOut`
- **MockRepository** -- Centralized mock management and batch verification
- **Protected Member Mocking** -- Mock `protected abstract` methods and properties
- **Recursive Mocks** -- `DefaultValue.Mock` for auto-mocking nested properties
- **Additional Matchers** -- `It.IsIn`, `It.IsNotNull`, `It.IsRegex`
- **SetupSet / VerifySet** -- Property setter behavior and verification
- **Throws\<TException>()** -- Typed exception throwing

### Fixed
- 450+ test failures from refactoring
- Complex expression evaluation (ternary, indexers)
- Event generation and verification
- Async setup stability

## [1.3.1] -- 2026-01-07

### Fixed
- NuGet packaging conflict with `Skugga.Core.Generators.dll`

## [1.3.0] -- 2026-01-07

### Added
- GitHub CodeQL security analysis
- CycloneDX SBOM generation
- `.editorconfig` enforcement
- AOT compatibility analysis documentation

## [1.2.0] -- 2026-01-05

### Added
- 60KB+ documentation (4 feature guides)
- Spectral-inspired OpenAPI linting (16 rules)
- Incremental generation cache
- Doppelgänger, AutoScribe, Chaos, and Allocation Testing demos

## [1.1.0] -- 2026-01-03

### Added
- Enterprise-grade project structure
- Dual generator NuGet packaging
- CI/CD with latest GitHub Actions
- 937 tests passing

## [1.0.0] -- Initial Release

### Added
- Core mocking with `Mock.Create<T>()`
- Strict and Loose behaviors
- Chaos mode, AutoScribe, Zero-allocation assertions
- Native AOT compatibility
- C# 12 interceptors
