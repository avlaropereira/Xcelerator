# Xcelerator.LogEngine.Tests

This project contains unit tests for the `Xcelerator.LogEngine` library, specifically testing the `LogHarvesterService` class.

## Framework

The tests are written using **xUnit**, a modern, extensible unit testing framework for .NET.

## Test Coverage

The test suite covers the following scenarios for the `GetLogsInParallelAsync` method:

### Success Scenarios
- ? Returns matching logs from a valid machine with recent logs
- ? Processes multiple machines in parallel
- ? Returns all recent logs when no pattern is specified
- ? Selects the most recent log file when multiple files exist
- ? Performs case-insensitive pattern matching

### Error Scenarios
- ? Returns failure result for non-existent paths
- ? Returns error for empty directories
- ? Returns "no match" message when pattern doesn't match any logs
- ? Filters out old logs based on the time window

### Edge Cases
- ? Returns empty results for empty machine list
- ? Skips log lines shorter than 23 characters
- ? Skips lines with invalid timestamps
- ? Returns all results even when some machines fail
- ? Correctly handles zero or very small lookback windows

### Performance Tests
- ? Efficiently processes large log files (10,000+ lines)

### Integration Tests
- ?? Real network share test (skipped by default - see below)

## Running the Tests

### Run all tests
```bash
dotnet test
```

### Run with detailed output
```bash
dotnet test --verbosity normal
```

### Run a specific test
```bash
dotnet test --filter "FullyQualifiedName~GetLogsInParallelAsync_WithValidMachineAndRecentLogs_ReturnsMatchingLogs"
```

### Run tests with coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Integration Tests

The test suite includes an integration test that connects to a real network share:

**Test**: `GetLogsInParallelAsync_WithRealNetworkShare_DownloadsAndValidatesLogs`  
**Network Path**: `\\SOA-C30COR01\D$\Proj\LogFiles\VC`

This test is **skipped by default** because it requires:
- Network access to the specified machine
- Appropriate permissions to access the share
- The share to contain actual log files

### Running the Integration Test

To enable and run the integration test:

1. **Remove the Skip attribute** from the test in `LogHarvesterServiceTests.cs`:
   ```csharp
   // Change from:
   [Fact(Skip = "Integration test - requires network access...")]
   
   // To:
   [Fact]
   ```

2. **Run the integration test specifically**:
   ```bash
   dotnet test --filter "Category=Integration"
   ```

3. **Or run with the specific test name**:
   ```bash
   dotnet test --filter "FullyQualifiedName~GetLogsInParallelAsync_WithRealNetworkShare_DownloadsAndValidatesLogs"
   ```

### Integration Test Requirements

- **Network Access**: Must be able to reach `\\SOA-C30COR01\D$\Proj\LogFiles\VC`
- **Permissions**: Must have read access to the D$ administrative share
- **Timeout**: Test has a 30-second timeout to prevent hanging
- **Expected Result**: Should find and download logs from the most recent log file

## Test Structure

Each test follows the **Arrange-Act-Assert (AAA)** pattern:

1. **Arrange**: Sets up test data and creates mock log files
2. **Act**: Calls the method being tested
3. **Assert**: Verifies the results match expectations

### Helper Methods

The test class includes helper methods to simplify test setup:

- `CreateTestMachineShare`: Creates a single log file for a machine
- `CreateMultipleLogFiles`: Creates multiple log files with different content

### Cleanup

All tests implement `IDisposable` to ensure proper cleanup of temporary files and directories created during testing.

## Dependencies

- **xUnit** (2.6.2): Test framework
- **Microsoft.NET.Test.Sdk** (17.8.0): Test SDK
- **Moq** (4.20.70): Mocking framework (included for future use)
- **coverlet.collector** (6.0.0): Code coverage collector

## Test Results

All 15 unit tests pass successfully, with 1 integration test skipped by default.

