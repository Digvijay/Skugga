#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

Write-Host "📦 Building Containers..." -ForegroundColor Cyan

# Build JIT
Write-Host "   [1/3] Building Standard JIT (Debian)..." -ForegroundColor DarkGray
docker build -t skugga-jit -f Dockerfile.jit . | Out-Null

# Build AOT (Distroless)
Write-Host "   [2/3] Building Skugga AOT (Alpine)..." -ForegroundColor DarkGray
docker build -t skugga-aot -f Dockerfile.aot . | Out-Null

# Build AOT (Chiseled)
Write-Host "   [3/3] Building Skugga AOT (Chiseled)..." -ForegroundColor DarkGray
docker build -t skugga-chiseled -f Dockerfile.chiseled . | Out-Null

Write-Host "`n📊 Image Footprint:" -ForegroundColor Yellow
docker images --format "table {{.Repository}}\t{{.Size}}" | Select-String "skugga-"

Write-Host "`n⚡ Measuring Cold Start (Container Run Time)..." -ForegroundColor Yellow

# Measure JIT
$jitTime = Measure-Command { 
    docker run --rm skugga-jit --benchmark 
}
Write-Host "   Standard JIT: $($jitTime.TotalMilliseconds) ms" -ForegroundColor Gray

# Measure AOT
$aotTime = Measure-Command { 
    docker run --rm skugga-aot --benchmark 
}
Write-Host "   Skugga AOT (Alpine):   $($aotTime.TotalMilliseconds) ms" -ForegroundColor Green

# Measure Chiseled
$chiseledTime = Measure-Command { 
    docker run --rm skugga-chiseled --benchmark 
}
Write-Host "   Skugga AOT (Chiseled): $($chiseledTime.TotalMilliseconds) ms" -ForegroundColor Green

Write-Host "`n✅ Comparison Complete." -ForegroundColor Cyan