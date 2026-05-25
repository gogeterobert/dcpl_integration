# Benchmark script using hyperfine for .NET compiled solution with DCPL middleware

Write-Host "=== DCPL .NET Solution Performance Benchmark (Hyperfine) ===" -ForegroundColor Cyan
Write-Host ""

# Check if hyperfine is installed
if (-not (Get-Command hyperfine -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: hyperfine is not installed!" -ForegroundColor Red
    Write-Host "Please install it from: https://github.com/sharkdp/hyperfine" -ForegroundColor Yellow
    Write-Host "Or use: winget install sharkdp.hyperfine" -ForegroundColor Yellow
    exit 1
}

$solutionPath = "..\compiled_solution\benchmark\src\Web"
$baseUrl = "http://localhost:5000"
$baselineMs = 40  # Simulated request logic delay

$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$resultsDir = "benchmark_results_dotnet_$timestamp"
New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null

Write-Host "Results will be saved to: $resultsDir" -ForegroundColor Gray
Write-Host ""

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

# Test if application is responding with retries
$maxRetries = 10
$retryCount = 0
$isReady = $false

while (-not $isReady -and $retryCount -lt $maxRetries) {
    try {
        $healthCheck = Invoke-WebRequest -Uri "$baseUrl/api/specification.json" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        Write-Host "Application is ready! (Status: $($healthCheck.StatusCode))" -ForegroundColor Green
        $isReady = $true
    } catch {
        $retryCount++
        if ($retryCount -lt $maxRetries) {
            Write-Host "Waiting for application... (attempt $retryCount/$maxRetries)" -ForegroundColor Gray
            Start-Sleep -Seconds 3
        } else {
            Write-Host "[ERROR] Application did not become ready after $maxRetries attempts" -ForegroundColor Red
            Write-Host "Stopping job and exiting..." -ForegroundColor Red
            Stop-Job -Id $dotnetJob.Id -ErrorAction SilentlyContinue
            Remove-Job -Id $dotnetJob.Id -ErrorAction SilentlyContinue
            exit 1
        }
    }
}

# Create test users for hyperfine
Write-Host "`nCreating test users..." -ForegroundColor Yellow
$testUserCount = 15  # More than warmup + runs to avoid issues

for ($i = 1; $i -le $testUserCount; $i++) {
    try {
        $userData = @{
            name = "HyperfineUser$i"
        } | ConvertTo-Json
        
        $response = Invoke-WebRequest -Uri "$baseUrl/api/WebusUser/create" `
            -Method POST `
            -Headers @{"Content-Type"="application/json"} `
            -Body $userData `
            -UseBasicParsing `
            -TimeoutSec 30 `
            -ErrorAction Stop
    } catch {
        Write-Host "  [WARNING] Failed to create user $i" -ForegroundColor Yellow
    }
}

Write-Host "Test users created successfully" -ForegroundColor Green
Start-Sleep -Seconds 2

# =============================================================================
# Run hyperfine benchmarks
# =============================================================================

Write-Host "`n--- Running hyperfine benchmarks ---" -ForegroundColor Yellow
Write-Host ""

# Use a counter for user names
$userIndex = 1

Write-Host "Benchmarking POST /api/WebusUser/SeeSchedules..." -ForegroundColor Gray
cmd /c "hyperfine --warmup 3 --runs 10 --export-json `"$resultsDir/see_schedules.json`" --parameter-list user 1,2,3,4,5,6,7,8,9,10,11,12,13 `"curl -s -X POST -H \`"Content-Type: application/json\`" -d \`"{\\\`"name\\\`":\\\`"HyperfineUser{user}\\\`"}\`" http://localhost:5000/api/WebusUser/SeeSchedules`""

Write-Host "`nBenchmarking POST /api/WebusUser/Buyticket..." -ForegroundColor Gray
cmd /c "hyperfine --warmup 3 --runs 10 --export-json `"$resultsDir/buy_ticket.json`" --parameter-list user 1,2,3,4,5,6,7,8,9,10,11,12,13 `"curl -s -X POST -H \`"Content-Type: application/json\`" -d \`"{\\\`"name\\\`":\\\`"HyperfineUser{user}\\\`"}\`" http://localhost:5000/api/WebusUser/Buyticket`""

Write-Host "`nBenchmarking POST /api/WebusUser/Subscribe..." -ForegroundColor Gray
cmd /c "hyperfine --warmup 3 --runs 10 --export-json `"$resultsDir/subscribe.json`" --parameter-list user 1,2,3,4,5,6,7,8,9,10,11,12,13 `"curl -s -X POST -H \`"Content-Type: application/json\`" -d \`"{\\\`"name\\\`":\\\`"HyperfineUser{user}\\\`"}\`" http://localhost:5000/api/WebusUser/Subscribe`""

Write-Host "`nBenchmarking POST /api/WebusUser/SeePurchaseHistory..." -ForegroundColor Gray
cmd /c "hyperfine --warmup 3 --runs 10 --export-json `"$resultsDir/see_purchase_history.json`" --parameter-list user 1,2,3,4,5,6,7,8,9,10,11,12,13 `"curl -s -X POST -H \`"Content-Type: application/json\`" -d \`"{\\\`"name\\\`":\\\`"HyperfineUser{user}\\\`"}\`" http://localhost:5000/api/WebusUser/SeePurchaseHistory`""

# =============================================================================
# Stop .NET Application
# =============================================================================

Write-Host "`n`nStopping .NET application..." -ForegroundColor Yellow
Stop-Job -Id $dotnetJob.Id -ErrorAction SilentlyContinue
Remove-Job -Id $dotnetJob.Id -ErrorAction SilentlyContinue

# =============================================================================
# Parse and display results
# =============================================================================

Write-Host "`n`n=== BENCHMARK RESULTS ===" -ForegroundColor Cyan
Write-Host "Baseline (simulated logic): $baselineMs ms`n" -ForegroundColor Gray

function Parse-HyperfineResult {
    param(
        [string]$JsonPath
    )
    
    if (-not (Test-Path $JsonPath)) {
        return $null
    }
    
    $data = Get-Content $JsonPath | ConvertFrom-Json
    $result = $data.results[0]
    
    return @{
        Mean = [Math]::Round($result.mean * 1000, 2)  # Convert to ms
        Min = [Math]::Round($result.min * 1000, 2)
        Max = [Math]::Round($result.max * 1000, 2)
        StdDev = [Math]::Round($result.stddev * 1000, 2)
        Median = [Math]::Round($result.median * 1000, 2)
    }
}

$endpoints = @(
    @{Name="POST /api/WebusUser/SeeSchedules"; File="see_schedules"; Display="SeeSchedules"},
    @{Name="POST /api/WebusUser/Buyticket"; File="buy_ticket"; Display="BuyTicket"},
    @{Name="POST /api/WebusUser/Subscribe"; File="subscribe"; Display="Subscribe"},
    @{Name="POST /api/WebusUser/SeePurchaseHistory"; File="see_purchase_history"; Display="SeePurchaseHistory"}
)

# Create CSV output
$csvContent = @()
$csvContent += '"Endpoint","MeanMs","MinMs","MaxMs","MedianMs","StdDevMs","BaselineMs","OverheadMs","OverheadPercent"'

$allMeans = @()

foreach ($endpoint in $endpoints) {
    Write-Host "--- $($endpoint.Name) ---" -ForegroundColor Yellow
    
    $stats = Parse-HyperfineResult -JsonPath "$resultsDir/$($endpoint.File).json"
    
    if ($stats) {
        Write-Host "  Mean: $($stats.Mean) ms ± $($stats.StdDev) ms" -ForegroundColor Green
        Write-Host "  Min: $($stats.Min) ms | Max: $($stats.Max) ms | Median: $($stats.Median) ms" -ForegroundColor Gray
        
        $overheadMs = [Math]::Round($stats.Mean - $baselineMs, 2)
        $overheadPercent = if ($baselineMs -gt 0) { [Math]::Round(($overheadMs / $baselineMs) * 100, 2) } else { 0 }
        
        Write-Host "  Overhead: +$overheadMs ms (+$overheadPercent% over ${baselineMs}ms baseline)" -ForegroundColor Cyan
        
        # Track for overall statistics
        $allMeans += $stats.Mean
        
        # Add to CSV
        $csvContent += "`"$($endpoint.Display)`",`"$($stats.Mean)`",`"$($stats.Min)`",`"$($stats.Max)`",`"$($stats.Median)`",`"$($stats.StdDev)`",`"$baselineMs`",`"$overheadMs`",`"$overheadPercent`""
    } else {
        Write-Host "  [ERROR] Could not parse results" -ForegroundColor Red
    }
    
    Write-Host ""
}

# Calculate overall statistics
if ($allMeans.Count -gt 0) {
    $overallMean = [Math]::Round(($allMeans | Measure-Object -Average).Average, 2)
    $overallMin = [Math]::Round(($allMeans | Measure-Object -Minimum).Minimum, 2)
    $overallMax = [Math]::Round(($allMeans | Measure-Object -Maximum).Maximum, 2)
    
    Write-Host "=== OVERALL STATISTICS ===" -ForegroundColor Cyan
    Write-Host "All requests (4 operations):" -ForegroundColor Green
    Write-Host "  Avg Mean: $overallMean ms" -ForegroundColor Gray
    Write-Host "  Min Mean: $overallMin ms | Max Mean: $overallMax ms" -ForegroundColor Gray
    
    $overallOverheadMs = [Math]::Round($overallMean - $baselineMs, 2)
    $overallOverheadPercent = if ($baselineMs -gt 0) { [Math]::Round(($overallOverheadMs / $baselineMs) * 100, 2) } else { 0 }
    
    Write-Host "`nOverall Overhead:" -ForegroundColor Cyan
    Write-Host "  +$overallOverheadMs ms (+$overallOverheadPercent% over ${baselineMs}ms baseline)" -ForegroundColor Cyan
    
    # Add overall to CSV
    $csvContent += "`"OVERALL`",`"$overallMean`",`"$overallMin`",`"$overallMax`",`"-`",`"-`",`"$baselineMs`",`"$overallOverheadMs`",`"$overallOverheadPercent`""
}

# Save CSV
$csvFile = "$resultsDir/summary.csv"
$csvContent | Out-File -FilePath $csvFile -Encoding UTF8

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "All hyperfine JSON results saved to: $resultsDir" -ForegroundColor Green
Write-Host "CSV summary saved to: $csvFile" -ForegroundColor Green
Write-Host "`nBenchmark complete!" -ForegroundColor Green
