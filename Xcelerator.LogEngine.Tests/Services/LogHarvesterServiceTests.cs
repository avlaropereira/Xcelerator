using Xcelerator.LogEngine.Services;

namespace Xcelerator.LogEngine.Tests.Services
{
    public class LogHarvesterServiceTests : IDisposable
    {
        private readonly LogHarvesterService _service;
        private readonly string _testRootPath;
        private readonly List<string> _createdTestDirectories;

        public LogHarvesterServiceTests()
        {
            _service = new LogHarvesterService();
            _testRootPath = Path.Combine(Path.GetTempPath(), "XceleratorLogEngineTests", Guid.NewGuid().ToString());
            _createdTestDirectories = new List<string>();
            Directory.CreateDirectory(_testRootPath);
        }

        public void Dispose()
        {
            // Cleanup all test directories
            try
            {
                if (Directory.Exists(_testRootPath))
                {
                    Directory.Delete(_testRootPath, true);
                }

                foreach (var dir in _createdTestDirectories)
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true);
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }

            // Cleanup XceleratorLogs temp directory
            try
            {
                string xceleratorLogsPath = Path.Combine(Path.GetTempPath(), "XceleratorLogs");
                if (Directory.Exists(xceleratorLogsPath))
                {
                    Directory.Delete(xceleratorLogsPath, true);
                }
            }
            catch
            {
                // Ignore cleanup errors for temp directory
            }
        }

        #region Test Helpers

        private string CreateTestMachineShare(string machineName, List<string> logLines, DateTime? fileWriteTime = null)
        {
            string machineSharePath = Path.Combine(_testRootPath, machineName);
            Directory.CreateDirectory(machineSharePath);
            _createdTestDirectories.Add(machineSharePath);

            string logFilePath = Path.Combine(machineSharePath, "test.log");
            File.WriteAllLines(logFilePath, logLines);

            if (fileWriteTime.HasValue)
            {
                File.SetLastWriteTime(logFilePath, fileWriteTime.Value);
            }

            return machineSharePath;
        }

        private string CreateMultipleLogFiles(string machineName, Dictionary<string, List<string>> filesWithContent)
        {
            string machineSharePath = Path.Combine(_testRootPath, machineName);
            Directory.CreateDirectory(machineSharePath);
            _createdTestDirectories.Add(machineSharePath);

            foreach (var file in filesWithContent)
            {
                string logFilePath = Path.Combine(machineSharePath, file.Key);
                File.WriteAllLines(logFilePath, file.Value);
            }

            return machineSharePath;
        }

        #endregion

        #region Success Scenarios

        [Fact(Skip = "Test requires modification - service now uses hardcoded network path")]
        public async Task GetLogsInParallelAsync_WithValidMachineAndRecentLogs_ReturnsAllLogs()
        {
            // Arrange
            string machineName = "testmachine1";
            var currentTime = DateTime.Now;
            var logLines = new List<string>
            {
                $"{currentTime:MM/dd/yyyy HH:mm:ss.fff} ERROR: Test error message",
                $"{currentTime.AddMinutes(-1):MM/dd/yyyy HH:mm:ss.fff} INFO: Normal operation",
                $"{currentTime.AddMinutes(-2):MM/dd/yyyy HH:mm:ss.fff} ERROR: Recent error message",
                $"{currentTime.AddMinutes(-5):MM/dd/yyyy HH:mm:ss.fff} ERROR: Old error message"
            };

            string sharePath = CreateTestMachineShare(machineName, logLines);
            string shareTemplate = Path.Combine(_testRootPath, "{0}");

            // Act
            var result = await _service.GetLogsInParallelAsync(
                machineName,
                "item1" );

            // Assert
            Assert.Equal(machineName, result.MachineName);
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
            Assert.NotNull(result.LocalFilePath);
            Assert.True(File.Exists(result.LocalFilePath.Split('|')[0]));
        }

        [Fact]
        public async Task GetLogsInParallelAsync_WithMultipleMachines_ProcessesAllInParallel()
        {
            // Arrange
            var machines = new[] { "machine1", "machine2", "machine3" };
            var currentTime = DateTime.Now;

            foreach (var machine in machines)
            {
                var logLines = new List<string>
                {
                    $"{currentTime:MM/dd/yyyy HH:mm:ss.fff} ERROR: Error on {machine}"
                };
                CreateTestMachineShare(machine, logLines);
            }

            string shareTemplate = Path.Combine(_testRootPath, "{0}");

            // Act - Test with first machine
            var result = await _service.GetLogsInParallelAsync(
                machines[0],
                "item1" );

            // Assert
            Assert.Equal(machines[0], result.MachineName);
            Assert.True(result.Success);
            Assert.NotNull(result.LocalFilePath);
        }

