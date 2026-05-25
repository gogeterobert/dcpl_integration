# Benchmark script using hyperfine to compare performance with and without RuleKeeper middleware

Write-Host "=== RuleKeeper Performance Benchmark (Hyperfine) ===" -ForegroundColor Cyan
Write-Host ""

# Check if hyperfine is installed
if (-not (Get-Command hyperfine -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: hyperfine is not installed!" -ForegroundColor Red
    Write-Host "Please install it from: https://github.com/sharkdp/hyperfine" -ForegroundColor Yellow
    Write-Host "Or use: winget install sharkdp.hyperfine" -ForegroundColor Yellow
    exit 1
}

$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$resultsDir = "benchmark_results_$timestamp"
New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null

Write-Host "Results will be saved to: $resultsDir" -ForegroundColor Gray
Write-Host ""

# =============================================================================
# Run benchmark WITH middleware
# =============================================================================

Write-Host "=== Starting services WITH middleware ===" -ForegroundColor Cyan

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

# Run hyperfine benchmarks WITH middleware
Write-Host "`n--- Running hyperfine benchmarks WITH middleware ---" -ForegroundColor Yellow
Write-Host ""

Write-Host "Benchmarking GET /tickets/schedules..." -ForegroundColor Gray
cmd /c "hyperfine --warmup 3 --runs 10 --export-json `"$resultsDir/with_middleware_schedules.json`" `"curl -s -H \`"X-User: testuser\`" http://localhost:3000/tickets/schedules`""

Write-Host "`nBenchmarking POST /tickets/buy_ticket..." -ForegroundColor Gray
cmd /c "hyperfine --warmup 3 --runs 10 --export-json `"$resultsDir/with_middleware_buy_ticket.json`" `"curl -s -X POST -H \`"X-User: testuser\`" -H \`"Content-Type: application/json\`" -d \`"{\\\`"name\\\`":\\\`"Test User\\\`",\\\`"e_mail\\\`":\\\`"testuser@example.com\\\`",\\\`"credit_card\\\`":1234567890123456,\\\`"destination\\\`":\\\`"Paris\\\`",\\\`"schedule\\\`":\\\`"2025-12-15T00:00:00.000Z\\\`"}\`" http://localhost:3000/tickets/buy_ticket`""

Write-Host "`nBenchmarking POST /newsletter/subscribe..." -ForegroundColor Gray
cmd /c "hyperfine --warmup 3 --runs 10 --export-json `"$resultsDir/with_middleware_subscribe.json`" `"curl -s -X POST -H \`"X-User: testuser\`" -H \`"Content-Type: application/json\`" -d \`"{\\\`"e_mail\\\`":\\\`"testuser@example.com\\\`"}\`" http://localhost:3000/newsletter/subscribe`""

Write-Host "`nBenchmarking GET /tickets/purchase_history..." -ForegroundColor Gray
cmd /c "hyperfine --warmup 3 --runs 10 --export-json `"$resultsDir/with_middleware_purchase_history.json`" `"curl -s -H \`"X-User: testuser\`" \`"http://localhost:3000/tickets/purchase_history?name=Test%%20User\`"`""

# Stop services
Write-Host "`n`nStopping services..." -ForegroundColor Yellow
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

# Run hyperfine benchmarks WITHOUT middleware
Write-Host "`n--- Running hyperfine benchmarks WITHOUT middleware ---" -ForegroundColor Yellow
Write-Host ""

Write-Host "Benchmarking GET /tickets/schedules..." -ForegroundColor Gray
cmd /c "hyperfine --warmup 3 --runs 10 --export-json `"$resultsDir/without_middleware_schedules.json`" `"curl -s -H \`"X-User: testuser\`" http://localhost:3000/tickets/schedules`""

Write-Host "`nBenchmarking POST /tickets/buy_ticket..." -ForegroundColor Gray
cmd /c "hyperfine --warmup 3 --runs 10 --export-json `"$resultsDir/without_middleware_buy_ticket.json`" `"curl -s -X POST -H \`"X-User: testuser\`" -H \`"Content-Type: application/json\`" -d \`"{\\\`"name\\\`":\\\`"Test User\\\`",\\\`"e_mail\\\`":\\\`"testuser@example.com\\\`",\\\`"credit_card\\\`":1234567890123456,\\\`"destination\\\`":\\\`"Paris\\\`",\\\`"schedule\\\`":\\\`"2025-12-15T00:00:00.000Z\\\`"}\`" http://localhost:3000/tickets/buy_ticket`""

Write-Host "`nBenchmarking POST /newsletter/subscribe..." -ForegroundColor Gray
cmd /c "hyperfine --warmup 3 --runs 10 --export-json `"$resultsDir/without_middleware_subscribe.json`" `"curl -s -X POST -H \`"X-User: testuser\`" -H \`"Content-Type: application/json\`" -d \`"{\\\`"e_mail\\\`":\\\`"testuser@example.com\\\`"}\`" http://localhost:3000/newsletter/subscribe`""

Write-Host "`nBenchmarking GET /tickets/purchase_history..." -ForegroundColor Gray
cmd /c "hyperfine --warmup 3 --runs 10 --export-json `"$resultsDir/without_middleware_purchase_history.json`" `"curl -s -H \`"X-User: testuser\`" \`"http://localhost:3000/tickets/purchase_history?name=Test%%20User\`"`""

# Stop service
Write-Host "`n`nStopping service..." -ForegroundColor Yellow
Stop-Job -Id $webusWithoutJob.Id -ErrorAction SilentlyContinue
Remove-Job -Id $webusWithoutJob.Id -ErrorAction SilentlyContinue

# =============================================================================
# Parse and display results
# =============================================================================

Write-Host "`n`n=== BENCHMARK RESULTS ===" -ForegroundColor Cyan
Write-Host ""

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
    @{Name="GET /tickets/schedules"; File="schedules"; Display="SeeSchedules"},
    @{Name="POST /tickets/buy_ticket"; File="buy_ticket"; Display="BuyTicket"},
    @{Name="POST /newsletter/subscribe"; File="subscribe"; Display="Subscribe"},
    @{Name="GET /tickets/purchase_history"; File="purchase_history"; Display="SeePurchaseHistory"}
)

