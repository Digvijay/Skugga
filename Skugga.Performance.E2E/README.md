# 🔬 Skugga Pilot Study

**Verification of Zero-Overhead Mocking for Cloud-Native .NET**

This directory contains the source code and benchmarks for the **Skugga Pilot**, a vertical-slice microservice designed to quantify the benefits of moving mocking from Runtime (Reflection) to Compile-Time (Source Generation).

---

## 🎯 Objectives

1.  **Prove AOT Compatibility**: Demonstrate a complex test suite running in a 100% Native AOT environment.
2.  **Measure Efficiency**: Quantify CPU and Memory reductions compared to standard JIT-based mocking (e.g., Moq).
3.  **Verify Scalability**: Ensure the Source Generator does not degrade build times under load (500+ mocks).

## 📊 Benchmark Results

Running the `Run-Benchmarks.ps1` script yields the following empirical data:

### ⚡ Computational Efficiency
*Lower is better.*

| Metric | Standard .NET (JIT) | Skugga (Native AOT) | Improvement |
| :--- | :--- | :--- | :--- |
| **User CPU Time** | 0.36s | **0.07s** | **5.1x Faster** 🚀 |
| **System Overhead** | 0.18s | **0.12s** | **33% Lower** |

### 📦 Deployment Footprint
*Smaller is better.*

| Artifact | Size | Notes |
| :--- | :--- | :--- |
| **Standard JIT** | ~200 MB | Requires full .NET Runtime & OS libs |
| **Skugga AOT** | **47 MB** | Self-contained, "Distroless" ready |

---

## 🛠️ How to Reproduce

### Prerequisites
*   .NET 8.0+ SDK
*   Docker (optional, for container size verification)

### Scripts

We provide PowerShell scripts to automate the verification process.

#### 1. Performance Benchmark
Compiles and runs the microservice workload in both JIT and AOT modes, reporting CPU time.
```powershell
./Run-Benchmarks.ps1
```

#### 2. Stress Test (Compiler Load)
Generates 500 distinct test classes to verify that Skugga's incremental generator does not slow down the "Inner Loop".
```powershell
./Run-StressTest.ps1
```

---

## 📂 Project Structure

*   **`src/`**: The domain logic and API endpoints.
*   **`tests/`**:
    *   `Functional/`: Standard unit tests using Skugga.
    *   `Stress/`: Large-scale generation tests.
*   **`artifacts/`**: Output directory for benchmark reports.

---

⬅️ Back to Main Repository