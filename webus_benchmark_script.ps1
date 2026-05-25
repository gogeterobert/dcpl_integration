# Benchmark script to compare performance with and without RuleKeeper middleware

Write-Host "=== RuleKeeper Performance Benchmark ===" -ForegroundColor Cyan
Write-Host ""

$iterations = 10
$results = @{
    "WithMiddleware" = @{}
    "WithoutMiddleware" = @{}
}

function Run-Benchmark {
    param(
        [string]$Mode,
        [string]$ManagerJobId,
        [string]$WebusJobId
    )
    
    Write-Host "`n--- Running $iterations iterations in $Mode mode ---" -ForegroundColor Yellow
    
    $testResults = @{
        "GetSchedules" = @()
        "BuyTicket" = @()
        "Subscribe" = @()
        "PurchaseHistory" = @()
    }
    
    for ($i = 1; $i -le $iterations; $i++) {
        Write-Host "  Iteration $i/$iterations..." -ForegroundColor Gray
        
        # Test 1: GET schedules
        try {
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            $response = Invoke-WebRequest -Uri "http://localhost:3000/tickets/schedules" -Headers @{"X-User"="testuser"} -UseBasicParsing
            $stopwatch.Stop()
            $testResults["GetSchedules"] += $stopwatch.ElapsedMilliseconds
        } catch {
            Write-Host "    [ERROR] Get schedules failed: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        # Test 2: POST buy ticket
        try {
            $ticketData = @{
                name = "Test User $i"
                e_mail = "testuser$i@example.com"
                credit_card = 1234567890123456
                destination = "Paris"
                schedule = "2025-12-15T00:00:00.000Z"
            } | ConvertTo-Json
            
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            $response = Invoke-WebRequest -Uri "http://localhost:3000/tickets/buy_ticket" -Method POST -Headers @{"X-User"="testuser"; "Content-Type"="application/json"} -Body $ticketData -UseBasicParsing
            $stopwatch.Stop()
            $testResults["BuyTicket"] += $stopwatch.ElapsedMilliseconds
        } catch {
            Write-Host "    [ERROR] Buy ticket failed: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        # Test 3: POST subscribe
        try {
            $newsletterData = @{
                e_mail = "testuser$i@example.com"
            } | ConvertTo-Json
            
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            $response = Invoke-WebRequest -Uri "http://localhost:3000/newsletter/subscribe" -Method POST -Headers @{"X-User"="testuser"; "Content-Type"="application/json"} -Body $newsletterData -UseBasicParsing
            $stopwatch.Stop()
            $testResults["Subscribe"] += $stopwatch.ElapsedMilliseconds
        } catch {
            Write-Host "    [ERROR] Subscribe failed: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        # Test 4: GET purchase history
        try {
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            $response = Invoke-WebRequest -Uri "http://localhost:3000/tickets/purchase_history?name=Test%20User" -Headers @{"X-User"="testuser"} -UseBasicParsing
            $stopwatch.Stop()
            $testResults["PurchaseHistory"] += $stopwatch.ElapsedMilliseconds
        } catch {
            Write-Host "    [ERROR] Purchase history failed: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        # Small delay between iterations
        Start-Sleep -Milliseconds 100
    }
    
    return $testResults
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
# Run benchmark WITH middleware
# =============================================================================

Write-Host "`n=== Starting services WITH middleware ===" -ForegroundColor Cyan

# Start RuleKeeper Manager
Write-Host "`nStarting RuleKeeper Manager..." -ForegroundColor Yellow
$managerPath = "RuleKeeper Manager"
$fullManagerPath = Join-Path (Get-Location).Path $managerPath
$managerJob = Start-Job -ScriptBlock {
    param($path)
    Set-Location $path
    $env:PATH = "$path;$env:PATH"
    npm start 2>&1 | Out-Null
} -ArgumentList $fullManagerPath

Start-Sleep -Seconds 10

# Start Webus WITH middleware
Write-Host "Starting Webus WITH middleware..." -ForegroundColor Yellow
$webusPath = "Use Cases\webus"
$fullWebusPath = Join-Path (Get-Location).Path $webusPath
$webusWithJob = Start-Job -ScriptBlock {
    param($path)
    Set-Location $path
    $env:ENABLE_RULEKEEPER = "true"
    npm start 2>&1 | Out-Null
} -ArgumentList $fullWebusPath

Write-Host "Waiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# Run benchmark
$results["WithMiddleware"] = Run-Benchmark -Mode "WITH middleware" -ManagerJobId $managerJob.Id -WebusJobId $webusWithJob.Id

# Stop services
Write-Host "`nStopping services..." -ForegroundColor Yellow
Stop-Job -Id $webusWithJob.Id -ErrorAction SilentlyContinue
Stop-Job -Id $managerJob.Id -ErrorAction SilentlyContinue
Remove-Job -Id $webusWithJob.Id -ErrorAction SilentlyContinue
Remove-Job -Id $managerJob.Id -ErrorAction SilentlyContinue

Start-Sleep -Seconds 3

# =============================================================================
# Run benchmark WITHOUT middleware
# =============================================================================

Write-Host "`n=== Starting services WITHOUT middleware ===" -ForegroundColor Cyan

# Start Webus WITHOUT middleware (no Manager needed)
Write-Host "`nStarting Webus WITHOUT middleware..." -ForegroundColor Yellow
$webusWithoutJob = Start-Job -ScriptBlock {
    param($path)
    Set-Location $path
    $env:ENABLE_RULEKEEPER = "false"
    npm start 2>&1 | Out-Null
} -ArgumentList $fullWebusPath

Write-Host "Waiting for service to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Run benchmark
$results["WithoutMiddleware"] = Run-Benchmark -Mode "WITHOUT middleware" -WebusJobId $webusWithoutJob.Id

# Stop service
Write-Host "`nStopping service..." -ForegroundColor Yellow
Stop-Job -Id $webusWithoutJob.Id -ErrorAction SilentlyContinue
Remove-Job -Id $webusWithoutJob.Id -ErrorAction SilentlyContinue

# =============================================================================
# Display Results
# =============================================================================

Write-Host "`n`n=== BENCHMARK RESULTS ===" -ForegroundColor Cyan
Write-Host "Iterations per test: $iterations`n" -ForegroundColor Gray

$testNames = @("GetSchedules", "BuyTicket", "Subscribe", "PurchaseHistory")

foreach ($testName in $testNames) {
    Write-Host "--- $testName ---" -ForegroundColor Yellow
    
    $withStats = Calculate-Stats -Values $results["WithMiddleware"][$testName]
    $withoutStats = Calculate-Stats -Values $results["WithoutMiddleware"][$testName]
    
    Write-Host "  WITH middleware:" -ForegroundColor Green
    Write-Host "    Min: $($withStats.Min) ms | Max: $($withStats.Max) ms | Avg: $($withStats.Avg) ms | Median: $($withStats.Median) ms"
    
    Write-Host "  WITHOUT middleware:" -ForegroundColor Magenta
    Write-Host "    Min: $($withoutStats.Min) ms | Max: $($withoutStats.Max) ms | Avg: $($withoutStats.Avg) ms | Median: $($withoutStats.Median) ms"
    
    if ($withoutStats.Avg -gt 0) {
        $overhead = [Math]::Round((($withStats.Avg - $withoutStats.Avg) / $withoutStats.Avg) * 100, 2)
        $overheadMs = [Math]::Round($withStats.Avg - $withoutStats.Avg, 2)
        Write-Host "  Overhead: +$overheadMs ms (+$overhead%)" -ForegroundColor Cyan
    }
    
    Write-Host ""
}

# Overall statistics
Write-Host "`n=== OVERALL STATISTICS ===" -ForegroundColor Cyan

$allWithTimes = @()
$allWithoutTimes = @()

foreach ($testName in $testNames) {
    $allWithTimes += $results["WithMiddleware"][$testName]
    $allWithoutTimes += $results["WithoutMiddleware"][$testName]
}

$overallWithStats = Calculate-Stats -Values $allWithTimes
$overallWithoutStats = Calculate-Stats -Values $allWithoutTimes

Write-Host "WITH middleware (all requests):" -ForegroundColor Green
Write-Host "  Avg: $($overallWithStats.Avg) ms | Median: $($overallWithStats.Median) ms"

Write-Host "WITHOUT middleware (all requests):" -ForegroundColor Magenta
Write-Host "  Avg: $($overallWithoutStats.Avg) ms | Median: $($overallWithoutStats.Median) ms"

if ($overallWithoutStats.Avg -gt 0) {
    $totalOverhead = [Math]::Round((($overallWithStats.Avg - $overallWithoutStats.Avg) / $overallWithoutStats.Avg) * 100, 2)
    $totalOverheadMs = [Math]::Round($overallWithStats.Avg - $overallWithoutStats.Avg, 2)
    Write-Host "Total Overhead: +$totalOverheadMs ms (+$totalOverhead%)" -ForegroundColor Cyan
}

Write-Host "`nBenchmark complete!" -ForegroundColor Green
