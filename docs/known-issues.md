# Known issues

This file records defects found in Skugga, what they affected, and how they were resolved. It is
kept honest deliberately: a compile-time mocking library earns trust by being explicit about the
ways it has been wrong, because its failures are invisible by construction.

| # | Issue | Severity | Status |
|---|---|---|---|
| 1 | Culture-dependent literals silently voided mock setups | High | **Fixed in 1.6.0** |
| 2 | OpenAPI generator emitted uncompilable code | High | **Fixed in 1.6.0** |
| 3 | Redundant SourceLink reference pulled a vulnerable package | Moderate | **Fixed in 1.6.0** |
| 4 | Single target framework forced consumers onto the newest runtime | Moderate | **Fixed in 1.6.0** |
| 5 | `dotnet pack` failed on the solution (NU5017) | Moderate | **Fixed in 1.6.0** |
| 6 | Samples and tests were published as NuGet packages | Moderate | **Fixed in 1.6.0** |
| 7 | Benchmarks project silently ignored all repository build settings | Moderate | **Fixed in 1.6.0** |
| 8 | CI never ran on the default branch | Moderate | **Fixed in 1.6.0** |
| 9 | Generator assemblies leaked into consumers as compile references | Moderate | **Fixed in 1.6.0** |
| 10 | Build warnings on the .NET 11 SDK and in samples | Low | **Fixed in 1.6.0** |
| 11 | Generator pipeline re-runs on every edit | Low | **Partially fixed** |

---

## 1. Mock setups were silently ignored outside invariant-like locales

**Severity: high. Fixed in 1.6.0. If you use decimal, double or float arguments in `Setup`, and
your machine or CI agent is not configured for an invariant-like culture, upgrade.**

### What happened

A configured mock returned `default` instead of the configured value, as though `Setup` had never
been called. There was no exception, no compiler warning and no diagnostic.

### Root cause

`GeneratorHelpers.FormatConstantValue` formatted numeric literals with culture-sensitive string
interpolation:

```csharp
if (value is decimal m) return $"{m}m";   // ambient culture
```

Generated source is **C# code**. C# numeric literals are defined in terms of the invariant culture
regardless of the culture the compiler happens to run under. On a machine using `,` as the decimal
separator, this setup:

```csharp
mock.Setup(x => x.CalculateDiscount(999.99m, "Electronics")).Returns(99.99m);
```

emitted its argument array as:

```csharp
new object?[] { 999,99m, "Electronics" }   // three elements, not two
```

The argument **count** then never matched the real invocation, `MockSetup.Matches` rejected it, and
the setup was silently skipped.

`MockSetup.AreArgumentsEquivalent` was correct throughout. The defect was purely in what the
generator emitted.

### Why it matters more than an ordinary bug

For a mocking library this is the worst available failure mode. The test still compiles and still
runs — it simply stops testing what it claims to test. It then either passes vacuously, or fails
while pointing at the system under test rather than at the mock, sending the reader to debug
correct code.

It is also locale-dependent, which means it reproduces on a developer's laptop but not on an
en-US CI agent, or the reverse.

### Fix

All numeric emission formats through `CultureInfo.InvariantCulture`, using round-trip (`"R"`)
formatting for `float` and `double` so the literal reconstructs the exact value, and an
`IFormattable` fallback so culture-specific negative signs cannot leak in either.

`AutoScribeCodeGenerator` contained a second copy of the same logic, which it *emitted into
generated code*; that copy was fixed as well.

### Proof

`tests/Skugga.Core.Tests/DecimalArgumentMatchingTests.cs` covers `decimal`, `double`, `float`, and
a mixed `int`/`string` control case. Two of the four fail without the fix.

`samples/AspNetCoreWebApi.Moq.Migration/Step2.WithSkugga.Tests` — three tests that were failing now
pass (12/12). `Skugga.Core.Tests` is 465/465.

---

## 2. The OpenAPI generator emitted code that would not compile

**Severity: high. Fixed in 1.6.0. Affected any OpenAPI schema with a non-integer numeric example.**

`Skugga.OpenApi.Tests` could not build:

```
IAllOfTestApi_Mock.g.cs(25,94): error CS0747: Invalid initializer member declarator
```

The generated source read:

```csharp
new AllOf_Product { Id = 123, Name = "Widget", Price = 29,99, InStock = true }
```

Same root cause as issue 1 — culture-sensitive numeric formatting — but inside an object
initializer, where the stray comma begins a new member and the compiler rejects it outright.

The two together are a useful illustration: one code path failed loudly at compile time, the other
failed silently at run time, from one shared mistake. Only the loud one was ever likely to be
noticed.

**Fix.** A single invariant formatter in `ExampleGenerator`, applied to all 20 duplicated emission
sites. Generated HTTP header values in `MockGenerator` and diagnostic text in `DocumentValidator`
were made invariant too — a header carrying `29,99` is wrong on the wire regardless of locale.

