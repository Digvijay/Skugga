#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

Write-Host "🔥 Starting Compiler Stress Test..." -ForegroundColor Cyan

# Measure Build Time
Write-Host "   Running Stress Test (500 iterations)..." -ForegroundColor DarkGray
$buildTime = Measure-Command {
    dotnet test tests/Skugga.Performance.E2E.Tests/Skugga.Performance.E2E.Tests.csproj -c Release --nologo --verbosity quiet
}

Write-Host "   Test Time: $($buildTime.TotalSeconds) seconds" -ForegroundColor Green
Write-Host "✅ Stress Test Complete." -ForegroundColor Cyan