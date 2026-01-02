# Contributing to Skugga

Thank you for your interest in contributing to Skugga! This document provides guidelines and information for contributors.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [How to Contribute](#how-to-contribute)
- [Coding Standards](#coding-standards)
- [Testing Guidelines](#testing-guidelines)
- [Pull Request Process](#pull-request-process)
- [Community](#community)

## Code of Conduct

This project adheres to the Microsoft Open Source Code of Conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/Skugga.git
   cd Skugga
   ```
3. **Add upstream remote**:
   ```bash
   git remote add upstream https://github.com/Digvijay/Skugga.git
   ```

## Development Setup

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (for latest features)
- Git
- Your favorite IDE:
  - [Visual Studio 2022](https://visualstudio.microsoft.com/) (17.8+)
  - [Visual Studio Code](https://code.visualstudio.com/) with C# extension
  - [JetBrains Rider](https://www.jetbrains.com/rider/)

### Building the Project

```bash
# Restore dependencies
dotnet restore Skugga.slnx

# Build in Release mode
dotnet build Skugga.slnx --configuration Release

# Run tests
dotnet test Skugga.slnx --configuration Release
```

### Running Benchmarks

```bash
dotnet run --project Skugga.Benchmarks/Skugga.Benchmarks.csproj --configuration Release
```

## How to Contribute

### Reporting Issues

- **Search existing issues** before creating a new one
- Use the issue template and provide:
  - Clear description of the problem
  - Steps to reproduce
  - Expected vs actual behavior
  - Environment details (.NET version, OS)
  - Code samples (if applicable)

### Suggesting Features

- Open an issue with the `enhancement` label
- Describe the problem you're trying to solve
- Provide examples of how the feature would be used
- Consider Native AOT compatibility implications

### Contributing Code

1. **Create a feature branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make your changes** following our [Coding Standards](#coding-standards)

3. **Write tests** for new functionality

4. **Run all tests** to ensure nothing breaks:
   ```bash
   dotnet test --configuration Release
   ```

5. **Commit your changes** using conventional commits:
   ```bash
   git commit -m "feat: add support for async methods"
   ```

6. **Push to your fork**:
   ```bash
   git push origin feature/your-feature-name
   ```

7. **Create a Pull Request** from your fork to the main repository

## Coding Standards

Skugga follows Microsoft's C# Coding Conventions and .NET library guidelines:

### General Principles

- **Performance-first**: Remember this is a performance-critical library
- **AOT-compatible**: All code must work with Native AOT
- **Zero-allocation**: Avoid allocations in hot paths
- **Clear intent**: Code should be self-documenting

### Code Style

```csharp
// ✅ Good
public sealed class MockHandler
{
    private readonly List<MockSetup> _setups = new();
    
    public MockBehavior Behavior { get; set; } = MockBehavior.Loose;
    
    public void AddSetup(string signature, object?[] args, object? value)
    {
        _setups.Add(new MockSetup(signature, args, value));
    }
}

// ❌ Bad
public class MockHandler  // Not sealed
{
    private List<MockSetup> setups;  // Not readonly
    public MockBehavior Behavior;     // Property should be used
}
```

### Key Guidelines

- **Use `sealed` classes** when inheritance isn't needed
- **Mark fields `readonly`** when they don't change after construction
- **Use expression-bodied members** for simple properties/methods
- **Prefer records** for immutable data types
- **Use nullable reference types** (`#nullable enable`)
- **Avoid LINQ in hot paths** (use `for` loops instead)
- **Use `Span<T>` and `stackalloc`** where appropriate

### File Organization

```
Skugga.Core/
├── Mock.cs              # Public API surface
├── MockExtensions.cs    # Extension methods
├── MockHandler.cs       # Core logic
└── Attributes.cs        # Custom attributes (if needed)
```

## Testing Guidelines

### Test Structure

Follow the AAA (Arrange-Act-Assert) pattern:

```csharp
[Fact]
public void Setup_WithReturnValue_ShouldReturnConfiguredValue()
{
    // Arrange
    var mock = Mock.Create<ITestService>();
    mock.Setup(x => x.GetData(1)).Returns("test-data");

    // Act
    var result = mock.GetData(1);

    // Assert
    result.Should().Be("test-data");
}
```

### Test Naming Convention

```
MethodName_Scenario_ExpectedBehavior

Examples:
- Create_WithLooseBehavior_ShouldReturnMockInstance
- Invoke_WithoutSetup_InStrictMode_ShouldThrowException
- Chaos_WithZeroFailureRate_ShouldNeverThrow
```

### Test Requirements

- **Unit tests** for all public API methods
- **Edge cases**: null, empty, boundary conditions
- **Error cases**: invalid inputs, strict mode violations
- **Performance tests** for critical paths (optional but encouraged)

### Using FluentAssertions

```csharp
// ✅ Preferred
result.Should().Be("expected");
action.Should().Throw<InvalidOperationException>()
    .WithMessage("*specific*");

// ❌ Avoid
Assert.Equal("expected", result);
Assert.Throws<InvalidOperationException>(() => action());
```

## Pull Request Process

### Before Submitting

- [ ] Code builds successfully (`dotnet build`)
- [ ] All tests pass (`dotnet test`)
- [ ] New tests added for new functionality
- [ ] Code follows style guidelines
- [ ] Commits follow conventional commit format
- [ ] Documentation updated (if needed)

### PR Title Format

Use [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `perf:` Performance improvements
- `refactor:` Code refactoring
- `test:` Adding/updating tests
- `chore:` Maintenance tasks

### Examples

```
feat: add support for async method mocking
fix: resolve property setup ambiguity
docs: improve README installation section
perf: optimize MockHandler.Invoke using Span<T>
```

### Review Process

1. **Automated checks** must pass (CI, tests, linting)
2. **Code review** by at least one maintainer
3. **Discussion** of design decisions (if needed)
4. **Approval** from maintainer
5. **Squash and merge** (maintainer will handle this)

### What We Look For

- ✅ **Clear problem statement**: What does this solve?
- ✅ **Minimal changes**: Focused, single-purpose PRs
- ✅ **Test coverage**: New code is tested
- ✅ **AOT compatibility**: No reflection, no dynamic code
- ✅ **Performance**: No regressions in benchmarks
- ✅ **Documentation**: Public APIs are documented

## Community

### Getting Help

- 💬 [GitHub Discussions](https://github.com/Digvijay/Skugga/discussions) - Ask questions
- 🐛 [Issues](https://github.com/Digvijay/Skugga/issues) - Report bugs
- 📧 Email maintainers for security issues

### Stay Updated

- ⭐ **Star the repository** to show support
- 👀 **Watch** for notifications on new releases
- 🔀 **Fork** to experiment with your own changes

## Recognition

Contributors will be:
- Listed in release notes
- Acknowledged in the README
- Eligible for maintainer status (for sustained contributions)

---

## Additional Resources

- [.NET Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Native AOT Deployment](https://docs.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Source Generators](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- [Unit Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

---

**Thank you for contributing to Skugga!** 🎉