**Proof.** `Skugga.OpenApi.Tests` builds and passes 195/201 (6 pre-existing skips), having
previously been unable to build at all. Generated output now reads `Price = 29.99`.

---

## 3. Redundant SourceLink reference pulled a vulnerable package

**Fixed in 1.6.0.** `Microsoft.SourceLink.GitHub` has been built into the SDK since .NET 8. The
explicit reference pulled `Microsoft.Build.Tasks.Git` and with it GHSA-23fw-v26w-5fgq. Removed;
SourceLink still works, because the SDK provides it.

---

## 4. A single target framework forced consumers onto the newest runtime

**Fixed in 1.6.0.** Skugga now multi-targets `net8.0` (LTS) and `net10.0` (current). `net11.0` is
validated in CI behind an opt-in switch but is not shipped in released packages, so a preview
runtime is never imposed on consumers.

---

## 5. `dotnet pack` failed on the solution

**Fixed in 1.6.0. Pre-existing — confirmed by reproducing against the previous release commit.**

```
error NU5017: Cannot create a package that has no dependencies nor content.
```

`Skugga.OpenApi.Generator` and `Skugga.OpenApi.Tasks` both set `IncludeBuildOutput=false`, because
their assemblies are packed into `analyzers/` and `tasks/` rather than `lib/`. The repository-wide
`IncludeSymbols=true` then asked NuGet to build a **symbol** package for them, and a symbol package
built from no build output has nothing in it.

Nothing was wrong with the shipped packages — but the repository could not be packed, so the CI
pack step would have failed on its first run.

**Fix.** `IncludeSymbols=false` on those two projects, with a comment explaining why. `dotnet pack
Skugga.slnx` now exits 0.

## 6. Samples and tests were published as NuGet packages

**Fixed in 1.6.0.** `dotnet pack` on the solution produced `Step1-WithMoq.nupkg`,
`Step2-WithSkugga.nupkg`, `OrdersApi.nupkg` and `Skugga.Benchmarks.nupkg` alongside the three real
packages. These would have been uploaded as CI release artifacts, and could have been pushed to a
feed by accident.

**Fix.** Only projects under `src/` are packable. `dotnet pack` now produces exactly `Skugga`,
`Skugga.OpenApi` and `Skugga.OpenApi.Tasks`.

`Skugga.OpenApi.Tasks` was also pinned at version 1.0.0 while the packages it ships alongside were
at 1.6.0; it now tracks the same version.

## 7. The benchmarks project silently ignored all repository build settings

**Fixed in 1.6.0.**

`tests/Skugga.Benchmarks/Directory.Build.props` did not import the file above it:

```xml
<Project>
  <PropertyGroup>
    <InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);Skugga.Generated</InterceptorsPreviewNamespaces>
  </PropertyGroup>
</Project>
```

MSBuild stops walking up the directory tree at the first `Directory.Build.props` it finds. Without
an explicit `GetPathOfFileAbove` import, this file **terminated the chain**, so the benchmarks
project silently inherited nothing from `tests/Directory.Build.props` or the root
`Directory.Build.props`: no `LangVersion 12`, no `Nullable`, no analysis level, no AOT flags, no
deterministic build settings, no package metadata, and not `IsPackable=false`.

The tell is the file's own contents. The `InterceptorsPreviewNamespaces` line was added here with a
comment saying it was "needed for Skugga's source generator to work" — it was needed only because
the import was missing. A workaround had been written for the symptom.

This matters beyond tidiness: the benchmark numbers used to make performance claims were produced
by a project compiled under **different settings** from the library it measures.

**Fix.** The import was added. Verified with
`dotnet msbuild tests\Skugga.Benchmarks -getProperty:IsPackable -getProperty:LangVersion`, which
now reports `false` and `12` rather than `true` and the SDK default.

---

## 8. CI never ran on the default branch

**Severity: moderate. Fixed in 1.6.0. Affected the build only, not consumers.**

The CI workflow triggered on pushes and pull requests to `main`. Skugga's default branch is
`master`, so no pull request to this repository was ever built or tested by CI, and every green
result described above came from local runs only. The workflow also ran on Linux alone.

**Fix:** the workflow triggers on `master` and runs on `ubuntu-latest` and `windows-latest`.

---

## 9. Generator assemblies leaked into consumers as compile references

**Severity: moderate. Fixed in 1.6.0. Affected project-reference consumers, not the NuGet package.**

`Skugga.Core.csproj` hooked `GetTargetPath` with a `GetDependencyTargetPaths` target that added
`Skugga.Generator.dll`, `Skugga.Core.Generators.dll` and `Skugga.OpenApi.Generator.dll` to its
own target path. Any project referencing `Skugga.Core` therefore compiled against three Roslyn
analyzer assemblies as if they were libraries. The comment said it was "for packaging"; packaging
never used it — the package takes the generators from explicit `analyzers/dotnet/cs` items.

