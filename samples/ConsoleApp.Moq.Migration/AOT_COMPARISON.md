# Native AOT Compilation: Moq vs Skugga

This document shows the **actual build results** when attempting to compile with Native AOT.

## 🔴 Step1: Moq FAILS with Native AOT

### Configuration
```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>

<PackageReference Include="Moq" Version="4.20.72" />
```

### Build Command
```bash
cd Step1-WithMoq
dotnet publish -c Release
```

### ❌ Result: BUILD FAILED

```
/Users/digvijay/.nuget/packages/moq/4.20.72/lib/net6.0/Moq.dll : 
  error IL2104: Assembly 'Moq' produced trim warnings.
  
/Users/digvijay/.nuget/packages/moq/4.20.72/lib/net6.0/Moq.dll : 
  error IL3053: Assembly 'Moq' produced AOT analysis warnings.
  
/Users/digvijay/.nuget/packages/castle.core/5.1.1/lib/net6.0/Castle.Core.dll : 
  error IL2104: Assembly 'Castle.Core' produced trim warnings.
  
/Users/digvijay/.nuget/packages/castle.core/5.1.1/lib/net6.0/Castle.Core.dll : 
  error IL3053: Assembly 'Castle.Core' produced AOT analysis warnings.

error MSB3073: The command "ilc" exited with code -1.

Build FAILED.
```

### 💥 Root Cause

Moq relies on **Castle.Core DynamicProxy** which uses:
- `System.Reflection.Emit` (runtime code generation)
- `Type.MakeGenericType()` (dynamic type construction)
- `MethodInfo.Invoke()` (reflection-based invocation)

All of these are **incompatible with Native AOT** because AOT requires all types and methods to be known at compile time.

---

## 🟢 Step2: Skugga SUCCEEDS with Native AOT

### Configuration
```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);Skugga.Generated</InterceptorsPreviewNamespaces>
</PropertyGroup>

<ProjectReference Include="../../../src/Skugga.Core/Skugga.Core.csproj" />
<ProjectReference Include="../../../src/Skugga.Generator/Skugga.Generator.csproj" 
                  OutputItemType="Analyzer" 
                  ReferenceOutputAssembly="false" />
```

### Build Command
```bash
cd Step2-WithSkugga
dotnet publish -c Release
```

### ✅ Result: BUILD SUCCEEDED

```
Restore complete (1.5s)
  Skugga.Core net8.0 succeeded (0.8s)
  Skugga.Generator netstandard2.0 succeeded (1.1s)
  Step2-WithSkugga net8.0 succeeded (2.3s)
  
Generating native code

Build succeeded in 10.2s
  
Step2-WithSkugga -> 
  /bin/Release/net8.0/osx-x64/publish/
```

### 🎯 Why It Works

Skugga uses **compile-time code generation** via Roslyn Source Generators:
- All mock implementations generated at **compile time**
- **Zero reflection** at runtime
- **Zero dynamic code generation** at runtime
- All types known to the AOT compiler

The source generator creates explicit mock classes during compilation, which the AOT compiler can analyze and optimize.

---

## 📊 Side-by-Side Comparison

| Aspect | Moq (Step1) | Skugga (Step2) |
|--------|-------------|----------------|
| **AOT Compilation** | ❌ FAILS (IL2104, IL3053) | ✅ SUCCEEDS |
| **Reflection Usage** | Heavy (Castle.Core) | None |
| **Runtime Code Gen** | Yes (DynamicProxy) | No |
| **Binary Size** | N/A (doesn't compile) | ~6-8 MB |
| **Startup Time** | N/A | ~10-20ms |
| **Trim-Safe** | ❌ No | ✅ Yes |
| **AOT Warnings** | 100+ warnings | 0 warnings |

---

## 🧪 Try It Yourself

### Test Moq Failure
```bash
cd samples/ConsoleApp-Moq-To-Skugga-Migration/Step1-WithMoq
dotnet publish -c Release

# Expected: Build fails with IL2104 and IL3053 errors
```

### Test Skugga Success
```bash
cd samples/ConsoleApp-Moq-To-Skugga-Migration/Step2-WithSkugga
dotnet publish -c Release

# Expected: Build succeeds, binary created in bin/Release/net8.0/osx-x64/publish/
```

### Run the Binaries
```bash
# Step1: Won't exist (build failed)
cd Step1-WithMoq
ls bin/Release/net10.0/osx-x64/publish/
# (empty - no binary produced)

# Step2: Works perfectly
cd Step2-WithSkugga
./bin/Release/net8.0/osx-x64/publish/Step2-WithSkugga
# ✅ Runs successfully with Native AOT!
```

---

## 📖 Learn More

- **IL2104**: Trim analysis warnings indicate reflection-based code that cannot be analyzed
- **IL3053**: AOT analysis warnings indicate runtime code generation or other AOT-incompatible patterns
- **Castle.Core**: The proxy generation library Moq depends on, fundamentally incompatible with AOT

For detailed technical explanation of why Moq fails and how Skugga solves it, see [README.md](./README.md).
