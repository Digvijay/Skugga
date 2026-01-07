# Skugga Samples - World-Class Demonstrations

> **"Learn by doing. See the value immediately."**

This directory contains comprehensive, production-ready demos showcasing Skugga's revolutionary features. Each sample is designed to be world-class: clear problem statements, concrete solutions, and quantified ROI.

---

## 🎯 Quick Start

**Two ways to use these samples:**

### Option 1: Explore in This Repository

```bash
# You cloned the repo - samples work out of the box
cd samples/ChaosEngineeringDemo
dotnet test --logger "console;verbosity=detailed"
```

### Option 2: Use in Your Own Project

```bash
# Install Skugga via NuGet
dotnet add package Skugga

# Copy sample code and adapt to your needs
# Full guide: See GETTING_STARTED.md
```

**[📖 Complete Setup Guide →](./GETTING_STARTED.md)** - Covers both scenarios with troubleshooting

---

## 📚 Available Demos

### 1. Doppelgänger Demo 🤖 - **Contract Drift Detection**

**Problem:** Your mocks drift from real APIs. Tests pass, production crashes.

**Solution:** Generate mocks from OpenAPI specs. Build fails when APIs change.

**ROI:** $23K-$33K/year in prevented incidents.

```bash
cd DoppelgangerDemo
dotnet test --logger "console;verbosity=detailed"
```

**[📖 Full Documentation →](./DoppelgangerDemo/README.md)**

**Key Demos:**
- ✨ Auto-generation from OpenAPI
- 💥 Contract drift detection (the killer demo!)
- 🔐 Authentication mocking
- 🗄️ Stateful CRUD behavior

---

### 2. AutoScribe Demo ✍️ - **Self-Writing Tests**

**Problem:** Setting up mocks for complex controllers takes 15+ minutes.

**Solution:** Record real interactions, generate test code automatically.

**ROI:** 10x faster test writing (15 min → 30 sec).

```bash
cd AutoScribeDemo
dotnet test --filter "ComplexOrder" --logger "console;verbosity=detailed"
```

**[📖 Full Documentation →](./AutoScribeDemo/README.md)**

**Key Demos:**
- 📝 Simple example (2 dependencies)
- 🚀 Complex example (9 dependencies - the impressive one!)
- 🎯 Side-by-side comparison (manual vs AutoScribe)

---

### 3. Chaos Engineering Demo 🔥 - **Resilience Testing**

**Problem:** How do you KNOW your retry logic and circuit breakers actually work?

**Solution:** Inject chaos (failures, latency, timeouts) into mocks to test resilience.

**ROI:** Prevent $4.7M/year in downtime costs.

```bash
cd ChaosEngineeringDemo
dotnet test --logger "console;verbosity=detailed"
```

**[📖 Full Documentation →](./ChaosEngineeringDemo/README.md)**

**Key Demos:**
- ❌ Without resilience (crashes immediately)
- ✅ With retry policy (survives chaos)
- ⏱️ Chaos with delays (test timeouts)
- 📊 Statistics (precise metrics)

---

### 4. Zero-Allocation Testing Demo ⚡ - **Performance Enforcement**

**Problem:** "Optimized" code silently allocates 50MB. GC pauses kill throughput.

**Solution:** Enforce zero-allocation contracts with precise GC-level measurements.

**ROI:** 10x throughput improvement, $50K-$100K/year cloud savings.

```bash
cd AllocationTestingDemo
dotnet test --logger "console;verbosity=detailed"
```

**[📖 Full Documentation →](./AllocationTestingDemo/README.md)**

**Key Demos:**
- 📝 String concat vs Span<T> (50MB → 0 bytes)
- 🔄 LINQ vs for loop (10x difference)
- 📦 Boxing detection and elimination
- 🛡️ Zero-allocation enforcement

---

### 5. ASP.NET Core Migration Demo 🌐

Shows migration from Moq to Skugga in real ASP.NET Core projects.

```bash
cd AspNetCoreWebApi.Moq.Migration
dotnet test
```

**Key Features:**
- Side-by-side Moq vs Skugga comparisons
- AOT compatibility validation
- RESTful API testing patterns

---

### 6. Console App Migration Demo 🖥️

