using System.Collections.Concurrent;
using Xcelerator.LogEngine.Models;

namespace Xcelerator.LogEngine.Services
{
    public class LogHarvesterService
    {

        /// <summary>
        /// Downloads logs from multiple machines in parallel and returns the local file path of the downloaded log file.
        /// The downloaded file is stored in a temporary directory and will NOT be automatically cleaned up.
        /// </summary>
        /// <param name="machines">List of machine names (e.g. sc1, sc2)</param>
        /// <param name="remoteShareTemplate">Format string for share (e.g. "\\{0}\c$\VCLogs")</param>
        public async Task<List<LogResult>> GetLogsInParallelAsync(IEnumerable<string> machines, string remoteShareTemplate)
        {
            var results = new ConcurrentBag<LogResult>();
            var tasks = new List<Task>();

            foreach (var machine in machines)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var result = await ProcessSingleMachineAsync(machine, remoteShareTemplate);
                    results.Add(result);
                }));
            }

            // Wait for all downloads to finish
            await Task.WhenAll(tasks);

            return results.ToList();
        }

        private async Task<LogResult> ProcessSingleMachineAsync(string machine, string shareTemplate)
        {
            var result = new LogResult { MachineName = machine, Success = true };

            // 1. Setup Temporary Workspace
            string tempFolder = Path.Combine(Path.GetTempPath(), "XceleratorLogs", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);

            try
            {
                string remotePath = string.Format(shareTemplate, machine);

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

                // 3. Copy to Temp (Optimized with Async I/O)
                string localFilePath = Path.Combine(tempFolder, recentFile.Name);
                await CopyFileAsync(recentFile.FullName, localFilePath);

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

        private async Task CopyFileAsync(string source, string destination)
        {
            const int bufferSize = 1024 * 1024; // 1 MB buffer for better performance on large files
            
            using (FileStream sourceStream = new FileStream(
                source, 
                FileMode.Open, 
                FileAccess.Read, 
                FileShare.ReadWrite,
                bufferSize,
                useAsync: true))
            using (FileStream destStream = new FileStream(
                destination,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize,
                useAsync: true))
            {
                await sourceStream.CopyToAsync(destStream, bufferSize);
            }
        }
    }
}