# Create CSV output
$csvContent = @()
$csvContent += '"Endpoint","MeanMs","MinMs","MaxMs","MedianMs","StdDevMs","BaselineMeanMs","OverheadMs","OverheadPercent"'

foreach ($endpoint in $endpoints) {
    Write-Host "--- $($endpoint.Name) ---" -ForegroundColor Yellow
    
    $withStats = Parse-HyperfineResult -JsonPath "$resultsDir/with_middleware_$($endpoint.File).json"
    $withoutStats = Parse-HyperfineResult -JsonPath "$resultsDir/without_middleware_$($endpoint.File).json"
    
    if ($withStats -and $withoutStats) {
        Write-Host "  WITH middleware:" -ForegroundColor Green
        Write-Host "    Mean: $($withStats.Mean) ms ± $($withStats.StdDev) ms" -ForegroundColor Gray
        Write-Host "    Min: $($withStats.Min) ms | Max: $($withStats.Max) ms | Median: $($withStats.Median) ms" -ForegroundColor Gray
        
        Write-Host "  WITHOUT middleware:" -ForegroundColor Magenta
        Write-Host "    Mean: $($withoutStats.Mean) ms ± $($withoutStats.StdDev) ms" -ForegroundColor Gray
        Write-Host "    Min: $($withoutStats.Min) ms | Max: $($withoutStats.Max) ms | Median: $($withoutStats.Median) ms" -ForegroundColor Gray
        
        $overheadMs = [Math]::Round($withStats.Mean - $withoutStats.Mean, 2)
        $overheadPercent = [Math]::Round((($withStats.Mean - $withoutStats.Mean) / $withoutStats.Mean) * 100, 2)
        
        Write-Host "  Overhead: $overheadMs ms ($overheadPercent%)" -ForegroundColor Cyan
        
        # Add to CSV
        $csvContent += "`"$($endpoint.Display)`",`"$($withStats.Mean)`",`"$($withStats.Min)`",`"$($withStats.Max)`",`"$($withStats.Median)`",`"$($withStats.StdDev)`",`"$($withoutStats.Mean)`",`"$overheadMs`",`"$overheadPercent`""
    }
    
    Write-Host ""
}

# Save CSV
$csvFile = "$resultsDir/summary.csv"
$csvContent | Out-File -FilePath $csvFile -Encoding UTF8

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "All hyperfine JSON results saved to: $resultsDir" -ForegroundColor Green
Write-Host "CSV summary saved to: $csvFile" -ForegroundColor Green
Write-Host "`nBenchmark complete!" -ForegroundColor Green
