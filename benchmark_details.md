# RuleKeeper Performance Benchmark

## Overview

This benchmark measures the performance overhead introduced by the RuleKeeper GDPR compliance middleware by comparing request execution times with and without the middleware enabled.

## Running the Benchmark

```powershell
.\benchmark.ps1
```

The script will automatically:
1. Start RuleKeeper Manager and Webus application WITH middleware
2. Run 10 iterations of each test endpoint
3. Stop services and clean up
4. Start Webus application WITHOUT middleware (no Manager needed)
5. Run 10 iterations of each test endpoint again
6. Display comparative statistics

**Prerequisites:**
- MongoDB running on localhost:27017
- Node.js installed
- All dependencies installed (`npm install` in both Manager and Webus directories)
- Test user and schedules in database (run `node create-test-user.js` in Manager directory)

## Test Endpoints

The benchmark tests four key endpoints of the Webus application:

### 1. GetSchedules
- **Endpoint:** `GET /tickets/schedules`
- **Description:** Retrieves all available travel schedules
- **Authentication:** Authenticated as `testuser`
- **GDPR Operation:** "see schedules" (allowed for role "all")
- **Tests:** Read access to public data

### 2. BuyTicket
- **Endpoint:** `POST /tickets/buy_ticket`
- **Description:** Purchases a travel ticket
- **Authentication:** Authenticated as `testuser`
- **GDPR Operation:** "buy ticket" (allowed for role "user")
- **Request Body:**
  ```json
  {
    "name": "Test User",
    "e_mail": "testuser@example.com",
    "credit_card": 1234567890123456,
    "destination": "Paris",
    "schedule": "2025-12-15T00:00:00.000Z"
  }
  ```
- **Tests:** 
  - Access control (role-based)
  - Data ownership verification
  - Data transfer policy enforcement
  - Personal data insertion

### 3. Subscribe
- **Endpoint:** `POST /newsletter/subscribe`
- **Description:** Subscribes email to newsletter
- **Authentication:** Authenticated as `testuser`
- **GDPR Operation:** "subscribe to newsletter" (allowed for role "user")
- **Request Body:**
  ```json
  {
    "e_mail": "testuser@example.com"
  }
  ```
- **Tests:**
  - Access control (role-based)
  - Personal data collection
  - Purpose limitation

### 4. PurchaseHistory
- **Endpoint:** `GET /tickets/purchase_history?name=Test User`
- **Description:** Retrieves user's ticket purchase history
- **Authentication:** Authenticated as `testuser`
- **GDPR Operation:** "see purchase history" (allowed for role "user")
- **Query Parameter:** `name=Test User`
- **Tests:**
  - Access control (role-based)
  - Data ownership verification
  - Personal data retrieval
  - Subject access rights

## Output Format

### Per-Endpoint Statistics

For each endpoint, the benchmark displays:

```
--- EndpointName ---
  WITH middleware:
    Min: X ms | Max: Y ms | Avg: Z ms | Median: W ms
  WITHOUT middleware:
    Min: A ms | Max: B ms | Avg: C ms | Median: D ms
  Overhead: +E ms (+F%)
```

**Metrics Explained:**
- **Min:** Fastest request time across all iterations
- **Max:** Slowest request time across all iterations
- **Avg:** Average (mean) request time
- **Median:** Middle value when all times are sorted
- **Overhead:** Additional time and percentage increase caused by middleware
  - Positive value = middleware adds latency
  - Negative value = middleware performs better (rare, usually due to variance)

### Overall Statistics

```
=== OVERALL STATISTICS ===
WITH middleware (all requests):
  Avg: X ms | Median: Y ms
WITHOUT middleware (all requests):
  Avg: A ms | Median: B ms
Total Overhead: +C ms (+D%)
```

**Overall Metrics:**
- Combines all request times from all endpoints
- Provides aggregate view of middleware performance impact
- **Total Overhead:** Average additional latency across all operations

## Interpreting Results

### Typical Performance Characteristics

1. **Read Operations (GetSchedules, PurchaseHistory)**
   - Usually have lower overhead (10-20ms or 10-50%)
   - Simple access control checks
   - Less data validation

