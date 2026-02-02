using System;
using System.IO;

namespace Xcelerator
{
    /// <summary>
    /// Helper class for diagnostic logging to file
    /// Useful for troubleshooting published applications where Debug output isn't visible
    /// </summary>
    public static class DiagnosticHelper
    {
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Xcelerator",
            "diagnostic.log"
        );

        static DiagnosticHelper()
        {
            try
            {
                var directory = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Clear old log on startup
                if (File.Exists(LogFilePath))
                {
                    File.Delete(LogFilePath);
                }

                Log("=== Xcelerator Diagnostic Log ===");
                Log($"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Log($"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
                
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                Log($"Executable Path: {exePath}");
                Log($"Executable Directory: {Path.GetDirectoryName(exePath)}");
                Log("================================");
            }
            catch
            {
                // Ignore errors during initialization
            }
        }

        /// <summary>
        /// Write a message to the diagnostic log file
        /// </summary>
        public static void Log(string message)
        {
            try
            {
                var logMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
                
                // Also write to Debug output
                System.Diagnostics.Debug.WriteLine(logMessage);
            }
            catch
            {
                // Ignore errors during logging
            }
        }

        /// <summary>
        /// Get the path to the diagnostic log file
        /// </summary>
        public static string GetLogFilePath() => LogFilePath;

        /// <summary>
        /// Open the diagnostic log file in Notepad
        /// </summary>
        public static void OpenLogFile()
        {
            try
            {
                if (File.Exists(LogFilePath))
                {
                    System.Diagnostics.Process.Start("notepad.exe", LogFilePath);
                }
            }
            catch
            {
                // Ignore errors
            }
        }
    }
}
