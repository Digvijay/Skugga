# Skugga Development Roadmap

**Last Updated:** January 2, 2026  
**Overall Progress:** 98% Moq Feature Parity | 371 Tests Passing  
**Current Focus:** Phase 14 - Enhanced Diagnostics

> **Note:** This file is for local development planning only and is NOT committed to git (see .gitignore line 5).

---

## 📊 Current Status

### Test Results (371 Passing)
```
Skugga.Core.Tests:          161 passing
Skugga.Generator.Tests:      10 passing
Skugga.AutoScribe.Tests:     18 passing
Skugga.Chaos.Tests:           9 passing
Skugga.Async.Tests:           7 passing
Skugga.SetupSequence.Tests:   9 passing
Skugga.Matchers.Tests:       20 passing
ProtectedMembers.Tests:      10 passing
Event.Tests:                 12 passing
OutRef.Tests:                20 passing
SetupProperty.Tests:          8 passing
Sequence.Tests:               9 passing
Additional tests:            78 passing
```

### Performance Metrics (vs Moq)
- **Speed:** 6.68x faster overall
- **Memory:** 4.1x less allocation  
- **Cold Start:** Zero reflection overhead
- **Build Impact:** <1 second for typical projects

### Feature Completion by Category
- ✅ **Core Mocking:** 100% (Mock.Create, Setup, Returns, Callback, Verify)
- ✅ **Argument Matching:** 100% (It.IsAny, It.Is, It.IsIn, It.IsNotIn, It.IsNull, It.IsNotNull, It.IsRegex)
- ✅ **Verification:** 100% (Times.Never/Once/Exactly/AtLeast/AtMost/Between)
- ✅ **Async Support:** 100% (ReturnsAsync, Task defaults)
- ✅ **SetupSequence:** 100% (Sequential returns/throws)
- ✅ **Special Features:** 100% (AutoScribe, Chaos Mode, Zero-Alloc Guard)
- ✅ **Properties:** 100% (SetupProperty with backing fields)
- ✅ **Protected Members:** 100% (Protected().Setup<T>("MethodName"))
- ✅ **Events:** 100% (Raise() and Raises() with full support)
- ✅ **Out/Ref Parameters:** 100% (OutValue(), Ref.IsAny)
- ✅ **MockSequence:** 100% (InSequence() for ordered verification)
- ✅ **Multiple Interfaces:** 100% (As<T>() for interface composition)
- ✅ **Partial Mocks:** 100% (Override specific methods via interceptors)
- ❌ **Mock.Of<T>(expr):** Not supported (AOT limitation - use Mock.Create + Setup)

---

## 🎯 Phase 14: Enhanced Diagnostics & Error Messages (NEXT PRIORITY)

**Status:** NOT STARTED  
**Estimated Time:** 1 week  
**Priority:** High (improves developer experience)

### Objectives
Provide industry-leading compile-time diagnostics and runtime error messages to guide developers toward correct usage.

### Planned Diagnostic Codes

#### SKUGGA003: Variable in Setup Expression
- **Level:** Warning
- **Message:** "Cannot use variable in Setup expression. Use It.Is<T>(x => x == {variable}) instead"
- **Code Action:** Convert to It.Is matcher
- **Example:**
  ```csharp
  // ❌ Current (throws NotSupportedException at runtime)
  string expected = "test";
  mock.Setup(x => x.Method(expected));  
  
  // ✅ Suggested fix
  mock.Setup(x => x.Method(It.Is<string>(s => s == expected)));
  ```

#### SKUGGA004: Generic Type Parameter Issue
- **Level:** Error
- **Message:** "The type or namespace name 'TState' could not be found in generic method. This is a known generator limitation."
- **Code Action:** Suggest alternative interface or manual implementation
- **Impact:** Blocks mocking common interfaces like `ILogger<T>`
- **Example:**
  ```csharp
  // ❌ Current (compile error)
  var loggerMock = Mock.Create<ILogger<MyClass>>();
  
  // ✅ Workaround
  // Use manual implementation or alternative logging abstraction
  ```

#### SKUGGA005: Argument Matcher Suggestion
- **Level:** Info
- **Message:** "Consider using It.Is<T>() for more flexible matching"
- **When:** User sets up with exact constant that could be a matcher
- **Example:**
  ```csharp
  // ℹ️ Works but could be more flexible
  mock.Setup(x => x.Method(42)).Returns("answer");
  
  // 💡 Suggestion
  mock.Setup(x => x.Method(It.Is<int>(n => n == 42))).Returns("answer");
  ```

### Runtime Error Improvements

#### Better Verification Mismatch Messages
**Current:**
```
Method not called with expected arguments
```

**Target:**
```
❌ Verification failed: IFoo.DoSomething(string)
Expected: "hello"
Actual calls:
  1. DoSomething("Hello")  // Note: case mismatch
  2. DoSomething(null)

💡 Suggestion: Did you mean It.IsAny<string>()?
   Or use It.Is<string>(s => s.Equals("hello", StringComparison.OrdinalIgnoreCase))?
```

#### "Did You Mean?" Suggestions
- Detect typos in method names (Levenshtein distance < 3)
- Suggest correct overload when arguments don't match
- Remind to Setup before Verify

