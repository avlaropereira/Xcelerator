using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using Xcelerator.LogEngine.Models;

namespace Xcelerator.LogEngine.Services
{
    public class LogHarvesterService
    {
        // Optimized buffer size for network transfers (8MB instead of 1MB)
        private const int NetworkBufferSize = 8 * 1024 * 1024; // 8MB

        /// <summary>
        /// Downloads logs from a single machine and item, and returns the local file path of the downloaded log file.
        /// The downloaded file is stored in a temporary directory and will NOT be automatically cleaned up.
        /// </summary>
        /// <param name="machine">Machine name (e.g. sc1)</param>
        /// <param name="item">Item identifier (e.g. service or component name)</param>
        public async Task<LogResult> GetLogsInParallelAsync(string machine, string item)
        {
            if (string.IsNullOrWhiteSpace(machine))
                throw new ArgumentException("Machine name cannot be null or empty.", nameof(machine));
            
            if (string.IsNullOrWhiteSpace(item))
                throw new ArgumentException("Item cannot be null or empty.", nameof(item));
            
            return await ProcessSingleMachineAsync(machine, item);
        }

        private async Task<LogResult> ProcessSingleMachineAsync(string machine, string item)
        {
            var result = new LogResult { MachineName = machine, Success = true };

            // 1. Setup Temporary Workspace
            string tempFolder = Path.Combine(Path.GetTempPath(), "XceleratorLogs", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);

            try
            {
                string remotePath = string.Format(@"\\{0}\D$\Proj\LogFiles\{1}", machine, item);

                // 2. Find Recent File (Fast metadata check)
                var directoryInfo = new DirectoryInfo(remotePath);

                if (!Directory.Exists(remotePath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"Path not accessible: {remotePath}";
                    return result;
                }

                var recentFile = directoryInfo.GetFiles()
                                              .OrderByDescending(f => f.LastWriteTime)
                                              .FirstOrDefault();

                if (recentFile == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "No log files found.";
                    return result;
                }

                // 3. Copy to Temp (Optimized with larger buffer and async I/O)
                string localFilePath = Path.Combine(tempFolder, recentFile.Name);
                
                // Use optimized copy method
                await CopyFileOptimizedAsync(recentFile.FullName, localFilePath);

                // 4. Set the local file path with the original remote file name appended
                result.LocalFilePath = $"{localFilePath}";

            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                
                // Cleanup on error
                try
                {
                    if (Directory.Exists(tempFolder))
                        Directory.Delete(tempFolder, true);
                }
                catch { /* Ignore cleanup errors */ }
            }

            return result;
        }

        /// <summary>
        /// Optimized file copy with larger buffer and sequential scan hints
        /// </summary>
        private async Task CopyFileOptimizedAsync(string source, string destination)
        {
            using (FileStream sourceStream = new FileStream(
                source, 
                FileMode.Open, 
                FileAccess.Read, 
                FileShare.ReadWrite,
                NetworkBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (FileStream destStream = new FileStream(
                destination,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                NetworkBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await sourceStream.CopyToAsync(destStream, NetworkBufferSize);
            }
        }

        private async Task CopyFileAsync(string source, string destination)
        {
            // Legacy method - redirects to optimized version
            await CopyFileOptimizedAsync(source, destination);
        }
    }
}
