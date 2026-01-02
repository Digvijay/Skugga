# Package Dependencies & Versions

This document outlines all NuGet package dependencies used in the Skugga project, their current versions, and verification that they are using the latest compatible versions.

## 📦 Core Dependencies

### Skugga.Core (src/Skugga.Core/Skugga.Core.csproj)

| Package | Version | Status | Notes |
|---------|---------|--------|-------|
| `Microsoft.SourceLink.GitHub` | `8.0.0` | ✅ Latest | Source linking for debugging |

**Target Frameworks:** `net8.0`, `net10.0`, `netstandard2.0`

### Skugga.Generator (src/Skugga.Generator/Skugga.Generator.csproj)

| Package | Version | Status | Notes |
|---------|---------|--------|-------|
| `Microsoft.CodeAnalysis.CSharp` | `5.0.0` | ✅ Latest Stable | Roslyn compiler APIs |

**Target Frameworks:** `netstandard2.0`

## 🧪 Testing & Benchmarking

### Core Tests (src/Skugga.Core.Tests/Skugga.Core.Tests.csproj)

| Package | Version | Status | Notes |
|---------|---------|--------|-------|
| `Microsoft.NET.Test.Sdk` | `17.12.0` | ✅ Latest | Test platform infrastructure |
| `xunit` | `2.9.3` | ✅ Latest | Testing framework |
| `xunit.runner.visualstudio` | `3.0.0` | ✅ Latest | Visual Studio test integration |
| `FluentAssertions` | `7.0.0` | ✅ Latest | Fluent assertion library |
| `coverlet.collector` | `6.0.2` | ✅ Latest | Code coverage collection |

**Target Frameworks:** `net10.0`

### Generator Tests (src/Skugga.Generator.Tests/Skugga.Generator.Tests.csproj)

| Package | Version | Status | Notes |
|---------|---------|--------|-------|
| `Microsoft.NET.Test.Sdk` | `17.12.0` | ✅ Latest | Test platform infrastructure |
| `Microsoft.CodeAnalysis.CSharp` | `5.0.0` | ✅ Latest Stable | Roslyn APIs for testing |
| `xunit` | `2.9.3` | ✅ Latest | Testing framework |
| `xunit.runner.visualstudio` | `3.0.0` | ✅ Latest | Visual Studio test integration |
| `FluentAssertions` | `7.0.0` | ✅ Latest | Fluent assertion library |

**Target Frameworks:** `net10.0`

### Benchmarks (src/Skugga.Benchmarks/Skugga.Benchmarks.csproj)

| Package | Version | Status | Notes |
|---------|---------|--------|-------|
| `BenchmarkDotNet` | `0.15.8` | ✅ Latest | Performance benchmarking framework |
| `Moq` | `4.20.73` | ✅ Latest | Competitor comparison |
| `NSubstitute` | `5.3.0` | ✅ Latest | Competitor comparison |

**Target Frameworks:** `net9.0`, `net10.0`

## 🔄 Version Update Process

Package versions are regularly verified against NuGet.org to ensure we're using the latest compatible versions:

1. **Automated Checks**: Dependabot monitors for updates
2. **Manual Verification**: Quarterly review using NuGet API queries
3. **Compatibility Testing**: All updates tested across all target frameworks
4. **Documentation Updates**: This document updated when versions change

## 🏗️ Build Requirements

- **.NET SDK**: 8.0, 9.0, or 10.0
- **Target Frameworks**:
  - `net8.0`, `net10.0` (main library)
  - `netstandard2.0` (generator for broader compatibility)
- **OS Support**: Windows, macOS, Linux
- **Architecture**: x64, ARM64

## 📋 Dependency Management

### Centralized Version Management

We use centralized package version management to ensure consistency across all projects. All package versions are defined in the solution file.

### NuGet Package Source

```xml
<packageSources>
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
</packageSources>
```

## 🔍 Verification Commands

To verify package versions are up to date:

```bash
# List all packages and their versions
dotnet list package

# Check for outdated packages
dotnet list package --outdated

# Check for deprecated packages
dotnet list package --deprecated

# Check for vulnerable packages
dotnet list package --vulnerable
```

## 📈 Version History

### January 2026
- Updated to BenchmarkDotNet 0.15.8
- Updated to xunit 2.9.3
- Updated to FluentAssertions 7.0.0
- Updated to Microsoft.CodeAnalysis.CSharp 5.0.0

### December 2025
- Initial dependency documentation
- All packages at latest stable versions

---

**Last Updated:** January 1, 2026  
**Next Review:** April 2026
