#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

Write-Host "🚀 Starting AOT Startup Time Benchmark (Alpine vs Debian)..." -ForegroundColor Cyan

# 1. Build Docker Images
Write-Host "`n[1/3] Building Docker Images..." -ForegroundColor Yellow
docker build -f Dockerfile.aot -t skugga-aot-alpine .
docker build -f Dockerfile.aot-debian -t skugga-aot-debian .

# 2. Benchmark AOT on Alpine Startup Time
Write-Host "`n[2/3] Benchmarking AOT on Alpine Startup Time..." -ForegroundColor Yellow
$alpineStartupTime = Measure-Command {
    $containerId = docker run -d -p 8080:8080 skugga-aot-alpine
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
        Write-Host "   AOT on Alpine Startup Time: $startupTime ms" -ForegroundColor Green
    }
    finally {
        docker stop $containerId | Out-Null
        docker rm $containerId | Out-Null
    }
}

# 3. Benchmark AOT on Debian Startup Time
Write-Host "`n[3/3] Benchmarking AOT on Debian Startup Time..." -ForegroundColor Yellow
$debianStartupTime = Measure-Command {
    $containerId = docker run -d -p 8080:8080 skugga-aot-debian
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
        Write-Host "   AOT on Debian Startup Time: $startupTime ms" -ForegroundColor Green
    }
    finally {
        docker stop $containerId | Out-Null
        docker rm $containerId | Out-Null
    }
}

Write-Host "`n✅ Benchmarks Complete." -ForegroundColor Cyan