### Generator Enhancement Tasks
- [ ] Implement SKUGGA003 analyzer with code fix (variable → It.Is conversion)
- [ ] Implement SKUGGA004 analyzer for generic type parameter detection
- [ ] Implement SKUGGA005 analyzer for matcher usage suggestions
- [ ] Enhance MockHandler to capture detailed call information for better error messages
- [ ] Add "Did you mean?" logic using fuzzy string matching
- [ ] Generate diagnostic documentation links (to GitHub wiki)
- [ ] Create comprehensive tests for each diagnostic (positive + negative cases)

**Deliverables:**
- 3 new diagnostic analyzers (SKUGGA003-005)
- Improved MockHandler with detailed error context
- Updated documentation with troubleshooting guide
- 15+ tests covering diagnostic scenarios

---

## 🚀 Phase 15: Skugga-Exclusive Features (Planned Enhancements)

**Status:** PARTIALLY COMPLETE (25%)  
**Estimated Time:** 4-6 weeks  
**Priority:** Medium (nice-to-have, community-driven)

### 15.1 Smart Suggestions (AI-Powered) 🔮
**Priority:** Low (requires ML/AI integration)

- [ ] **Analyze test patterns and suggest missing verifications**
  - Detect Setup calls without corresponding Verify
  - Warn about over-verification (verifying every call)
  
- [ ] **Detect common anti-patterns**
  - Over-mocking (too many dependencies)
  - Tight coupling (test knows too much about implementation)
  - "God mock" (one mock with 20+ setups)
  
- [ ] **Generate test templates based on production code**
  - Scan method signatures and suggest test structure
  - Auto-generate Setup/Verify patterns from method signature
  
- [ ] **Suggest better matcher alternatives**
  - Recommend It.Is instead of exact values when appropriate
  - Suggest It.IsRegex for string patterns (e.g., email validation)

**Technical Approach:**
- Roslyn analyzer to detect patterns
- Rule-based system (no ML initially)
- Optional: ML model trained on open-source test repositories

### 15.2 Advanced Chaos Strategies 🌪️
**Priority:** Medium (useful for microservices/distributed systems)

**Current Implementation (COMPLETE ✅):**
- Basic Chaos mode with failure rate configuration
- Exception injection support
- Integration with mock setups

**Planned Enhancements:**
- [ ] **Network latency simulation**
  - Configurable delays (min/max/average)
  - Jitter for realistic network conditions
  - Example: `chaos.SetLatency(min: 50ms, max: 500ms, jitter: 0.2)`

- [ ] **Timeout scenarios for distributed systems**
  - Simulate slow responses
  - Force timeout exceptions (OperationCanceledException)
  - Example: `chaos.SetTimeout(after: TimeSpan.FromSeconds(5))`

- [ ] **Chaos schedules**
  - Inject failures at specific times
  - Time-based chaos (fail after N seconds into test)
  - Example: `chaos.SetSchedule(failAfter: TimeSpan.FromSeconds(10))`

- [ ] **Chaos statistics and reporting**
  - Dashboard showing failure rates, latencies
  - Export chaos results to CSV/JSON for analysis
  - Integration with test reporting tools

- [ ] **Integration with Polly resilience policies**
  - Test retry logic with controlled chaos
  - Verify circuit breaker behavior under chaos
  - Validate timeout policies

**Technical Approach:**
- Extend existing ChaosMode class
- Add ChaosDashboard for statistics
- Polly integration via extension methods

### 15.3 Performance Profiling Integration 📊
**Priority:** Medium (useful for performance-critical applications)

**Current Implementation (COMPLETE ✅):**
- AssertAllocations.Zero() for allocation testing
- Integration with GC for heap validation

**Planned Enhancements:**
- [ ] **Detailed allocation reports per mock method**
  - Show exact allocation size and location
  - Stack traces for allocations (in Debug mode)
  - Example output: "Method() allocated 1.2 KB (48 bytes on stack, 1.15 KB on heap)"

- [ ] **CPU profiling hooks for hot paths**
  - Measure method execution time
  - Identify performance bottlenecks in tests
  - Warn when mock overhead exceeds threshold (e.g., >10% of test time)

- [ ] **Integration with BenchmarkDotNet for CI/CD**
  - Automatic benchmark runs in pipeline
  - Compare performance across commits
  - Fail build if performance regresses >10%

- [ ] **Performance regression detection**
  - Alert when tests get slower (CI/CD integration)
  - Automated performance gates (e.g., "no test >100ms")
  - Historical performance tracking

- [ ] **Visualization of mock overhead**
  - Charts showing setup vs execution time
  - Compare mock implementations (Skugga vs Moq)
  - Identify high-overhead mocks

**Technical Approach:**
- Extend AssertAllocations with profiling APIs
- BenchmarkDotNet integration via attributes
- Performance dashboard using Plotly or similar

### 15.4 Enhanced AutoScribe 📝
**Priority:** Medium (useful for learning test behavior)

**Current Implementation (COMPLETE ✅):**
- AutoScribe.Capture<T>() recording proxy
- Automatic test setup code generation
- Zero reflection, works with sync/async/void/generic methods
- 18 comprehensive tests

