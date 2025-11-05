# Benchmark script for .NET compiled solution with DCPL middleware

Write-Host "=== DCPL .NET Solution Performance Benchmark ===" -ForegroundColor Cyan
Write-Host ""

$iterations = 10
$solutionPath = "..\compiled_solution\benchmark\src\Web"
$baseUrl = "http://localhost:5000"
$baselineMs = 20  # Simulated request logic delay

$results = @{
    "SeeSchedules" = @()
    "BuyTicket" = @()
    "Subscribe" = @()
    "SeePurchaseHistory" = @()
}

function Calculate-Stats {
    param([array]$Values)
    
    if ($Values.Count -eq 0) {
        return @{
            "Min" = 0
            "Max" = 0
            "Avg" = 0
            "Median" = 0
        }
    }
    
    $sorted = $Values | Sort-Object
    $median = if ($sorted.Count % 2 -eq 0) {
        ($sorted[$sorted.Count / 2 - 1] + $sorted[$sorted.Count / 2]) / 2
    } else {
        $sorted[[Math]::Floor($sorted.Count / 2)]
    }
    
    return @{
        "Min" = ($Values | Measure-Object -Minimum).Minimum
        "Max" = ($Values | Measure-Object -Maximum).Maximum
        "Avg" = [Math]::Round(($Values | Measure-Object -Average).Average, 2)
        "Median" = $median
    }
}

# =============================================================================
# Start .NET Solution
# =============================================================================

Write-Host "=== Starting .NET Solution ===" -ForegroundColor Cyan

$fullSolutionPath = Join-Path (Get-Location).Path $solutionPath
Write-Host "Solution path: $fullSolutionPath" -ForegroundColor Gray

# Check if solution exists
if (-not (Test-Path $fullSolutionPath)) {
    Write-Host "[ERROR] Solution path not found: $fullSolutionPath" -ForegroundColor Red
    exit 1
}

Write-Host "Starting .NET application..." -ForegroundColor Yellow

# Start the .NET application
$dotnetJob = Start-Job -ScriptBlock {
    param($path)
    Set-Location $path
    dotnet run --urls="http://localhost:5000" 2>&1
} -ArgumentList $fullSolutionPath

Write-Host "Waiting for application to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# Test if application is responding
try {
    $healthCheck = Invoke-WebRequest -Uri "$baseUrl/api/specification.json" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
    Write-Host "Application is ready! (Status: $($healthCheck.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "[WARNING] Could not verify application readiness: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "[WARNING] Proceeding anyway..." -ForegroundColor Yellow
}

# =============================================================================
# Run Benchmark
# =============================================================================

Write-Host "`n--- Running $iterations iterations ---" -ForegroundColor Yellow
Write-Host ""

$createdUserName = $null