2. **Write Operations (BuyTicket, Subscribe)**
   - May have higher overhead (20-50ms or 20-100%)
   - More complex policy enforcement
   - Data ownership verification
   - Data transfer control checks

3. **Overall Overhead**
   - Typical range: 10-30ms (10-50%)
   - Acceptable for GDPR compliance benefits
   - Scales with policy complexity

### Example Results Analysis

From the provided benchmark results:

- **Best Performance:** PurchaseHistory (-1.69% overhead) - Nearly identical with/without middleware
- **Moderate Overhead:** Subscribe (+22.89%) - Acceptable for write operation with GDPR checks
- **Highest Overhead:** GetSchedules (+45.5%) - Higher variability in first request, likely due to initialization
- **Overall Impact:** +15.76% total overhead - Very competitive for GDPR enforcement middleware

### Factors Affecting Performance

**WITH Middleware:**
- Policy evaluation (OPA/WASM execution)
- Access control checks
- Data ownership verification
- Context resolution (principal → entity → roles)
- Consent validation
- Purpose limitation checks

**WITHOUT Middleware:**
- Direct database operations only
- No GDPR compliance checks
- No access control enforcement
- Higher security risk

## Alternative Test Scripts

### Simple Timed Tests

For quick manual testing with timing information:

```powershell
# Run WITH middleware
.\test-with-timing.ps1

# Run WITHOUT middleware
.\test-with-timing.ps1 -NoMiddleware
```

These scripts show individual request times but don't perform statistical analysis.

### Standard Test Script

For functionality testing without performance measurement:

```powershell
.\start-and-test.ps1
```

Shows verbose logs from Manager and Webus to debug policy enforcement.

## Customizing the Benchmark

### Changing Iteration Count

Edit `benchmark.ps1` line 7:

```powershell
$iterations = 10  # Change to desired number (e.g., 20, 50, 100)
```

More iterations provide better statistical accuracy but take longer to run.

### Adding More Endpoints

To benchmark additional endpoints, modify the `Run-Benchmark` function in `benchmark.ps1`:

1. Add test to `$testResults` hashtable
2. Create new test block with stopwatch timing
3. Add test name to `$testNames` array in results section

## Performance Optimization Tips

If overhead is too high for your use case:

1. **Cache Policy Data:** Ensure policy data is loaded once at startup
2. **Optimize Rego Policies:** Simplify policy rules where possible
3. **Use Indexes:** Ensure MongoDB has proper indexes for ownership queries
4. **Tune Connection Pools:** Adjust MongoDB connection pool settings
5. **Profile Individual Hooks:** Add timing to specific middleware hooks to identify bottlenecks

## Troubleshooting

### Services Won't Start
- Ensure MongoDB is running
- Check ports 3000 (Webus) and 3031 (Manager) are available
- Verify OPA executable exists in Manager directory

### All Tests Fail
- Verify test user exists: `node create-test-user.js` in Manager directory
- Verify schedules exist in MongoDB
- Check `debug-manager.log` for policy loading errors

### High Variability
- First run often slower due to cold start
- Run multiple full benchmarks and average results
- Close other applications to reduce system load

### Timeout Errors
- Increase wait times in benchmark script
- Check system resources (CPU, memory)
- Verify network connectivity (localhost loops)

## Architecture Notes

### With Middleware Flow
```
Request → Auth → RuleKeeper Context → Access Control → 
Mongoose Hooks (Ownership/Transfer) → Database → Response
```

### Without Middleware Flow
```
Request → Auth → Mongoose → Database → Response
```

The overhead represents the time spent in RuleKeeper's policy enforcement layer, which provides:
- ✅ GDPR Article 6 (Lawfulness) compliance
- ✅ GDPR Article 15 (Subject Access) enforcement
- ✅ GDPR Article 17 (Right to Erasure) support
- ✅ GDPR Article 21 (Right to Object) implementation
- ✅ Purpose limitation and data minimization
- ✅ Consent management and verification
- ✅ Audit trail for compliance demonstration

## License

See main repository LICENSE file.