**Planned Enhancements:**
- [ ] **Capture method timing for performance analysis**
  - Record how long each method call took
  - Identify slow operations in recordings
  - Example output: "GetData() called 3 times, avg: 45ms, max: 120ms"

- [ ] **Export recordings to JSON/CSV formats**
  - Share recordings with team
  - Version control test data (golden master testing)
  - Example: `AutoScribe.Export("recording.json", format: ExportFormat.Json)`

- [ ] **Replay recordings for integration testing**
  - Use recorded data as mock responses
  - Deterministic tests from captured behavior
  - Example: `AutoScribe.Replay("recording.json")`

- [ ] **Diff tool for comparing test recordings**
  - Detect changes in behavior between versions
  - Regression testing: compare old vs new recordings
  - Example: `AutoScribe.Diff("old.json", "new.json")`

- [ ] **Integration with test explorers**
  - Visual Studio Test Explorer integration
  - JetBrains Rider integration
  - VS Code Test Explorer support

**Technical Approach:**
- Extend AutoScribe with export/import APIs
- Use JSON serialization for portability
- Diff tool using text-based comparison

---

## 🔧 Known Issues to Fix

### High Priority (Blocks Common Scenarios)

#### 1. Variable in Setup Expressions ⚠️
- **Issue:** Cannot use variables in Setup expressions → NotSupportedException
  ```csharp
  string expected = "test";
  mock.Setup(x => x.Method(expected)); // ❌ Throws at runtime
  ```
- **Workaround:** Use `It.Is<T>(x => x == variable)` or constants
  ```csharp
  mock.Setup(x => x.Method(It.Is<string>(s => s == expected))); // ✅ Works
  ```
- **Fix Required:** Support FieldExpression/VariableExpression in generator argument extraction
- **Impact:** Developer friction, requires workaround knowledge
- **Targeted in:** Phase 14 (SKUGGA003 diagnostic)

#### 2. Generic Type Parameters ⚠️
- **Issue:** ILogger<T> and generic interfaces with unbound type parameters fail
  ```csharp
  var logger = Mock.Create<ILogger<MyClass>>(); // ❌ Compile error
  // Error: "The type or namespace name 'TState' could not be found"
  ```
- **Root Cause:** Generator doesn't properly handle generic method type parameters
  ```csharp
  // ILogger<T> has:
  void Log<TState>(LogLevel level, EventId id, TState state, ...);
  // Generator fails to include <TState> in generated method signature
  ```
- **Workaround:** Use alternative logging abstraction or manual implementation
- **Fix Required:** Properly handle generic method type parameters in code generation
- **Impact:** Blocks mocking common BCL interfaces like ILogger
- **Targeted in:** Phase 14 (SKUGGA004 diagnostic + fix)

### Medium Priority (Workarounds Exist)

#### 3. Property Get/Set Tracking
- **Issue:** No distinction between property reads and writes
  ```csharp
  // Cannot separately verify reads vs writes
  mock.Verify(x => x.Property); // Verifies both get and set
  ```
