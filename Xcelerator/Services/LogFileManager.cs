using System.Collections.Concurrent;
using System.IO;

namespace Xcelerator.Services
{
    /// <summary>
    /// Centralized service for tracking and managing downloaded log files
    /// Provides thread-safe operations and automatic cleanup on application exit
    /// </summary>
    public class LogFileManager
    {
        private readonly ConcurrentBag<string> _logFiles = new ConcurrentBag<string>();
        private readonly object _cleanupLock = new object();

        /// <summary>
        /// Registers a log file path for tracking and later cleanup
        /// </summary>
        /// <param name="logFilePath">Full path to the downloaded log file</param>
        public void RegisterLogFile(string logFilePath)
        {
            if (string.IsNullOrEmpty(logFilePath))
                return;

            _logFiles.Add(logFilePath);
            System.Diagnostics.Debug.WriteLine($"Registered log file for cleanup: {logFilePath}");
        }

        /// <summary>
        /// Cleans up all registered log files and their parent directories
        /// This method is thread-safe and can be called multiple times
        /// </summary>
        /// <returns>Statistics about the cleanup operation</returns>
        public CleanupStatistics CleanupAllLogFiles()
        {
            lock (_cleanupLock)
            {
                var stats = new CleanupStatistics();
                var directories = new HashSet<string>();

                System.Diagnostics.Debug.WriteLine($"Starting cleanup of {_logFiles.Count} log files...");

                // Delete all log files
                foreach (var logFile in _logFiles)
                {
                    try
                    {
                        if (File.Exists(logFile))
                        {
                            File.Delete(logFile);
                            stats.FilesDeleted++;

                            // Track parent directory for cleanup
                            var directory = Path.GetDirectoryName(logFile);
                            if (!string.IsNullOrEmpty(directory))
                            {
                                directories.Add(directory);
                            }

                            System.Diagnostics.Debug.WriteLine($"Deleted log file: {logFile}");
                        }
                        else
                        {
                            stats.FilesAlreadyDeleted++;
                        }
                    }
                    catch (Exception ex)
                    {
                        stats.FilesFailed++;
                        System.Diagnostics.Debug.WriteLine($"Error deleting log file {logFile}: {ex.Message}");
                    }
                }

                // Delete empty directories (from most nested to least nested)
                var sortedDirectories = directories.OrderByDescending(d => d.Length).ToList();
                foreach (var directory in sortedDirectories)
                {
                    try
                    {
                        if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                        {
                            Directory.Delete(directory);
                            stats.DirectoriesDeleted++;
                            System.Diagnostics.Debug.WriteLine($"Deleted empty directory: {directory}");
                        }
                    }
                    catch (Exception ex)
                    {
                        stats.DirectoriesFailed++;
                        System.Diagnostics.Debug.WriteLine($"Error deleting directory {directory}: {ex.Message}");
                    }
                }

                // Try to clean up the root XceleratorLogs directory if it's empty
                try
                {
                    string rootPath = Path.Combine(Path.GetTempPath(), "XceleratorLogs");
                    if (Directory.Exists(rootPath) && !Directory.EnumerateFileSystemEntries(rootPath).Any())
                    {
                        Directory.Delete(rootPath);
                        stats.DirectoriesDeleted++;
                        System.Diagnostics.Debug.WriteLine($"Deleted root XceleratorLogs directory: {rootPath}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error cleaning up root directory: {ex.Message}");
                }

                System.Diagnostics.Debug.WriteLine($"Cleanup completed: {stats}");
                return stats;
            }
        }

        /// <summary>
        /// Gets the count of currently tracked log files
        /// </summary>
        public int GetTrackedFileCount() => _logFiles.Count;

        /// <summary>
        /// Removes a specific log file from tracking and optionally deletes it
        /// </summary>
        /// <param name="logFilePath">Path to the log file to remove</param>
        /// <param name="deleteFile">Whether to delete the file immediately</param>
        /// <returns>True if the file was successfully removed/deleted, false otherwise</returns>
        public bool RemoveLogFile(string logFilePath, bool deleteFile = true)
        {
            if (string.IsNullOrEmpty(logFilePath))
                return false;

            try
            {
                if (deleteFile && File.Exists(logFilePath))
                {
                    File.Delete(logFilePath);

                    // Try to delete parent directory if empty
                    var directory = Path.GetDirectoryName(logFilePath);
                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                    {
                        if (!Directory.EnumerateFileSystemEntries(directory).Any())
                        {
                            Directory.Delete(directory);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing log file {logFilePath}: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Statistics about a cleanup operation
    /// </summary>
    public class CleanupStatistics
    {
        public int FilesDeleted { get; set; }
        public int FilesAlreadyDeleted { get; set; }
        public int FilesFailed { get; set; }
        public int DirectoriesDeleted { get; set; }
        public int DirectoriesFailed { get; set; }

        public int TotalFilesProcessed => FilesDeleted + FilesAlreadyDeleted + FilesFailed;

        public override string ToString()
        {
            return $"Files: {FilesDeleted} deleted, {FilesAlreadyDeleted} already deleted, {FilesFailed} failed | " +
                   $"Directories: {DirectoriesDeleted} deleted, {DirectoriesFailed} failed";
        }
    }
}