Demonstrates Moq → Skugga migration for console applications.

```bash
cd ConsoleApp.Moq.Migration
dotnet test
```

**Key Features:**
- Step-by-step migration guide
- Feature parity demonstrations
- Performance comparisons

---

### 7. Azure Functions Demo ☁️ - **Non-Invasive Testing**

Shows how to test Azure Functions without modifying production code.

```bash
cd AzureFunctions.NonInvasive
dotnet test
```

**Key Features:**
- Zero production code changes
- Native AOT compatibility
- Serverless testing patterns

---

## 🏆 What Makes These Samples World-Class?

### 1. Clear Problem Statements
Every demo starts with a real problem developers face daily.

### 2. Concrete Solutions  
Shows exactly how Skugga solves the problem with runnable code.

### 3. Quantified ROI
Real numbers: time saved, money saved, performance improvements.

### 4. Progressive Learning
Simple examples → complex examples → advanced features.

### 5. Before/After Comparisons
Side-by-side demonstrations showing the difference.

### 6. Industry Unique Features
Highlights features NO other mocking library has.

### 7. Production-Ready Code
All examples mirror real-world scenarios and best practices.

---

## 📊 Feature Comparison Across Demos

| Feature | Doppelgänger | AutoScribe | Chaos | Allocation |
|---------|-------------|------------|-------|------------|
| **Industry First** | ✅ | ✅ | ✅ | ✅ |
| **Quantified ROI** | $23K-$33K | 10x faster | $4.7M | $50K-$100K |
| **Native AOT** | ✅ | ✅ | ✅ | ✅ |
| **Zero Reflection** | ✅ | ✅ | ✅ | ✅ |
| **Problem → Solution** | ✅ | ✅ | ✅ | ✅ |
| **Before/After** | ✅ | ✅ | ✅ | ✅ |

---

## 🎓 Learning Path

### **Beginner** - Start Here
1. **AutoScribeDemo** - Easiest to understand, immediate value
2. **ChaosEngineeringDemo** - Fun and visual
3. **AllocationTestingDemo** - Eye-opening performance insights

### **Intermediate**
4. **DoppelgangerDemo** - More advanced concept but huge value
5. **ConsoleApp.Moq.Migration** - See feature parity

### **Advanced**
6. **AspNetCoreWebApi.Moq.Migration** - Real-world migration
7. **AzureFunctions.NonInvasive** - Serverless patterns

---

## 🔧 Running All Demos

```bash
# Run each demo individually
for dir in */; do
  echo "Running $dir..."
  cd "$dir"
  dotnet test --logger "console;verbosity=detailed"
  cd ..
done
```

---

## 💡 Sample Standards

Each sample follows these standards:

### ✅ README Quality
- Clear problem statement
- Concrete solution with code
- Quick start (< 1 minute to first run)
- Quantified value/ROI
- Learning objectives
- Links to detailed docs

### ✅ Code Quality
- Production-ready patterns
- Comprehensive comments
- xUnit best practices
- FluentAssertions for readability
- Native AOT compatible

### ✅ Output Quality
- Formatted console output
- Before/After comparisons
- Statistics and metrics
- Clear success indicators

---

## 🚀 Contributing New Samples

Want to add a sample? Follow this template:

```markdown
# [Feature Name] Demo [Emoji]

> **"One-line value proposition"**

## The Problem
[Clear problem statement with code example]

## The Solution
[Skugga solution with code example]

## Quick Start
```bash
cd [DemoName]
dotnet test --logger "console;verbosity=detailed"
```

## The Demos
[List of demos with what you'll learn]

## ROI
[Quantified value - time saved, money saved, etc.]

## Learn More
[Links to docs]
```

---

## 📖 Additional Resources

- **Main README:** [/README.md](../README.md)
- **Full Documentation:** [/docs/](../docs/)
- **API Reference:** [/docs/API_REFERENCE.md](../docs/API_REFERENCE.md)
- **Contributing Guide:** [/CONTRIBUTING.md](../CONTRIBUTING.md)

---

**Built by [Digvijay Chauhan](https://github.com/Digvijay)** • Open Source • MIT License

*World-class samples for a world-class mocking library.*
