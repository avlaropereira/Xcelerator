using System.Collections.Concurrent;
using Xcelerator.LogEngine.Models;

namespace Xcelerator.LogEngine.Services
{
    public class LogHarvesterService
    {

        /// <summary>
        /// Downloads logs from multiple machines in parallel and returns all lines from the most recent log file.
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

                if (!directoryInfo.Exists)
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

                // 4. Read All Lines
                // ReadLines is lazy; we iterate once and add all lines
                foreach (var line in File.ReadLines(localFilePath))
                {
                    result.LogLines.Add(line);
                }

                if (result.LogLines.Count == 0)
                {
                    result.LogLines.Add("No log lines found in the file.");
                }

            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                // 5. CLEANUP: Aggressive deletion of temp data
                try
                {
                    if (Directory.Exists(tempFolder))
                        Directory.Delete(tempFolder, true);
                }
                catch { /* Ignore cleanup errors to prevent crashing main thread */ }
            }

            return result;
        }

        private async Task CopyFileAsync(string source, string destination)
        {
            using (FileStream sourceStream = File.Open(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (FileStream destStream = File.Create(destination))
            {
                await sourceStream.CopyToAsync(destStream);
            }
        }
    }
}
