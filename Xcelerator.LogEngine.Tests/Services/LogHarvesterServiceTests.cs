using System.Collections.Concurrent;
using Xcelerator.LogEngine.Models;
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

        [Fact]
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
            var results = await _service.GetLogsInParallelAsync(
                new[] { machineName },
                shareTemplate
            );

            // Assert
            Assert.Single(results);
            var result = results[0];
            Assert.Equal(machineName, result.MachineName);
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(4, result.LogLines.Count); // All lines returned, no filtering
            Assert.Contains("Test error message", result.LogLines[0]);
            Assert.Contains("Normal operation", result.LogLines[1]);
            Assert.Contains("Recent error message", result.LogLines[2]);
            Assert.Contains("Old error message", result.LogLines[3]);
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

            // Act
            var results = await _service.GetLogsInParallelAsync(
                machines,
                shareTemplate
            );

            // Assert
            Assert.Equal(3, results.Count);
            Assert.All(results, r => Assert.True(r.Success));
            Assert.All(results, r => Assert.NotEmpty(r.LogLines));
            Assert.Contains(results, r => r.MachineName == "machine1");
            Assert.Contains(results, r => r.MachineName == "machine2");
            Assert.Contains(results, r => r.MachineName == "machine3");
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
            var results = await _service.GetLogsInParallelAsync(
                new[] { machineName },
                shareTemplate
            );

            // Assert
            var result = results[0];
            Assert.True(result.Success);
            Assert.Equal(3, result.LogLines.Count); // All lines returned
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
            var results = await _service.GetLogsInParallelAsync(
                new[] { machineName },
                shareTemplate
            );

            // Assert
            var result = results[0];
            Assert.True(result.Success);
            Assert.Single(result.LogLines);
            Assert.Contains("Recent log", result.LogLines[0]);
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
            var results = await _service.GetLogsInParallelAsync(
                new[] { machineName },
                shareTemplate
            );

            // Assert
            var result = results[0];
            Assert.True(result.Success);
            Assert.Equal(3, result.LogLines.Count); // All lines returned regardless of pattern
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
            var results = await _service.GetLogsInParallelAsync(
                new[] { machineName },
                shareTemplate
            );

            // Assert
            Assert.Single(results);
            var result = results[0];
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
            var results = await _service.GetLogsInParallelAsync(
                new[] { machineName },
                shareTemplate
            );

            // Assert
            var result = results[0];
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
            var results = await _service.GetLogsInParallelAsync(
                new[] { machineName },
                shareTemplate
            );

            // Assert
            var result = results[0];
            Assert.True(result.Success);
            Assert.Single(result.LogLines);
            Assert.Contains("INFO: Normal operation", result.LogLines[0]);
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
            var results = await _service.GetLogsInParallelAsync(
                new[] { machineName },
                shareTemplate
            );

            // Assert
            var result = results[0];
            Assert.True(result.Success);
            Assert.Single(result.LogLines);
            Assert.Contains("Old error", result.LogLines[0]);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task GetLogsInParallelAsync_WithEmptyMachineList_ReturnsEmptyResults()
        {
            // Arrange
            var machines = Array.Empty<string>();
            string shareTemplate = Path.Combine(_testRootPath, "{0}");

            // Act
            var results = await _service.GetLogsInParallelAsync(
                machines,
                shareTemplate
            );

            // Assert
            Assert.Empty(results);
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
            var results = await _service.GetLogsInParallelAsync(
                new[] { machineName },
                shareTemplate
            );

            // Assert
            var result = results[0];
            Assert.True(result.Success);
            Assert.Equal(3, result.LogLines.Count); // All lines included, no filtering
            Assert.Contains("Valid log", result.LogLines[0]);
            Assert.Contains("Short line ERROR", result.LogLines[1]);
            Assert.Contains("ERROR", result.LogLines[2]);
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
            var results = await _service.GetLogsInParallelAsync(
                new[] { machineName },
                shareTemplate
            );

            // Assert
            var result = results[0];
            Assert.True(result.Success);
            Assert.Equal(2, result.LogLines.Count); // All lines included, no filtering
            Assert.Contains("Valid timestamp", result.LogLines[0]);
            Assert.Contains("Invalid timestamp", result.LogLines[1]);
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
            var machines = new[] { "goodmachine", "badmachine" };

            // Act
            var results = await _service.GetLogsInParallelAsync(
                machines,
                shareTemplate
            );

            // Assert
            Assert.Equal(2, results.Count);
            Assert.Contains(results, r => r.MachineName == "goodmachine" && r.Success);
            Assert.Contains(results, r => r.MachineName == "badmachine" && !r.Success);
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
            var results = await _service.GetLogsInParallelAsync(
                new[] { machineName },
                shareTemplate
            );

            // Assert
            var result = results[0];
            Assert.True(result.Success);
            Assert.Equal(2, result.LogLines.Count); // All lines returned
            Assert.Contains("Current minute", result.LogLines[0]);
            Assert.Contains("One minute ago", result.LogLines[1]);
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
            var results = await _service.GetLogsInParallelAsync(
                new[] { machineName },
                shareTemplate
            );
            stopwatch.Stop();

            // Assert
            var result = results[0];
            Assert.True(result.Success);
            Assert.Equal(10002, result.LogLines.Count); // All lines returned
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Processing took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
        }

        #endregion
    }
}
