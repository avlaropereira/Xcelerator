using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using Xcelerator.LogEngine.Models;

namespace Xcelerator.LogEngine.Services
{
    /// <summary>
    /// High-performance log harvester with parallel downloads and streaming support
    /// </summary>
    public class LogHarvesterServiceAdvanced
    {
        // Optimized buffer sizes
        private const int NetworkBufferSize = 8 * 1024 * 1024; // 8MB
        private const int ParallelChunkCount = 4; // Number of parallel download chunks
        private const long MinFileSizeForParallel = 10 * 1024 * 1024; // 10MB minimum for parallel

        /// <summary>
        /// Downloads logs from a single machine and item with advanced optimizations
        /// </summary>
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
            string tempFolder = Path.Combine(Path.GetTempPath(), "XceleratorLogs", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);

            try
            {
                string remotePath = string.Format(@"\\{0}\D$\Proj\LogFiles\{1}", machine, item);
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

                string localFilePath = Path.Combine(tempFolder, recentFile.Name);
                
                // Choose copy method based on file size
                if (recentFile.Length >= MinFileSizeForParallel)
                {
                    await CopyFileParallelAsync(recentFile.FullName, localFilePath, recentFile.Length);
                }
                else
                {
                    await CopyFileOptimizedAsync(recentFile.FullName, localFilePath);
                }

                result.LocalFilePath = localFilePath;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                
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
        /// Standard optimized file copy with large buffer
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

        /// <summary>
        /// Parallel chunk-based file copy for large files
        /// Splits file into chunks and downloads them in parallel
        /// </summary>
        private async Task CopyFileParallelAsync(string source, string destination, long fileSize)
        {
            long chunkSize = fileSize / ParallelChunkCount;
            var tasks = new List<Task>();

            // Create destination file with correct size
            using (var fs = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.SetLength(fileSize);
            }

            // Download chunks in parallel
            for (int i = 0; i < ParallelChunkCount; i++)
            {
                int chunkIndex = i;
                long start = chunkIndex * chunkSize;
                long end = (chunkIndex == ParallelChunkCount - 1) ? fileSize : start + chunkSize;
                
                tasks.Add(Task.Run(async () =>
                {
                    await CopyChunkAsync(source, destination, start, end - start, start);
                }));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Copies a specific chunk of a file
        /// </summary>
        private async Task CopyChunkAsync(string source, string destination, long offset, long length, long destOffset)
        {
            const int chunkBufferSize = 1024 * 1024; // 1MB buffer per chunk
            byte[] buffer = new byte[chunkBufferSize];

            using (FileStream sourceStream = new FileStream(
                source,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                chunkBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (FileStream destStream = new FileStream(
                destination,
                FileMode.Open,
                FileAccess.Write,
                FileShare.Write,
                chunkBufferSize,
                FileOptions.Asynchronous | FileOptions.RandomAccess))
            {
                sourceStream.Seek(offset, SeekOrigin.Begin);
                destStream.Seek(destOffset, SeekOrigin.Begin);

                long remaining = length;
                while (remaining > 0)
                {
                    int toRead = (int)Math.Min(chunkBufferSize, remaining);
                    int bytesRead = await sourceStream.ReadAsync(buffer, 0, toRead);
                    
                    if (bytesRead == 0)
                        break;

                    await destStream.WriteAsync(buffer, 0, bytesRead);
                    remaining -= bytesRead;
                }
            }
        }
    }
}