Two symptoms followed. `Skugga.Core.Tests` reported `MSB3277` on `net8.0`, because the generators
depend on `System.Collections.Immutable` 9.0 and the `net8.0` framework supplies 8.0. And
`Skugga.OpenApi.Tests`, which calls generator types directly, compiled only because of the leak: it
referenced the OpenAPI generator with `ReferenceOutputAssembly="false"`.

**Fix:** the hook is deleted and `Skugga.OpenApi.Tests` references its generator's assembly
explicitly. The solution builds with no `MSB3277`, every test passes, and the `Skugga` package
was packed before and after the change and its file list compared: identical.

---

## 10. Build warnings on the .NET 11 SDK and in samples

**Severity: low. Fixed in 1.6.0. Affected the build only.**

* `NU1510` (SDK 11): `Skugga.OpenApi.Tests` referenced `System.Memory`, `System.Buffers` and
  `System.Runtime.CompilerServices.Unsafe`, all of which the target frameworks already provide.
  Removed. The same references in the generator projects are kept; they are needed for
  `netstandard2.0`.
* `CA1050` × 54: every type in `Skugga.Benchmarks` was in the global namespace. The four source
  files now declare `namespace Skugga.Benchmarks;`.
* `CA1873`: both Moq-migration sample controllers logged through `LogInformation` with a params
  array, allocating on every call even when logging is disabled. They now use a source-generated
  `[LoggerMessage]` method — the pattern a sample for this toolkit should teach.

The solution now builds with zero warnings on SDK 10 and on SDK `11.0.100-rc.1.26425.128`.

---

## How these were found

Issues 1 and 2 were found by building and testing the **entire solution** — samples, playgrounds
and secondary test projects included — rather than the primary test project, and by the fact that
the machine running the build is configured for a Swedish locale.

Issues 5, 6 and 7 were found by running `dotnet pack` on the solution for the first time while
adding a CI step that publishes packages as artifacts.

None would have been caught by the library's own primary test suite, because that suite ran in a
single project under a single culture and never packed anything. The wider context is recorded in
the Viking Air integration repository's `docs/known-issues.md`, which documents defects found
across all four libraries.


---

# Partially fixed

## 11. The generator pipeline re-runs on every edit

**Severity: low. Partially fixed; the remainder is open. Affects IDE responsiveness only; build
output is correct.**

`SkuggaGenerator` selected every `InvocationExpressionSyntax` in the compilation, performed
semantic lookups on many of them, and then combined the results with `CompilationProvider`. The
compilation changes on every keystroke, so all mock generation re-ran on every edit in every
consuming project, regardless of whether a `Mock.Create` call changed. That is a real cost for a
toolkit whose case rests on doing less work.

**Fixed:** the predicate is now a syntactic name check (`IsCandidateInvocation`, matching `Create`,
`Capture`, `Of`, `Partial` and the `MockRepository` entry points) instead of every invocation in
the file, so the semantic model is consulted for a small fraction of the nodes it used to be. And
the `CompilationProvider.Combine` is gone — its value was destructured and then never read, so it
was pure cost: it forced the output stage to re-run on every keystroke and bought nothing.

`GeneratorIncrementalityTests` pins both. The test asserting that the output stage does not depend
on the compilation was run against the previous code first and fails there with
`Expected steps.Keys {"Compilation", "SourceOutput"} to not contain "Compilation"`, so it is known
to catch the regression rather than merely to pass.

**Still open:** `TargetInfo` carries `INamedTypeSymbol`, `Location` and syntax nodes, and the five
downstream generators (mock, interceptor, harness, recording proxy, setup/verify interceptor) are
all symbol-driven. A probe confirms the output step still reports `Modified` on an identical
re-run. Full caching requires extracting a value model for the entire interface graph, which is a
large change against 1922 tests and should not be rushed alongside the rest of this work.

A shortcut was considered and rejected: giving `TargetInfo` an `Equals` based on the symbol's
display-string key. It would make the tests above pass and it would be a correctness bug. Two
compilations can present the same key for an interface whose members have changed, and the
generator would then serve the previous compilation's generated code. A caching fix that can emit
stale output is worse than the cost it removes.

Two caveats belong here rather than above, because neither is a defect and both bound what the
entries are worth:

* Every result recorded here was produced on a single Windows ARM64 machine. CI has never executed
  on a GitHub-hosted runner, so nothing above is confirmed on x64 or on Linux.
* The `net11.0` leg is opt-in via `IncludePreviewTargetFramework`. It has been exercised on the
  same machine with SDK `11.0.100-rc.1.26425.128` (restore, build and every test, `net8.0`,
  `net10.0` and `net11.0`), with no failures. A release candidate is not a release; the leg
  should be re-run against the GA SDK.

A record of ten fixed defects and one partly fixed measures how hard this repository was looked at.
It is not a claim that there is nothing left to find — and as the section above says, none of these
would have been caught by the library's own primary test suite.
