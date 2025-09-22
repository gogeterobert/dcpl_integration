# E2E Test Runner for DOI Application
# This script starts the application, waits for it to be ready, runs tests, and cleans up

param(
    [int]$HealthCheckTimeout = 60,
    [int]$HealthCheckInterval = 2,
    [string]$AppPort = "5000",
    [string]$AppUrl = "http://localhost:$AppPort",
    [string]$HealthEndpoint = "/health"
)

# Colors for output
$Red = [System.ConsoleColor]::Red
$Green = [System.ConsoleColor]::Green
$Yellow = [System.ConsoleColor]::Yellow
$Blue = [System.ConsoleColor]::Blue
$Cyan = [System.ConsoleColor]::Cyan

function Write-ColoredOutput {
    param([string]$Message, [System.ConsoleColor]$Color = [System.ConsoleColor]::White)
    Write-Host $Message -ForegroundColor $Color
}

function Write-Step {
    param([string]$Message)
    Write-ColoredOutput "[STEP] $Message" $Blue
}

function Write-Success {
    param([string]$Message)
    Write-ColoredOutput "[SUCCESS] $Message" $Green
}

function Write-Error {
    param([string]$Message)
    Write-ColoredOutput "[ERROR] $Message" $Red
}

function Write-Warning {
    param([string]$Message)
    Write-ColoredOutput "[WARNING] $Message" $Yellow
}

function Write-Info {
    param([string]$Message)
    Write-ColoredOutput "[INFO] $Message" $Cyan
}

# Global variables
$AppProcess = $null
$OriginalLocation = Get-Location

function Start-Application {
    Write-Step "Starting DOI Web Application..."
    
    # Navigate to the Web project directory
    $WebProjectPath = "..\..\src\Web"
    if (-not (Test-Path $WebProjectPath)) {
        Write-Error "Web project not found at $WebProjectPath"
        return $false
    }
    
    Set-Location $WebProjectPath
    
    try {
        # Start the application in background
        $Global:AppProcess = Start-Process -FilePath "dotnet" -ArgumentList "run" -NoNewWindow -PassThru -RedirectStandardOutput "app.log" -RedirectStandardError "app-error.log"
        
        if ($Global:AppProcess) {
            Write-Success "Application started with PID: $($Global:AppProcess.Id)"
            return $true
        } else {
            Write-Error "Failed to start application"
            return $false
        }
    }
    catch {
        Write-Error "Error starting application: $_"
        return $false
    }
    finally {
        Set-Location $OriginalLocation
    }
}

function Wait-ForApplicationReady {
    Write-Step "Waiting for application to be ready..."
    
    $healthUrl = "$AppUrl$HealthEndpoint"
    $startTime = Get-Date
    $timeout = $startTime.AddSeconds($HealthCheckTimeout)
    
    Write-Info "Health check URL: $healthUrl"
    Write-Info "Timeout: $HealthCheckTimeout seconds"
    
    while ((Get-Date) -lt $timeout) {
        try {
            # Check if process is still running
            if ($Global:AppProcess -and $Global:AppProcess.HasExited) {
                Write-Error "Application process has exited unexpectedly"
                return $false
            }
            
            # Try to connect to health endpoint
            $response = Invoke-WebRequest -Uri $healthUrl -Method GET -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
            
            if ($response.StatusCode -eq 200) {
                $elapsed = ((Get-Date) - $startTime).TotalSeconds
                Write-Success "Application is ready! (took $([math]::Round($elapsed, 1)) seconds)"
                return $true
            }
        }
        catch {
            # Continue waiting
            Write-Host "." -NoNewline -ForegroundColor Gray
        }
        
        Start-Sleep -Seconds $HealthCheckInterval
    }
    
    Write-Error "Application failed to become ready within $HealthCheckTimeout seconds"
    return $false
}

function Run-E2ETests {
    Write-Step "Running E2E tests..."
    
    try {
        # Build TypeScript first
        Write-Info "Building TypeScript..."
        $buildResult = & npm run build
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "TypeScript build had issues, but continuing..."
        }
        
        # Run Cucumber tests
        Write-Info "Executing Cucumber tests..."
        $testResult = & npm run test:cucumber
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "All E2E tests passed!"
            return $true
        } else {
            Write-Error "Some E2E tests failed"
            return $false
        }
    }
    catch {
        Write-Error "Error running tests: $_"
        return $false
    }
}

function Stop-Application {
    Write-Step "Stopping application..."
    
    if ($Global:AppProcess -and -not $Global:AppProcess.HasExited) {
        try {
            # Try graceful shutdown first
            $Global:AppProcess.CloseMainWindow()
            
            # Wait a few seconds for graceful shutdown
            if (-not $Global:AppProcess.WaitForExit(5000)) {
                # Force kill if graceful shutdown failed
                Write-Warning "Graceful shutdown timed out, forcing termination..."
                $Global:AppProcess.Kill()
                $Global:AppProcess.WaitForExit(3000)
            }
            
            Write-Success "Application stopped successfully"
        }
        catch {
            Write-Warning "Error stopping application: $_"
            try {
                # Last resort: kill by PID
                Stop-Process -Id $Global:AppProcess.Id -Force -ErrorAction SilentlyContinue
            }
            catch {
                # Ignore errors here
            }
        }
    }
    
    # Clean up any remaining dotnet processes on our port
    try {
        $processes = Get-NetTCPConnection -LocalPort $AppPort -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess
        foreach ($pid in $processes) {
            $process = Get-Process -Id $pid -ErrorAction SilentlyContinue
            if ($process -and $process.ProcessName -eq "dotnet") {
                Write-Info "Cleaning up remaining dotnet process: $pid"
                Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
            }
        }
    }
    catch {
        # Ignore cleanup errors
    }
}

function Main {
    Write-ColoredOutput "DOI E2E Test Runner" $Cyan
    Write-ColoredOutput "===================" $Cyan
    
    $success = $false
    
    try {
        # Start the application
        if (-not (Start-Application)) {
            return 1
        }
        
        # Wait for it to be ready
        if (-not (Wait-ForApplicationReady)) {
            return 1
        }
        
        # Run the tests
        $success = Run-E2ETests
        
    }
    finally {
        # Always clean up
        Stop-Application
        Set-Location $OriginalLocation
    }
    
    if ($success) {
        Write-Success "E2E tests completed successfully!"
        return 0
    } else {
        Write-Error "E2E tests failed"
        return 1
    }
}

# Handle Ctrl+C gracefully
Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action {
    Stop-Application
}

# Run the main function
exit (Main)