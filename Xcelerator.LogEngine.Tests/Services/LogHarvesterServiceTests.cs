using Xcelerator.LogEngine.Services;
using Xcelerator.LogEngine.Models;

namespace Xcelerator.LogEngine.Tests.Services
{
    /// <summary>
    /// Tests for LogHarvesterService.
    /// Note: Most tests are integration tests as the service uses hardcoded network paths.
    /// Unit tests focus on parameter validation and basic flow.
    /// </summary>
    public class LogHarvesterServiceTests : IDisposable
    {
        private readonly LogHarvesterService _service;
        private readonly string _testRootPath;

        public LogHarvesterServiceTests()
        {
            _service = new LogHarvesterService();
            _testRootPath = Path.Combine(Path.GetTempPath(), "XceleratorLogs");
        }

        public void Dispose()
        {
            // Cleanup XceleratorLogs temp directory
            try
            {
                if (Directory.Exists(_testRootPath))
                {
                    Directory.Delete(_testRootPath, true);
                }
            }
            catch
            {
                // Ignore cleanup errors for temp directory
            }
        }

        #region Unit Tests - Basic Validation

        [Fact]
        public async Task GetLogsInParallelAsync_WithNonExistentMachine_ReturnsFailureResult()
        {
            // Arrange
            string nonExistentMachine = "NONEXISTENT_MACHINE_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string item = "TestItem";

            // Act
            var result = await _service.GetLogsInParallelAsync(nonExistentMachine, item);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nonExistentMachine, result.MachineName);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("not accessible", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetLogsInParallelAsync_WithInvalidPath_ReturnsFailureResult()
        {
            // Arrange
            string machine = "INVALID_PATH";
            string item = "NonExistentItem_" + Guid.NewGuid().ToString("N").Substring(0, 8);

            // Act
            var result = await _service.GetLogsInParallelAsync(machine, item);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(machine, result.MachineName);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public async Task GetLogsInParallelAsync_ReturnsLogResultWithMachineName()
        {
            // Arrange
            string machine = "TestMachine";
            string item = "TestItem";

            // Act
            var result = await _service.GetLogsInParallelAsync(machine, item);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(machine, result.MachineName);
            Assert.IsType<LogResult>(result);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task GetLogsInParallelAsync_WithNullMachine_ThrowsException()
        {
            // Arrange
            string machine = null;
            string item = "TestItem";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _service.GetLogsInParallelAsync(machine, item);
            });
        }

        [Fact]
        public async Task GetLogsInParallelAsync_WithEmptyMachine_ThrowsException()
        {
            // Arrange
            string machine = "";
            string item = "TestItem";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _service.GetLogsInParallelAsync(machine, item);
            });
        }

        [Fact]
        public async Task GetLogsInParallelAsync_WithNullItem_ThrowsException()
        {
            // Arrange
            string machine = "TestMachine";
            string item = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _service.GetLogsInParallelAsync(machine, item);
            });
        }

        [Fact]
        public async Task GetLogsInParallelAsync_WithEmptyItem_ThrowsException()
        {
            // Arrange
            string machine = "TestMachine";
            string item = "";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _service.GetLogsInParallelAsync(machine, item);
            });
        }

        #endregion
    }
}