for ($i = 1; $i -le $iterations; $i++) {
    Write-Host "Iteration $i/$iterations..." -ForegroundColor Gray
    
    # Create WebusUser entity (not tracked in benchmark)
    try {
        $userData = @{
            name = "TestUser$i"
        } | ConvertTo-Json
        
        $response = Invoke-WebRequest -Uri "$baseUrl/api/WebusUser/create" `
            -Method POST `
            -Headers @{"Content-Type"="application/json"} `
            -Body $userData `
            -UseBasicParsing `
            -TimeoutSec 30 `
            -ErrorAction Stop
        $createdUserName = "TestUser$i"
        Write-Host "  [SETUP] Created WebusUser: $createdUserName" -ForegroundColor Gray
    } catch {
        Write-Host "  [ERROR] Create WebusUser failed: $($_.Exception.Message)" -ForegroundColor Red
        continue
    }
    
    Start-Sleep -Milliseconds 200
    
    # Test 2: See Schedules
    try {
        $schedulesData = @{
            name = $createdUserName
        } | ConvertTo-Json
        
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $response = Invoke-WebRequest -Uri "$baseUrl/api/WebusUser/SeeSchedules" `
            -Method POST `
            -Headers @{"Content-Type"="application/json"} `
            -Body $schedulesData `
            -UseBasicParsing `
            -TimeoutSec 30
        $stopwatch.Stop()
        $results["SeeSchedules"] += $stopwatch.ElapsedMilliseconds
        Write-Host "  [OK] See Schedules: $($stopwatch.ElapsedMilliseconds)ms" -ForegroundColor Green
    } catch {
        Write-Host "  [ERROR] See Schedules failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Start-Sleep -Milliseconds 200
    
    # Test 3: Buy Ticket
    try {
        $ticketData = @{
            name = $createdUserName
        } | ConvertTo-Json
        
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $response = Invoke-WebRequest -Uri "$baseUrl/api/WebusUser/Buyticket" `
            -Method POST `
            -Headers @{"Content-Type"="application/json"} `
            -Body $ticketData `
            -UseBasicParsing `
            -TimeoutSec 30
        $stopwatch.Stop()
        $results["BuyTicket"] += $stopwatch.ElapsedMilliseconds
        Write-Host "  [OK] Buy Ticket: $($stopwatch.ElapsedMilliseconds)ms" -ForegroundColor Green
    } catch {
        Write-Host "  [ERROR] Buy Ticket failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Start-Sleep -Milliseconds 200
    
    # Test 4: Subscribe
    try {
        $subscribeData = @{
            name = $createdUserName
        } | ConvertTo-Json
        
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $response = Invoke-WebRequest -Uri "$baseUrl/api/WebusUser/Subscribe" `
            -Method POST `
            -Headers @{"Content-Type"="application/json"} `
            -Body $subscribeData `
            -UseBasicParsing `
            -TimeoutSec 30
        $stopwatch.Stop()
        $results["Subscribe"] += $stopwatch.ElapsedMilliseconds
        Write-Host "  [OK] Subscribe: $($stopwatch.ElapsedMilliseconds)ms" -ForegroundColor Green
    } catch {
        Write-Host "  [ERROR] Subscribe failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Start-Sleep -Milliseconds 200
    
    # Test 5: See Purchase History
    try {
        $historyData = @{
            name = $createdUserName
        } | ConvertTo-Json
        
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $response = Invoke-WebRequest -Uri "$baseUrl/api/WebusUser/SeePurchaseHistory" `
            -Method POST `
            -Headers @{"Content-Type"="application/json"} `
            -Body $historyData `
            -UseBasicParsing `
            -TimeoutSec 30
        $stopwatch.Stop()
        $results["SeePurchaseHistory"] += $stopwatch.ElapsedMilliseconds
        Write-Host "  [OK] See Purchase History: $($stopwatch.ElapsedMilliseconds)ms" -ForegroundColor Green
    } catch {
        Write-Host "  [ERROR] See Purchase History failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Delay between full iterations
    Start-Sleep -Milliseconds 500
}

# =============================================================================
# Stop .NET Application
# =============================================================================

Write-Host "`nStopping .NET application..." -ForegroundColor Yellow
Stop-Job -Id $dotnetJob.Id -ErrorAction SilentlyContinue
Remove-Job -Id $dotnetJob.Id -ErrorAction SilentlyContinue

# =============================================================================
# Display Results
# =============================================================================

Write-Host "`n`n=== BENCHMARK RESULTS ===" -ForegroundColor Cyan
Write-Host "Iterations per test: $iterations" -ForegroundColor Gray
Write-Host "Baseline (simulated logic): $baselineMs ms`n" -ForegroundColor Gray

$testNames = @("SeeSchedules", "BuyTicket", "Subscribe", "SeePurchaseHistory")

foreach ($testName in $testNames) {
    Write-Host "--- $testName ---" -ForegroundColor Yellow
    
    $stats = Calculate-Stats -Values $results[$testName]
    
    if ($stats.Avg -eq 0) {
        Write-Host "  No successful requests" -ForegroundColor Red
    } else {
        Write-Host "  Min: $($stats.Min) ms | Max: $($stats.Max) ms | Avg: $($stats.Avg) ms | Median: $($stats.Median) ms" -ForegroundColor Green
        
        # Calculate overhead over baseline
        $overheadMs = [Math]::Round($stats.Avg - $baselineMs, 2)
        $overheadPercent = if ($baselineMs -gt 0) { [Math]::Round(($overheadMs / $baselineMs) * 100, 2) } else { 0 }
        
        if ($overheadMs -ge 0) {
            Write-Host "  Overhead: +$overheadMs ms (+$overheadPercent% over ${baselineMs}ms baseline)" -ForegroundColor Cyan
        } else {
            Write-Host "  Overhead: $overheadMs ms ($overheadPercent% over ${baselineMs}ms baseline)" -ForegroundColor Cyan
        }
    }
    
    Write-Host ""
}

# Overall statistics
Write-Host "`n=== OVERALL STATISTICS ===" -ForegroundColor Cyan

$allTimes = @()

foreach ($testName in $testNames) {
    $allTimes += $results[$testName]
}

$overallStats = Calculate-Stats -Values $allTimes

Write-Host "All requests (4 operations):" -ForegroundColor Green
Write-Host "  Avg: $($overallStats.Avg) ms | Median: $($overallStats.Median) ms"
Write-Host "  Min: $($overallStats.Min) ms | Max: $($overallStats.Max) ms"

# Calculate overall overhead
$overallOverheadMs = [Math]::Round($overallStats.Avg - $baselineMs, 2)
$overallOverheadPercent = if ($baselineMs -gt 0) { [Math]::Round(($overallOverheadMs / $baselineMs) * 100, 2) } else { 0 }

Write-Host "`nOverall Overhead:" -ForegroundColor Cyan
Write-Host "  +$overallOverheadMs ms (+$overallOverheadPercent% over ${baselineMs}ms baseline)" -ForegroundColor Cyan

$totalRequests = $allTimes.Count
$successfulRequests = ($allTimes | Where-Object { $_ -gt 0 }).Count
$successRate = if ($totalRequests -gt 0) { [Math]::Round(($successfulRequests / $totalRequests) * 100, 2) } else { 0 }

Write-Host "`nSuccess Rate: $successfulRequests / $totalRequests ($successRate%)" -ForegroundColor Cyan

Write-Host "`nBenchmark complete!" -ForegroundColor Green
