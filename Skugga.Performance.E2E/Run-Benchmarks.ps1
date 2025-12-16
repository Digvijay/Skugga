#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

Write-Host "🚀 Starting Skugga Benchmark Suite..." -ForegroundColor Cyan

# Detect Runtime Identifier (RID) for AOT
$RID = dotnet --info | Select-String "RID" | ForEach-Object { $_.ToString().Split(":")[1].Trim() }
Write-Host "   Target Runtime: $RID" -ForegroundColor DarkGray

# 1. JIT Benchmark
Write-Host "`n[1/2] Benchmarking Standard JIT..." -ForegroundColor Yellow
dotnet build src/Skugga.Performance.E2E.csproj -c Release --nologo --verbosity quiet
$jitTime = Measure-Command {
    dotnet run --project src/Skugga.Performance.E2E.csproj -c Release --no-build -- --benchmark
}
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ JIT Benchmark Failed. Please check prerequisites." -ForegroundColor Red
    exit 1
}
Write-Host "   Time: $($jitTime.TotalSeconds) seconds" -ForegroundColor Green

# 2. AOT Benchmark
Write-Host "`n[2/2] Benchmarking Native AOT..." -ForegroundColor Yellow
dotnet publish src/Skugga.Performance.E2E.csproj -c Release -r $RID /p:PublishAot=true -o artifacts/aot --nologo --verbosity quiet
$aotTime = Measure-Command {
    ./artifacts/aot/Skugga.Performance.E2E --benchmark
}
Write-Host "   Time: $($aotTime.TotalSeconds) seconds" -ForegroundColor Green

Write-Host "`n✅ Benchmarks Complete." -ForegroundColor Cyan