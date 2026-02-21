# Skugga Documentation

> **Complete guides for Native AOT-compatible mocking at compile time**

##  Table of Contents

### Core Features

#### [Doppelgänger - OpenAPI Mock Generation](DOPPELGANGER.md)
> *"Your tests should fail when APIs change, not your production."*

Auto-generate test mocks from OpenAPI/Swagger specs with build-time contract validation. The only tool that prevents contract drift by failing your build when APIs change.

**Key Benefits:**
- Prevents production incidents from API contract changes
- Saves $23k-33k per year per team
- Zero manual interface coding
- Built-in OAuth/JWT mocking

**[-> Read the Doppelgänger Guide](DOPPELGANGER.md)** | **[-> Demo and Example Code](../samples/DoppelgangerDemo)**

---

#### [AutoScribe - Self-Writing Test Code](AUTOSCRIBE.md)
> *"Stop writing mock setup code. Let AutoScribe record it for you."*

Record real interactions and auto-generate mock setup code. Turn 15 minutes of tedious mock configuration into 30 seconds.

**Key Benefits:**
- Records method calls and return values automatically
- Generates copy/paste ready `mock.Setup()` code
- Captures real data, not guesses
- Works with complex objects and async methods

**[-> Read the AutoScribe Guide](AUTOSCRIBE.md)** | **[-> Demo and Example Code](../samples/AutoScribeDemo)**

---

#### [Chaos Engineering - Resilience Testing](CHAOS_ENGINEERING.md)
> *"Prove your retry logic works before production breaks."*

**Industry First:** The only .NET mocking library with built-in chaos engineering. Inject faults (exceptions, latency, timeouts) directly into mocks to test resilience patterns.

**Key Benefits:**
- Test retry policies with reproducible chaos
- Validate circuit breakers and fallback logic
- Configurable failure rates and delays
- Detailed chaos statistics tracking

**[-> Read the Chaos Engineering Guide](CHAOS_ENGINEERING.md)** | **[-> Demo and Example Code](../samples/ChaosEngineeringDemo)**

---

#### [Zero-Allocation Testing - Performance Enforcement](ALLOCATION_TESTING.md)
> *"Catch memory regressions before they hit production."*

**Industry First:** The only .NET mocking library providing allocation assertions. Enforce zero-allocation requirements for hot paths with GC-level precision.

**Key Benefits:**
- Prove hot paths are truly zero-allocation
- Set allocation budgets for controlled memory use
- Measure and compare allocation patterns
- Catch performance regressions in CI/CD

**[-> Read the Allocation Testing Guide](ALLOCATION_TESTING.md)** | **[-> Demo and Example Code](../samples/AllocationTestingDemo)**

---

### Reference Documentation

#### [API Reference](API_REFERENCE.md)
Complete API documentation covering all Skugga features, attributes, and extension methods.

#### [Technical Summary](TECHNICAL_SUMMARY.md)
Deep dive into Skugga's architecture, source generation approach, and AOT compatibility.

#### [Executive Summary](EXECUTIVE_SUMMARY.md)
Business case for adopting Skugga: ROI, risk mitigation, and competitive advantages.

#### [Troubleshooting](TROUBLESHOOTING.md)
Common issues and solutions for Skugga users.

#### [Dependencies](DEPENDENCIES.md)
Third-party packages used by Skugga and their purposes.

---

##  Quick Start

New to Skugga? Start here:

1. **[Installation](../README.md#installation)** - Get Skugga via NuGet
2. **[Basic Mocking](API_REFERENCE.md#basic-mocking)** - Create your first mock
3. **[Doppelgänger Tutorial](DOPPELGANGER.md#quick-start)** - Generate mocks from OpenAPI
4. **[Sample Projects](../samples/)** - Working examples for all features

---

##  Feature Comparison

| Feature | Skugga | Moq | NSubstitute | FakeItEasy |
|---------|--------|-----|-------------|------------|
| Native AOT Compatible |  100% |  No |  No |  No |
| OpenAPI Mock Generation |  Yes |  No |  No |  No |
| Self-Writing Tests (AutoScribe) |  Yes |  No |  No |  No |
| Chaos Engineering |  Yes |  No |  No |  No |
| Zero-Allocation Testing |  Yes |  No |  No |  No |
| Compile-Time Generation |  Yes |  No |  No |  No |
| Reflection-Free |  Yes |  No |  No |  No |

---

##  Contributing

Found a bug or want to contribute? See our [Contributing Guide](../CONTRIBUTING.md).

---

##  License

Skugga is licensed under the [MIT License](../LICENSE).

---

**Built with  by [Digvijay](https://github.com/Digvijay) | Contributions welcome!**

*Mocking at the Speed of Compilation.*
