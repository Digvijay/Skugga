#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

Write-Host "🚀 Starting Startup Time Benchmark Suite..." -ForegroundColor Cyan

# 1. Build Docker Images
Write-Host "`n[1/3] Building Docker Images..." -ForegroundColor Yellow
docker build -f Dockerfile.jit -t skugga-jit .
docker build -f Dockerfile.aot -t skugga-aot .

# 2. Benchmark JIT Startup Time
Write-Host "`n[2/3] Benchmarking JIT Startup Time..." -ForegroundColor Yellow
$jitStartupTime = Measure-Command {
    $containerId = docker run -d -p 8080:8080 skugga-jit
    try {
        # Wait for the "started in" message in the logs
        $timeout = 30 # seconds
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        while ($stopwatch.Elapsed.TotalSeconds -lt $timeout) {
            $logs = docker logs $containerId
            if ($logs -like "*started in*") {
                break
            }
            Start-Sleep -Milliseconds 100
        }
        $stopwatch.Stop()

        $logs = docker logs $containerId
        $startupTimeLine = $logs | Select-String "started in"
        $startupTime = ($startupTimeLine -split " ")[3]
        Write-Host "   JIT Startup Time: $startupTime ms" -ForegroundColor Green
    }
    finally {
        docker stop $containerId | Out-Null
        docker rm $containerId | Out-Null
    }
}

# 3. Benchmark AOT Startup Time
Write-Host "`n[3/3] Benchmarking AOT Startup Time..." -ForegroundColor Yellow
$aotStartupTime = Measure-Command {
    $containerId = docker run -d -p 8080:8080 skugga-aot
    try {
        # Wait for the "started in" message in the logs
        $timeout = 30 # seconds
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        while ($stopwatch.Elapsed.TotalSeconds -lt $timeout) {
            $logs = docker logs $containerId
            if ($logs -like "*started in*") {
                break
            }
            Start-Sleep -Milliseconds 100
        }
        $stopwatch.Stop()

        $logs = docker logs $containerId
        $startupTimeLine = $logs | Select-String "started in"
        $startupTime = ($startupTimeLine -split " ")[3]
        Write-Host "   AOT Startup Time: $startupTime ms" -ForegroundColor Green
    }
    finally {
        docker stop $containerId | Out-Null
        docker rm $containerId | Out-Null
    }
}

Write-Host "`n✅ Benchmarks Complete." -ForegroundColor Cyan