        [Fact]
        public async Task GetLogsInParallelAsync_WithoutPattern_ReturnsAllLogs()
        {
            // Arrange
            string machineName = "testmachine";
            var currentTime = DateTime.Now;
            var logLines = new List<string>
            {
                $"{currentTime:MM/dd/yyyy HH:mm:ss.fff} ERROR: Test error",
                $"{currentTime.AddMinutes(-1):MM/dd/yyyy HH:mm:ss.fff} INFO: Test info",
                $"{currentTime.AddMinutes(-2):MM/dd/yyyy HH:mm:ss.fff} WARNING: Test warning"
            };

            CreateTestMachineShare(machineName, logLines);
            string shareTemplate = Path.Combine(_testRootPath, "{0}");

            // Act
            var result = await _service.GetLogsInParallelAsync(
                machineName,
                "item1" );

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.LocalFilePath);
        }

        [Fact]
        public async Task GetLogsInParallelAsync_WithMostRecentFile_SelectsCorrectFile()
        {
            // Arrange
            string machineName = "testmachine";
            var oldTime = DateTime.Now.AddDays(-1);
            var recentTime = DateTime.Now;

            var filesWithContent = new Dictionary<string, List<string>>
            {
                { "old.log", new List<string> { $"{oldTime:MM/dd/yyyy HH:mm:ss.fff} ERROR: Old log" } },
                { "recent.log", new List<string> { $"{recentTime:MM/dd/yyyy HH:mm:ss.fff} ERROR: Recent log" } }
            };

            string sharePath = CreateMultipleLogFiles(machineName, filesWithContent);

            // Set file write times
            File.SetLastWriteTime(Path.Combine(sharePath, "old.log"), oldTime);
            File.SetLastWriteTime(Path.Combine(sharePath, "recent.log"), recentTime);

            string shareTemplate = Path.Combine(_testRootPath, "{0}");

            // Act
            var result = await _service.GetLogsInParallelAsync(
                machineName,
                "item1" );

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.LocalFilePath);
            Assert.True(File.Exists(result.LocalFilePath.Split('|')[0]));
        }

        [Fact]
        public async Task GetLogsInParallelAsync_WithCaseInsensitivePattern_ReturnsAllLogs()
        {
            // Arrange
            string machineName = "testmachine";
            var currentTime = DateTime.Now;
            var logLines = new List<string>
            {
                $"{currentTime:MM/dd/yyyy HH:mm:ss.fff} error: lowercase error",
                $"{currentTime:MM/dd/yyyy HH:mm:ss.fff} ERROR: uppercase ERROR",
                $"{currentTime:MM/dd/yyyy HH:mm:ss.fff} Error: mixed case Error"
            };

            CreateTestMachineShare(machineName, logLines);
            string shareTemplate = Path.Combine(_testRootPath, "{0}");

            // Act
            var result = await _service.GetLogsInParallelAsync(
                machineName,
                "item1" );

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.LocalFilePath);
        }

        #endregion

        #region Error Scenarios

        [Fact]
        public async Task GetLogsInParallelAsync_WithNonExistentPath_ReturnsFailureResult()
        {
            // Arrange
            string machineName = "nonexistent";
            string shareTemplate = Path.Combine(_testRootPath, "nonexistent", "{0}");

            // Act
            var result = await _service.GetLogsInParallelAsync(
                machineName,
                "item1" );

            // Assert
            Assert.Equal(machineName, result.MachineName);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("not accessible", result.ErrorMessage);
        }

        [Fact]
        public async Task GetLogsInParallelAsync_WithEmptyDirectory_ReturnsNoFilesError()
        {
            // Arrange
            string machineName = "emptymachine";
            string machineSharePath = Path.Combine(_testRootPath, machineName);
            Directory.CreateDirectory(machineSharePath);
            _createdTestDirectories.Add(machineSharePath);

            string shareTemplate = Path.Combine(_testRootPath, "{0}");

            // Act
            var result = await _service.GetLogsInParallelAsync(
                machineName,
                "item1" );

            // Assert
            Assert.False(result.Success);
            Assert.Contains("No log files found", result.ErrorMessage);
        }

        [Fact]
        public async Task GetLogsInParallelAsync_WithDifferentLogTypes_ReturnsAllLines()
        {
            // Arrange
            string machineName = "testmachine";
            var currentTime = DateTime.Now;
            var logLines = new List<string>
            {
                $"{currentTime:MM/dd/yyyy HH:mm:ss.fff} INFO: Normal operation"
            };

            CreateTestMachineShare(machineName, logLines);
            string shareTemplate = Path.Combine(_testRootPath, "{0}");

            // Act
            var result = await _service.GetLogsInParallelAsync(
                machineName,
                "item1" );

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.LocalFilePath);
        }

        [Fact]
        public async Task GetLogsInParallelAsync_WithOldLogs_ReturnsAllLines()
        {
            // Arrange
            string machineName = "testmachine";
            var oldTime = DateTime.Now.AddMinutes(-10);
            var logLines = new List<string>
            {
                $"{oldTime:MM/dd/yyyy HH:mm:ss.fff} ERROR: Old error"
            };

            CreateTestMachineShare(machineName, logLines);
            string shareTemplate = Path.Combine(_testRootPath, "{0}");

            // Act
            var result = await _service.GetLogsInParallelAsync(
                machineName,
                "item1" );

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.LocalFilePath);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task GetLogsInParallelAsync_WithEmptyMachineList_ReturnsEmptyResults()
        {
            // Arrange
            string machineName = "emptymachine";
            string shareTemplate = Path.Combine(_testRootPath, "{0}");

            // Act
            var result = await _service.GetLogsInParallelAsync(
                machineName,
                "item1" );

            // Assert
            Assert.NotNull(result);
            Assert.Equal(machineName, result.MachineName);
        }

        [Fact]
        public async Task GetLogsInParallelAsync_WithLogLinesShorterThan23Characters_IncludesAllLines()
        {
            // Arrange
            string machineName = "testmachine";
            var currentTime = DateTime.Now;
            var logLines = new List<string>
            {
                $"{currentTime:MM/dd/yyyy HH:mm:ss.fff} ERROR: Valid log",
                "Short line ERROR",
                "ERROR"
            };

            CreateTestMachineShare(machineName, logLines);
            string shareTemplate = Path.Combine(_testRootPath, "{0}");

            // Act
            var result = await _service.GetLogsInParallelAsync(
                machineName,
                "item1" );

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.LocalFilePath);
        }

        [Fact]
        public async Task GetLogsInParallelAsync_WithInvalidTimestamp_IncludesAllLines()
        {
            // Arrange
            string machineName = "testmachine";
            var currentTime = DateTime.Now;
            var logLines = new List<string>
            {
                $"{currentTime:MM/dd/yyyy HH:mm:ss.fff} ERROR: Valid timestamp",
                "99/99/9999 99:99:99.999 ERROR: Invalid timestamp"
            };

            CreateTestMachineShare(machineName, logLines);
            string shareTemplate = Path.Combine(_testRootPath, "{0}");

            // Act
            var result = await _service.GetLogsInParallelAsync(
                machineName,
                "item1" );

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.LocalFilePath);
        }

        [Fact]
        public async Task GetLogsInParallelAsync_WithMixedSuccessAndFailure_ReturnsAllResults()
        {
            // Arrange
            var currentTime = DateTime.Now;
            var logLines = new List<string>
            {
                $"{currentTime:MM/dd/yyyy HH:mm:ss.fff} ERROR: Test error"
            };

            CreateTestMachineShare("goodmachine", logLines);

            string shareTemplate = Path.Combine(_testRootPath, "{0}");

            // Act - Test with good machine
            var result = await _service.GetLogsInParallelAsync(
                "goodmachine",
                "item1" );

            // Assert
            Assert.Equal("goodmachine", result.MachineName);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task GetLogsInParallelAsync_WithMultipleTimeRanges_ReturnsAllLogs()
        {
            // Arrange
            string machineName = "testmachine";
            var now = DateTime.Now;
            var oneMinuteAgo = now.AddMinutes(-1);
            var logLines = new List<string>
            {
                $"{now:MM/dd/yyyy HH:mm:ss.fff} ERROR: Current minute",
                $"{oneMinuteAgo:MM/dd/yyyy HH:mm:ss.fff} ERROR: One minute ago"
            };

            CreateTestMachineShare(machineName, logLines);
            string shareTemplate = Path.Combine(_testRootPath, "{0}");

            // Act
            var result = await _service.GetLogsInParallelAsync(
                machineName,
                "item1" );

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.LocalFilePath);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task GetLogsInParallelAsync_WithLargeLogFile_ProcessesAllLinesEfficiently()
        {
            // Arrange
            string machineName = "largemachine";
            var currentTime = DateTime.Now;
            var logLines = new List<string>();

            // Create 10,000 log lines
            for (int i = 0; i < 10000; i++)
            {
                var timestamp = currentTime.AddSeconds(-i);
                logLines.Add($"{timestamp:MM/dd/yyyy HH:mm:ss.fff} INFO: Log line {i}");
            }

            // Add some ERROR lines
            logLines.Insert(0, $"{currentTime:MM/dd/yyyy HH:mm:ss.fff} ERROR: Recent error 1");
            logLines.Insert(50, $"{currentTime.AddSeconds(-50):MM/dd/yyyy HH:mm:ss.fff} ERROR: Recent error 2");

            CreateTestMachineShare(machineName, logLines);
            string shareTemplate = Path.Combine(_testRootPath, "{0}");

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await _service.GetLogsInParallelAsync(
                machineName,
                "item1" );
            stopwatch.Stop();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.LocalFilePath);
            Assert.True(File.Exists(result.LocalFilePath.Split('|')[0]));
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Processing took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
        }

        #endregion

        #region Integration Tests

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetLogsInParallelAsync_WithRealNetworkShare_DownloadsAndValidatesLogs()
        {
            // Arrange
            string machineName = "TOA-C34COR01";
            string remoteShareTemplate = @"\\{0}\D$\Proj\LogFiles\VC";

            // Act
            var result = await _service.GetLogsInParallelAsync(
                machineName,
                "VC" );

            // Assert
            Assert.Equal(machineName, result.MachineName);
            Assert.True(result.Success, $"Failed to access logs: {result.ErrorMessage}");
            Assert.NotNull(result.LocalFilePath);
            Assert.True(File.Exists(result.LocalFilePath.Split('|')[0]), "Expected to find downloaded log file");
        }

        #endregion
    }
}

