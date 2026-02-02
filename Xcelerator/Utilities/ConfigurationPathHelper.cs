using System;
using System.IO;

namespace Xcelerator.Utilities
{
    /// <summary>
    /// Centralized helper for resolving configuration file paths
    /// Prioritizes LocalAppData for published applications
    /// </summary>
    public static class ConfigurationPathHelper
    {
        private static readonly string LocalAppDataPath = 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Xcelerator");

        /// <summary>
        /// Gets the path to the Resources directory, checking multiple locations in priority order:
        /// 1. %LocalAppData%\Xcelerator\Resources (for published apps)
        /// 2. Resources folder relative to base directory (for development)
        /// 3. Resources folder relative to executable location
        /// 4. Fallback to C:\XceleratorTool\Resources (legacy)
        /// </summary>
        public static string GetResourcesDirectory()
        {
            var locationsToCheck = new[]
            {
                // 1. LocalAppData (primary location for published apps)
                Path.Combine(LocalAppDataPath, "Resources"),

                // 2. Resources folder relative to base directory (development)
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources"),

                // 3. Resources folder relative to executable location
                Path.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "") ?? "", "Resources"),

                // 4. Fallback to legacy location
                @"C:\XceleratorTool\Resources"
            };

            foreach (var path in locationsToCheck)
            {
                if (Directory.Exists(path))
                {
                    System.Diagnostics.Debug.WriteLine($"[ConfigurationPathHelper] Using Resources directory: {path}");
                    return path;
                }
            }

            // If no existing directory found, return the LocalAppData path and create it
            var defaultPath = Path.Combine(LocalAppDataPath, "Resources");
            System.Diagnostics.Debug.WriteLine($"[ConfigurationPathHelper] No existing Resources directory found, will use: {defaultPath}");
            
            // Create the directory if it doesn't exist
            try
            {
                Directory.CreateDirectory(defaultPath);
                System.Diagnostics.Debug.WriteLine($"[ConfigurationPathHelper] Created Resources directory: {defaultPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigurationPathHelper] Failed to create Resources directory: {ex.Message}");
            }

            return defaultPath;
        }

        /// <summary>
        /// Resolves the full path to a resource file, checking multiple locations in priority order
        /// </summary>
        /// <param name="filename">Name of the resource file (e.g., "servers.json", "cluster.json")</param>
        /// <returns>Full path to the file, or null if not found</returns>
        public static string? ResolveResourceFilePath(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return null;

            System.Diagnostics.Debug.WriteLine($"[ConfigurationPathHelper] Resolving path for: {filename}");

            var locationsToCheck = new[]
            {
                // 1. LocalAppData (primary location for published apps)
                Path.Combine(LocalAppDataPath, "Resources", filename),

                // 2. Resources folder relative to base directory (development)
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", filename),

                // 3. Resources folder relative to executable location
                Path.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "") ?? "", "Resources", filename),

                // 4. Directly in base directory
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename),

                // 5. Fallback to legacy location
                Path.Combine(@"C:\XceleratorTool\Resources", filename)
            };

            foreach (var path in locationsToCheck)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigurationPathHelper] Checking: {path}");
                
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    System.Diagnostics.Debug.WriteLine($"[ConfigurationPathHelper] ✓ Found: {path}");
                    return path;
                }
            }

            // If file not found, return the LocalAppData path (caller should handle file creation if needed)
            var defaultPath = Path.Combine(LocalAppDataPath, "Resources", filename);
            System.Diagnostics.Debug.WriteLine($"[ConfigurationPathHelper] ✗ File not found in any location, returning default: {defaultPath}");
            return defaultPath;
        }

        /// <summary>
        /// Gets the path for servers.json configuration file
        /// </summary>
        public static string GetServersJsonPath() => ResolveResourceFilePath("servers.json") ?? 
            Path.Combine(LocalAppDataPath, "Resources", "servers.json");

        /// <summary>
        /// Gets the path for cluster.json configuration file
        /// </summary>
        public static string GetClusterJsonPath() => ResolveResourceFilePath("cluster.json") ?? 
            Path.Combine(LocalAppDataPath, "Resources", "cluster.json");

        /// <summary>
        /// Gets the path for ColorConfig.json configuration file
        /// </summary>
        public static string GetColorConfigJsonPath() => ResolveResourceFilePath("ColorConfig.json") ?? 
            Path.Combine(LocalAppDataPath, "Resources", "ColorConfig.json");

        /// <summary>
        /// Ensures the Resources directory exists in LocalAppData
        /// </summary>
        public static void EnsureResourcesDirectoryExists()
        {
            var resourcesPath = Path.Combine(LocalAppDataPath, "Resources");
            
            try
            {
                if (!Directory.Exists(resourcesPath))
                {
                    Directory.CreateDirectory(resourcesPath);
                    System.Diagnostics.Debug.WriteLine($"[ConfigurationPathHelper] Created Resources directory: {resourcesPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigurationPathHelper] Error creating Resources directory: {ex.Message}");
                throw;
            }
        }
    }
}
