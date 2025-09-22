# DOI E2E Tests

End-to-end tests for the DOI (Data Owner Interface) application using TypeScript, Cucumber, and Gherkin scenarios.

## Overview

This E2E test suite:
- ✅ Starts the .NET DOI Web application automatically
- ✅ Runs Gherkin-based test scenarios against the live API
- ✅ Tests patient registration functionality
- ✅ Validates GDPR violation detection by the ViolationEvaluatorService
- ✅ Cleans up and stops the application after tests complete

## Prerequisites

- Node.js (v18 or higher)
- .NET 9.0 SDK
- The DOI application should be buildable

## Setup

1. Install dependencies:
```bash
cd tests/e2e
npm install
```

2. Build TypeScript files:
```bash
npm run build
```

## Running Tests

### Full E2E Test Suite
```bash
npm run e2e
```
This command will:
1. Start the DOI Web application
2. Wait for it to be ready
3. Run all Cucumber tests
4. Stop the application
5. Clean up

### Run Tests Only (if app is already running)
```bash
npm test
```

### Development/Debug Mode
```bash
npm run test:debug
```

## Test Scenarios

### Patient Registration (`features/patient-registration.feature`)

- ✅ **Successful patient registration** - Tests basic patient registration flow
- ✅ **Validation errors** - Tests registration with missing required fields
- ✅ **Duplicate prevention** - Tests that duplicate patients cannot be registered
- ✅ **Data validation** - Tests various invalid data scenarios
- ✅ **GDPR compliance** - Verifies compliance tracking is set up

### GDPR Violation Detection (`features/violation-detection.feature`)

- ✅ **Background service monitoring** - Validates ViolationEvaluatorService runs periodically
- ✅ **Violation detection** - Tests detection of overdue access request responses
- ✅ **Reactive consequences** - Verifies violation consequences are executed
- ✅ **Database entity creation** - Checks that D3violated entities are created
- ✅ **Application stability** - Ensures app remains stable during violation processing

## Architecture

```
tests/e2e/
├── features/                    # Gherkin feature files
│   ├── patient-registration.feature
│   └── violation-detection.feature
├── step-definitions/           # Cucumber step implementations
│   ├── patient-steps.ts
│   └── violation-steps.ts
├── src/                       # Core TypeScript classes
│   ├── ApplicationManager.ts  # Manages .NET app lifecycle
│   └── ApiClient.ts          # HTTP API client
├── scripts/                   # Utility scripts
│   └── run-e2e.js            # Main E2E runner
└── package.json              # Node.js configuration
```

## How It Works

1. **ApplicationManager** starts the .NET Web application using `dotnet run`
2. **ApiClient** waits for the health endpoint to confirm the app is ready
3. **Cucumber** executes Gherkin scenarios using step definitions
4. **Step definitions** make HTTP requests to test API functionality
5. **ApplicationManager** stops the application and cleans up

## Key Features

### Patient Registration Testing
- Tests all CRUD operations for patients
- Validates input validation and error handling
- Checks compliance tracking setup

### GDPR Violation Monitoring
- Verifies the ViolationEvaluatorService background service is running
- Tests violation detection for overdue access requests
- Validates that reactive consequences execute correctly
- Ensures the application remains stable during violation processing

### Application Lifecycle Management
- Automatically starts and stops the .NET application
- Handles process cleanup and error scenarios
- Monitors application health and readiness

## Sample Test Output

```
🚀 Starting DOI Web Application...
✅ Application started successfully!
⏳ Waiting for application to be ready...
✅ Application health check passed!

🧪 Running E2E tests...

Feature: Patient Registration
  ✅ Successful patient registration
  ✅ Patient registration with missing required fields
  ✅ Prevent duplicate patient registration

Feature: GDPR Violation Detection
  ✅ ViolationEvaluatorService runs periodically
  ✅ Detect access request response deadline violation
  ✅ Application remains healthy during violation detection

✅ All E2E tests passed!

🧹 Cleaning up...
🛑 Stopping DOI Web Application...
✅ Application stopped successfully
```

## Configuration

The tests are configured to connect to:
- **HTTP**: `http://localhost:5039`
- **HTTPS**: `https://localhost:7039`
- **Health Endpoint**: `/health`

These can be modified in `src/ApiClient.ts` if needed.

## Troubleshooting

### Application Won't Start
- Ensure .NET 9.0 SDK is installed
- Check that the DOI solution builds successfully: `dotnet build`
- Verify no other process is using ports 5039 or 7039

### Tests Fail
- Check application logs for errors
- Verify the health endpoint is accessible: `curl http://localhost:5039/health`
- Ensure database is accessible and properly configured

### Port Conflicts
- Modify the ports in `ApplicationManager.ts` if needed
- Update the `ApiClient.ts` base URL accordingly

## Contributing

To add new test scenarios:

1. Create or modify `.feature` files in the `features/` directory
2. Implement step definitions in the `step-definitions/` directory
3. Add any new API client methods to `ApiClient.ts`
4. Update this README with the new scenarios

## Next Steps

Future enhancements could include:
- Database state verification through API endpoints
- Log file analysis for deeper violation detection testing
- Performance testing scenarios
- Integration with CI/CD pipelines
- Test report generation and visualization