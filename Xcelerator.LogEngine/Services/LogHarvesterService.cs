using System.Collections.Concurrent;
using Xcelerator.LogEngine.Models;

namespace Xcelerator.LogEngine.Services
{
    public class LogHarvesterService
    {

        /// <summary>
        /// Downloads logs from multiple machines in parallel, extracts recent lines, and cleans up temp files.
        /// </summary>
        /// <param name="machines">List of machine names (e.g. sc1, sc2)</param>
        /// <param name="remoteShareTemplate">Format string for share (e.g. "\\{0}\c$\VCLogs")</param>
        /// <param name="pattern">Regex pattern to search for (e.g. "Error")</param>
        /// <param name="lookbackMinutes">How many minutes back to search (e.g. 2.5)</param>
        public async Task<List<LogResult>> GetLogsInParallelAsync(IEnumerable<string> machines, string remoteShareTemplate, string pattern, double lookbackMinutes)
        {
            var results = new ConcurrentBag<LogResult>();
            var tasks = new List<Task>();

            // Calculate the cutoff time once
            DateTime cutoffTime = DateTime.Now.AddMinutes(-lookbackMinutes);

            foreach (var machine in machines)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var result = await ProcessSingleMachineAsync(machine, remoteShareTemplate, pattern, cutoffTime);
                    results.Add(result);
                }));
            }

            // Wait for all downloads to finish
            await Task.WhenAll(tasks);

            return results.ToList();
        }

        private async Task<LogResult> ProcessSingleMachineAsync(string machine, string shareTemplate, string pattern, DateTime cutoffTime)
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

                // 4. Parse Locally
                // Optimization: ReadLines is lazy; we iterate once.
                foreach (var line in File.ReadLines(localFilePath))
                {
                    // Filter 1: Check pattern match first (faster than timestamp parsing)
                    if (!string.IsNullOrEmpty(pattern) &&
                        !line.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Filter 2: Check Timestamp
                    // Expected format: MM/DD/YYYY HH:MM:SS.fff
                    // We'll try to parse the first 23 characters if available
                    if (line.Length >= 23)
                    {
                        string timestampStr = line.Substring(0, 23);
                        if (DateTime.TryParse(timestampStr, out DateTime logTime))
                        {
                            if (logTime >= cutoffTime)
                            {
                                result.LogLines.Add(line);
                            }
                        }
                    }
                }

                if (result.LogLines.Count == 0)
                {
                    result.LogLines.Add($"No logs found matching '{pattern}' in the last {(DateTime.Now - cutoffTime).TotalMinutes:F1} minutes.");
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