- **Workaround:** Use methods instead of properties for complex tracking
- **Fix Required:** Generate separate interceptors for get/set accessors
- **Impact:** Limited property verification capabilities
- **Priority:** Medium (most users don't need this level of detail)

### Low Priority (Quality of Life)

#### 4. Setup Error Messages
- **Issue:** Could be more helpful when setup doesn't match call
  - **Current:** "Method not setup"
  - **Target:** "Method(42) was called but no setup exists for this argument. Did you mean Method(It.IsAny<int>())?"
- **Fix Required:** Enhanced error context in MockHandler
- **Impact:** Debugging friction
- **Targeted in:** Phase 14 (enhanced error messages)

#### 5. More Compile-Time Diagnostics
- **Issue:** Could warn about more common mistakes at compile-time
  - Mocking sealed classes (currently SKUGGA001 ✅)
  - Mocking classes without virtual members (currently SKUGGA002 ✅)
  - Using variables in Setup (planned SKUGGA003)
  - Generic type parameter issues (planned SKUGGA004)
- **Fix Required:** Additional Roslyn analyzers with code fixes
- **Impact:** Quality of life improvement
- **Targeted in:** Phase 14 (SKUGGA003-005)

---

## 📝 Deferred Features (Not Critical for v1.0)

These features are intentionally deferred based on complexity, demand, and architectural compatibility.

### LINQ to Mocks (Mock.Of<T>)
- **Feature:** `Mock.Of<T>(x => x.Prop == value)` for inline mock creation with setup
  ```csharp
  var foo = Mock.Of<IFoo>(x => x.Name == "test" && x.Age == 42);
  ```
- **Status:** SKIP (architecturally incompatible)
- **Reason:** Requires runtime proxy generation, violates zero-reflection principle
- **Workaround:** Use standard Mock.Create<T>() and Setup
- **Complexity:** Very High (fundamental architecture change)

---

## 🎯 Performance Goals

### Current Performance (v1.1.0 - January 2026)
Benchmarked on Intel Core i7-4980HQ @ 2.80GHz, 16GB RAM, macOS 15.7, .NET 10.0.1

#### vs Moq
- **Overall Speed:** 6.68x faster (4.24 μs vs 28.33 μs)
- **Memory:** 4.1x less allocation (1.12 KB vs 4.57 KB)
- **Void Method Setup:** 67.62x faster
- **Callback Execution:** 69.84x faster
- **Argument Matching:** 34.98-79.84x faster (varies by scenario)

#### vs NSubstitute
- **Speed:** 4.34x faster (4.24 μs vs 18.42 μs)
- **Memory:** 6.94x less allocation

#### vs FakeItEasy
- **Speed:** 3.09x faster (4.24 μs vs 13.10 μs)
- **Memory:** Similar allocation profile

#### Build Performance
- **Generator overhead:** <1 second for typical projects
- **Cold start:** Zero reflection overhead (AOT-friendly)
- **Incremental builds:** Minimal impact (only when mocks change)

### v2.0 Performance Targets (Q4 2026)
- **Speed:** 10x faster than Moq (current: 6.68x, target: 50% improvement)
- **Memory:** 5x less allocation (current: 4.1x, target: 22% improvement)
- **Build Impact:** <500ms for 100 mocks (currently ~1s)
- **Generator:** Parallel processing for large solutions (>50 mocks)

### Optimization Strategies
- ✅ Cache compilation data between builds (DONE)
- ✅ Optimize hash generation with FNV-1a algorithm (DONE)
- ✅ Minimize string allocations in generator (DONE)
- [ ] Parallel mock generation for multiple interfaces
- [ ] Incremental source generation (Roslyn v2)
- [ ] Reduce generated code size (remove redundant null checks)

---

## 📚 Documentation Improvements

### Completed ✅
- ✅ README with getting started, examples, benchmarks
- ✅ API_REFERENCE.md comprehensive guide (300+ lines)
- ✅ CONTRIBUTING.md for contributors
- ✅ CHANGELOG.md following Keep a Changelog format
- ✅ Benchmark documentation (benchmarks/MoqVsSkugga.md, benchmarks/FourFramework.md)
- ✅ Migration guide from Moq to Skugga (in API_REFERENCE.md)
- ✅ Troubleshooting guide in README
- ✅ Performance tuning guide (in benchmarks/README.md)

### Planned 📝

#### Video Tutorials
- [ ] **Quick start (5 min):** Create first mock, setup, verify
  - Install NuGet package
  - Create mock with Mock.Create<T>()
  - Setup with Setup() and Returns()
  - Verify with Verify() and Times

- [ ] **Deep dive into interceptors (15 min):** How Skugga works internally
  - Source generators vs reflection
  - Interceptors and compile-time code generation
  - Why Skugga is AOT-compatible

- [ ] **Migration walkthrough (10 min):** Converting Moq tests to Skugga
  - Replace Moq NuGet with Skugga
  - Update Mock<T> → Mock.Create<T>()
  - Update It.IsAny<T>() (compatible!)
  - Handle edge cases (variables in Setup)

- [ ] **AutoScribe demo (8 min):** Self-writing tests feature
  - What is AutoScribe and when to use it
  - Capture real interactions
  - Generate test code automatically

- [ ] **Chaos mode tutorial (7 min):** Resilience testing
  - Enable Chaos mode
  - Configure failure rate
  - Test retry logic and error handling

#### Sample Projects
- [ ] **Basic console app with unit tests**
  - Simple calculator with Skugga tests
  - Demonstrate Setup, Returns, Verify

- [ ] **ASP.NET Core Web API with Skugga tests**
  - Minimal API with dependency injection
  - Controller tests with repository mocks
  - Integration tests with WebApplicationFactory

- [ ] **Azure Functions example**
  - HTTP trigger with Skugga mocks
  - Dependency injection setup
  - Test logging and configuration

- [ ] **Native AOT deployment**
  - Trimmed, self-contained executable
  - Demonstrate zero reflection overhead
  - Performance comparison (AOT vs JIT)

- [ ] **Kubernetes deployment example**
  - Containerized app with health checks
  - Test resilience with Chaos mode
  - CI/CD pipeline with Skugga tests

- [ ] **Complex domain model with AutoScribe**
  - E-commerce domain (Order, Customer, Product)
  - Use AutoScribe to capture interactions
  - Generate comprehensive test suite

#### Advanced Patterns Cookbook
- [ ] **Testing retry logic with SetupSequence**
  - Simulate transient failures
  - Verify retry attempts
  - Example: HTTP client with retry policy

- [ ] **Mocking database repositories**
  - Generic repository pattern
  - Async queries with ReturnsAsync
  - Verify SaveChanges called

- [ ] **Testing event-driven architectures**
  - Message bus mocks
  - Event handlers with callbacks
  - Asynchronous event processing

- [ ] **Resilience testing with Chaos mode**
  - Test circuit breaker patterns
  - Verify fallback behavior
  - Chaos schedules for time-based failures

- [ ] **Performance testing with Zero-Alloc guard**
  - Identify allocations in hot paths
  - Optimize high-throughput scenarios
  - Benchmark with AssertAllocations.Zero()

#### Troubleshooting FAQ
- [ ] **Common error messages and solutions**
  - "Cannot use variable in Setup" → Use It.Is
  - "Type 'TState' could not be found" → ILogger workaround
  - "Method not setup" → Check argument matching

- [ ] **Variable in Setup workaround**
  - Why variables aren't supported
  - How to use It.Is instead
  - When to use constants

- [ ] **Generic type parameter issues**
  - Why ILogger<T> fails
  - Alternative logging abstractions
  - Generator limitations

- [ ] **Build-time vs runtime errors**
  - Compile errors from generator
  - Runtime exceptions from MockHandler
  - Diagnostic codes (SKUGGA001-005)

#### Performance Tuning Guide
- [ ] **Benchmark setup recommendations**
  - BenchmarkDotNet best practices
  - Measuring mock overhead
  - Comparing frameworks

- [ ] **Identifying mock overhead**
  - When mocks slow down tests
  - AssertAllocations.Zero() usage
  - Profiling with dotnet-trace

- [ ] **Optimizing test suite performance**
  - Parallel test execution
  - Minimize setup complexity
  - Reuse mocks when safe

- [ ] **CI/CD integration best practices**
  - Cache NuGet packages
  - Incremental builds
  - Performance regression detection

---

## 🏆 Milestones & Releases

### Version 1.0.0 (Completed: December 2025)
**Theme:** Foundation & Core Features

**Delivered:**
- ✅ Core mocking API (Setup, Returns, Callback, Verify)
- ✅ Argument matchers (It.IsAny, It.Is, It.IsIn, It.IsRegex)
- ✅ Verification (Times.Never/Once/Exactly/AtLeast/AtMost/Between)
- ✅ AutoScribe feature (self-writing tests)
- ✅ Chaos mode (resilience testing)
- ✅ Zero-Alloc guard (performance testing)
- ✅ 234 tests passing
- ✅ Comprehensive documentation (README, API_REFERENCE, CONTRIBUTING, CHANGELOG)
- ✅ Zero reflection architecture (AOT-compatible)
- ✅ 3.9x faster than Moq benchmark validation

**Commits:** Phases 1-11 (June - December 2025)

### Version 1.1.0 (Completed: January 2026)
**Theme:** Async Support & Benchmarking & Advanced Features

**Delivered:**
- ✅ Async support (ReturnsAsync, Task defaults)
- ✅ SetupSequence (sequential returns/throws)
- ✅ Additional matchers (It.IsNotNull, It.IsNotIn)
- ✅ Protected Members (Protected().Setup<T>("MethodName"))
- ✅ Event Support (Raise() and Raises() methods)
- ✅ Out/Ref Parameters (OutValue(), Ref.IsAny)
- ✅ MockSequence (InSequence() for ordered verification)
- ✅ SetupProperty (automatic property backing fields)
- ✅ Multiple Interfaces (As<T>() for interface composition)
- ✅ Partial Mocks (override specific methods via interceptors)
- ✅ Comprehensive benchmarks (12 Moq scenarios, 4-framework comparison)
  - 6.68x faster than Moq overall
  - 67.62x faster on void method setup
  - 69.84x faster on callback execution
- ✅ Benchmark documentation (MoqVsSkugga.md, FourFramework.md)
- ✅ Hardware specs and methodology documented
- ✅ 371 tests passing (137 new tests)
- ✅ Updated all documentation with benchmark results
- ✅ 98% Moq feature parity achieved

**Commits:** Phases 12-13 (December 30, 2025 - January 1, 2026)

### Version 1.2.0 (Target: Q1 2026)
**Theme:** Developer Experience & Diagnostics

**Planned:**
- ⏳ Enhanced diagnostics (Phase 14 - IN PROGRESS)
  - SKUGGA003: Variable in Setup warning
  - SKUGGA004: Generic type parameter error
  - SKUGGA005: Matcher usage suggestions
- ⏳ Improved error messages with "Did you mean?" suggestions
- ⏳ Troubleshooting FAQ documentation
- ⏳ Video tutorials (quick start, migration, deep dive)
- ⏳ Sample project (ASP.NET Core Web API)
- ⏳ Consolidate roadmap (DONE ✅ January 2, 2026)

**Estimated Release:** Late January - Early February 2026

### Version 2.0.0 (Target: Q3-Q4 2026)
**Theme:** Skugga-Exclusive Features & Ecosystem

**Planned:**
- 🔮 Phase 15: Skugga-exclusive features
  - Smart suggestions (AI-powered test analysis)
  - Advanced chaos strategies (network latency, timeouts, schedules)
  - Performance profiling integration (detailed reports, BenchmarkDotNet)
  - Enhanced AutoScribe (export, replay, diff, timing)
- 🔮 10x performance target (vs Moq) - 50% improvement from current 6.68x
- 🔮 Sample projects (Azure Functions, Kubernetes, Native AOT)
- 🔮 Community feedback integration
- 🔮 Ecosystem integration (IDE support, CI/CD)
- 🔮 Advanced patterns cookbook
- 🔮 1,000+ GitHub stars, 10+ contributors

**Estimated Release:** September - December 2026

---

## 📅 Recent Progress

### January 2, 2026 ✅
**Roadmap Consolidation**
- Consolidated three files: .ROADMAP (483 lines), ROADMAP.md (207 lines), FEATURE_PARITY.md (164 lines)
- Total: 854 lines consolidated into single focused roadmap
- Removed all completed features from Phases 1-13 (.ROADMAP historical data)
- Integrated feature parity tracking into current status section
- Organized around next priorities: Phase 14 (Diagnostics) and Phase 15 (Exclusive Features)
- File remains git-ignored for local development use

**Benchmark Documentation**
- Created fixed filenames: benchmarks/MoqVsSkugga.md, benchmarks/FourFramework.md
- Embedded timestamps in markdown headers (not filenames)
- Removed all .txt files from /benchmarks directory
- Updated all documentation references to fixed filenames
- Updated benchmarks/README.md with comprehensive guide

### January 1, 2026 ✅
**Phase 13: Benchmarking Complete**
- **MoqVsSkugga benchmarks:** 12 comprehensive scenarios
  - Overall: 6.68x faster (4.24 μs vs 28.33 μs)
  - Void Method Setup: 67.62x faster (0.15 μs vs 10.12 μs)
  - Callback Execution: 69.84x faster (0.14 μs vs 9.96 μs)
  - Argument Matching: 34.98-79.84x faster (varies by run)
  - Memory: 4.1x less allocation (1.12 KB vs 4.57 KB)

- **FourFramework benchmarks:** vs Moq, NSubstitute, FakeItEasy
  - Moq: 2.55-3.35x slower than Skugga
  - NSubstitute: 4.34-4.36x slower than Skugga
  - FakeItEasy: 3.09-3.84x slower than Skugga

- **Documentation updates:**
  - Updated README.md with benchmark results
  - Updated docs/BENCHMARK_COMPARISON.md with methodology
  - Updated docs/BENCHMARK_SUMMARY.md with latest results
  - Created src/Skugga.Benchmarks/README.md
  - Hardware specs documented (Intel i7-4980HQ, 16GB RAM, macOS 15.7, .NET 10.0.1)

### December 30, 2025 ✅
**Phase 12: Async Support Complete (100%)**
- Implemented ReturnsAsync() extension methods (value, function, 1-arg, 2-arg)
- Generator now produces proper Task default values
  - Task methods: `return Task.CompletedTask;`
  - Task<T> methods: `return Task.FromResult(default(T));`
- 7 comprehensive async tests passing
  - ReturnsAsync with value
  - ReturnsAsync with function
  - ReturnsAsync with 1-arg callback
  - ReturnsAsync with 2-arg callback
  - Default Task.CompletedTask in loose mode
  - Default Task.FromResult in loose mode
  - Backwards compatibility (Task.FromResult still works)
- Full Moq async API compatibility achieved
- No more NullReferenceException when calling unsetup async methods in Loose mode

### December 2025 ✅
**Phases 1-11: Foundation Complete**
- **Phase 1:** Testing infrastructure, CI/CD, documentation, code quality
  - 170 tests passing (Core + Generator)
  - TreatWarningsAsErrors compliance
  - FluentAssertions integration
  - Coverlet code coverage

- **Phase 2:** Eliminate reflection (CRITICAL)
  - Removed all Expression.Lambda().Compile() calls
  - Removed DispatchProxy runtime fallback
  - Zero reflection in production code
  - 3.9x faster than Moq benchmark validation

- **Phase 3:** API enhancements
  - Setup/Returns/Callback/Verify API
  - Argument matchers (It.IsAny, It.Is, It.IsIn, It.IsRegex)
  - SetupSequence for consecutive returns

- **Phase 4:** Generator enhancements
  - Code formatting improvements
  - Diagnostics (SKUGGA001: sealed classes, SKUGGA002: no virtual members)
  - Stable hash generation (FNV-1a)
  - XML documentation generation

- **Phase 5:** Advanced features
  - Chaos Mode (resilience testing)
  - AutoScribe (self-writing tests)
  - Zero-Alloc Guard (performance testing)

- **Phase 6:** Production-ready documentation
  - API_REFERENCE.md (comprehensive guide)
  - Migration guide from Moq
  - Troubleshooting guide

- **Phases 9-11:** Async improvements
  - ReturnsAsync syntax (shorthand)
  - Async default values
  - Full async test coverage

**Total Effort:** ~6 months of development (June - December 2025)

---

## 🔄 Maintenance & Ongoing Tasks

### Continuous Monitoring (Weekly)
- **GitHub Issues:** Respond within 48 hours
- **Pull Requests:** Review within 1 week
- **Security:** Dependabot alerts monitored daily
- **Performance:** Run benchmarks on each commit to master
- **Tests:** All 362 tests must pass before merge

### Quarterly Reviews (Every 3 Months)
- **Dependencies:** Update quarterly
  - Microsoft.CodeAnalysis.CSharp (Roslyn) - track .NET SDK updates
  - xUnit, FluentAssertions - keep current with latest stable
  - .NET SDK - track .NET 11 preview, C# 13 features
- **Roadmap:** Prioritize based on community feedback
  - Gather GitHub issues/discussions feedback
  - Survey users on feature priorities
  - Adjust Phase 15 scope based on demand
- **Benchmarks:** Re-run on new hardware/OS/runtime
  - Validate 6.68x advantage still holds
  - Update documentation with new results
  - Track performance trends over time
- **Documentation:** Review for accuracy
  - Verify code examples still work
  - Update screenshots if UI changed
  - Check links for 404s
- **Test Coverage:** Analyze with coverlet
  - Maintain >90% code coverage
  - Identify untested edge cases
  - Add regression tests for fixed bugs

### Community Engagement (Ongoing)
- **GitHub Discussions:** Monitor daily, respond within 48 hours
- **Issues:** Triage weekly (label: bug, enhancement, question, help wanted)
- **Pull Requests:** Review within 1 week, provide feedback
- **Monthly Updates:** Blog post or discussion post (if >100 stars)
  - Progress on current phase
  - New features shipped
  - Performance improvements
- **Conference Talks:** Submit proposals to NDC, .NET Conf, etc.
- **Blog Posts:** Write for major releases (1.0, 1.1, 2.0)

---

## 📈 Success Metrics

### Current State (v1.1.0 - January 2026)
- **GitHub Stars:** TBD (not yet published to NuGet/public GitHub)
- **NuGet Downloads:** TBD (not yet published)
- **Test Coverage:** 371 tests passing, ~90% code coverage
- **Performance:** 6.68x faster than Moq, 4.1x less memory
- **Build Time Impact:** <1 second for typical projects
- **Contributors:** 1 (core maintainer)
- **Documentation:** Comprehensive
  - README.md (getting started, examples, benchmarks)
  - API_REFERENCE.md (300+ lines, complete API guide)
  - CONTRIBUTING.md (contributor guidelines)
  - CHANGELOG.md (release history)
  - benchmarks/*.md (performance documentation)

### Target State (v2.0.0 - Q4 2026)
- **GitHub Stars:** 1,000+ (indicates community interest)
- **NuGet Downloads:** 10,000+ (indicates production adoption)
- **Test Coverage:** >95% code coverage
- **Performance:** 10x faster than Moq (50% improvement from current)
- **Build Time Impact:** <500ms for 100 mocks
- **Contributors:** 10+ active contributors
- **Documentation:** Docs site with search
  - Video tutorials (5 videos, 40+ min total)
  - Sample projects (6 projects covering different scenarios)
  - Advanced patterns cookbook
  - Interactive troubleshooting guide

### Leading Indicators (Track Monthly)
- **Issue Resolution Time:** Average <7 days from open to close
- **PR Review Time:** Average <3 days from submission to merge
- **Test Suite Performance:** All tests complete in <30 seconds
- **Community Engagement:** >10 discussions per month (if >100 stars)
- **External Mentions:** Blog posts, tweets, Stack Overflow questions

---

## 🎯 Next Immediate Steps

### This Week (Priority 1 - January 3-9, 2026)
1. **Start Phase 14:** Enhanced diagnostics and error messages
   - Design SKUGGA003, SKUGGA004, SKUGGA005 diagnostic codes
   - Sketch Roslyn analyzer architecture
   - Write design doc for enhanced MockHandler error context

2. **Documentation:** Create troubleshooting FAQ
   - Document "Variable in Setup" workaround (It.Is pattern)
   - Document ILogger<T> generic type parameter issue
   - Document common verification mismatch scenarios

3. **Testing:** Plan diagnostic analyzer tests
   - Identify test scenarios for each diagnostic (positive + negative)
   - Set up Roslyn analyzer test infrastructure
   - Create test project: Skugga.Analyzers.Tests

### This Month (Priority 2 - January 2026)
4. **Implement SKUGGA003 Analyzer:** Variable in Setup warning
   - Detect FieldExpression/VariableExpression in Setup lambda
   - Provide code action to convert to It.Is
   - Write 5+ tests (positive, negative, edge cases)

5. **Implement SKUGGA004 Analyzer:** Generic type parameter error
   - Detect unbound generic type parameters in mocked interfaces
   - Provide helpful error message with workaround
   - Investigate generator fix (may defer to later)

6. **Implement SKUGGA005 Analyzer:** Matcher usage suggestions
   - Detect Setup with exact constants
   - Suggest It.Is as alternative
   - Write tests for suggestion scenarios

7. **Enhanced MockHandler:** Better error messages
   - Capture detailed call information (method, args, timestamp)
   - Format verification mismatch messages with context
   - Implement "Did you mean?" logic (fuzzy string matching)

8. **Video Tutorial:** Record quick start video (5 min)
   - Script: Install NuGet, create mock, setup, verify
   - Record with screen capture + narration
   - Upload to YouTube, embed in README

### This Quarter (Priority 3 - Q1 2026)
9. **Sample Project:** ASP.NET Core Web API with Skugga tests
   - Minimal API with dependency injection
   - Repository pattern with Skugga mocks
   - Integration tests with WebApplicationFactory
   - Publish to GitHub: skugga-samples/aspnetcore-webapi

10. **Performance:** Large-scale mock generation validation
    - Test with 100+ mocks in solution
    - Measure build time impact (target: <10 seconds)
    - Identify generator bottlenecks
    - Optimize if needed (parallel processing)

11. **Community:** Prepare for v1.2.0 release
    - Finalize Phase 14 (enhanced diagnostics)
    - Complete troubleshooting FAQ
    - Record migration tutorial video (10 min)
    - Write blog post: "Skugga 1.2: Better Error Messages, Better DX"
    - Announce on:
      - Reddit: r/dotnet, r/csharp
      - Twitter: @dotnet, #dotnet hashtag
      - Dev.to / Medium
      - GitHub Discussions

12. **Production Readiness:** Integration tests with real-world projects
    - Test Skugga with existing open-source .NET projects
    - Identify edge cases and file issues
    - Gather feedback from early adopters
    - Fix critical bugs before v1.2 release

---

## ⚠️ Notes & Reminders

### Development Principles (Core Philosophy)
- **Zero Reflection:** All mocking logic happens at compile-time via source generators
  - Tests CAN use reflection (xUnit, FluentAssertions are fine)
  - Skugga.Core MUST NOT use reflection in production code
  - Goal: Zero reflection = faster cold starts, lower memory, true AOT compatibility

- **AOT Compatibility:** Full Native AOT support is non-negotiable
  - Must work with PublishAot=true
  - No runtime proxy generation (unlike Moq, NSubstitute)
  - Trimming-safe (no private reflection)

- **Performance First:** Maintain 6.68x speed advantage over Moq
  - Target: 10x faster by v2.0
  - Every feature must be benchmarked
  - No performance regressions allowed

- **Developer Experience:** Clear error messages, helpful diagnostics, comprehensive docs
  - Compile-time errors > runtime exceptions
  - "Did you mean?" suggestions for common mistakes
  - Documentation with examples, not just API reference

### Roadmap Philosophy
- **This roadmap tracks PENDING WORK ONLY**
  - Completed features → CHANGELOG.md and git commit history
  - Historical reference → Commit messages and PRs
  - Focus: What's NEXT, not what's DONE

- **Community Feedback Drives Prioritization**
  - GitHub issues/discussions inform feature priority
  - User surveys for major version planning
  - Early adopters shape Phase 15 scope

- **Performance and Stability > Feature Count**
  - Quality over quantity
  - Deferred features may never ship (and that's okay)
  - Maintain 90%+ test coverage

### File Status & Git Management
- **ROADMAP.md:** Local development use only
  - Listed in .gitignore (line 5)
  - Removed from git tracking with `git rm --cached ROADMAP.md` (January 2, 2026)
  - This file should NOT be committed to git
  - Purpose: Internal planning, not public roadmap

- **FEATURE_PARITY.md:** REMOVED (consolidated here)
  - Content merged into "Current Status" section
  - File to be deleted from repository

- **.ROADMAP:** REMOVED (consolidated here)
  - Content merged into this roadmap
  - Historical phases (1-13) documented in "Recent Progress"
  - File to be deleted from repository

### Communication Guidelines
- **Internal vs External Roadmap:**
  - This file (ROADMAP.md): Internal, detailed, includes deferred features
  - Public roadmap (GitHub Projects): High-level, user-facing, excludes deferred features
  - Users see: "Phase 14: Enhanced Diagnostics" (not "SKUGGA003-005 implementation details")

- **Issue Labels:**
  - `enhancement`: New feature requests
  - `bug`: Something isn't working
  - `documentation`: Improvements or additions to docs
  - `good first issue`: Good for newcomers
  - `help wanted`: Extra attention needed
  - `wontfix`: This will not be worked on (deferred features)
  - `phase-14`, `phase-15`: Link issues to roadmap phases

### Last Review & Update
- **Last Full Review:** January 2, 2026
- **Last Update:** January 2, 2026 (roadmap consolidation)
- **Next Review:** After Phase 14 completion (estimated late January 2026)
- **Review Frequency:** After each major phase completion

### Maintenance Notes
- **This file is LARGE (~900 lines)**
  - Consider splitting into multiple files if it grows >1,500 lines
  - Potential split: ROADMAP.md (high-level), ROADMAP_DETAILED.md (implementation details)

- **Keep it updated:**
  - Mark tasks complete ✅ as they finish
  - Add new tasks as they arise
  - Update "Recent Progress" section monthly
  - Review "Known Issues" quarterly (remove fixed issues)

- **Sync with CHANGELOG.md:**
  - When Phase 14 completes → Update CHANGELOG.md with release notes
  - Keep ROADMAP.md (future) and CHANGELOG.md (past) in sync
  - Reference CHANGELOG.md for historical context

---

**File Metadata:**
- **Total Lines:** ~900
- **Consolidated From:** 
  - .ROADMAP (483 lines) - Phases 1-13 historical data
  - Old ROADMAP.md (207 lines) - Phase 14 & 15 initial draft
  - FEATURE_PARITY.md (164 lines) - Feature tracking matrix
- **Total Source:** 854 lines consolidated
- **Reduction:** ~5% consolidation gain while maintaining all critical information
- **Organization:** Removed completed work, focused on NEXT priorities (Phase 14 & 15)
