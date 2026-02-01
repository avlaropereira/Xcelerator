using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xcelerator.Models.Topology;

namespace Xcelerator.Services
{
    /// <summary>
    /// Manages server configuration in the servers.json file
    /// </summary>
    public static class ServerConfigManager
    {
        private const string ServersJsonPath = @"C:\XceleratorTool\Resources\servers.json";

        /// <summary>
        /// Parses a server name to extract cluster identifier and server type
        /// Example: "TCA-C34COR01" -> ClusterCode: "C34", ServerType: "COR"
        /// Example: "TCB-C1COR01" -> ClusterCode: "C1", ServerType: "COR"
        /// </summary>
        public static (string ClusterCode, string ServerType, bool IsValid) ParseServerName(string serverName)
        {
            // Pattern: XXX-CY[Y]SSS## where:
            // XXX = Site code (e.g., TCA, TOA, SOA, TCB)
            // C = Cluster prefix
            // Y[Y] = Cluster number - 1 or 2 digits (e.g., 1, 34, 32, 30)
            // SSS = Server type (e.g., COR, API, WEB, MED, IVR, AGM, AGT)
            // ## = Server number (1-2 digits)
            var pattern = @"^[A-Z]{2,3}-C(\d{1,2})([A-Z]{3})\d{1,2}$";
            var match = Regex.Match(serverName.ToUpper(), pattern);

            if (match.Success)
            {
                string clusterCode = "C" + match.Groups[1].Value; // e.g., "C34" or "C1"
                string serverType = match.Groups[2].Value; // e.g., "COR"

                // Validate that the server type is recognized
                var validServerTypes = new[] { "COR", "API", "WEB", "MED", "IVR", "AGM", "AGT" };
                if (validServerTypes.Contains(serverType))
                {
                    return (clusterCode, serverType, true);
                }
            }

            return (string.Empty, string.Empty, false);
        }

        /// <summary>
        /// Maps server name and cluster code to cluster name based on naming conventions
        /// Extracts cluster prefix from the server name (first 2 letters of site code)
        /// Example: "TCA-C1COR01" with "C1" -> "TC1"
        /// Example: "TOA-C34COR01" with "C34" -> "TO34"
        /// </summary>
        public static string MapClusterCodeToName(string serverName, string clusterCode)
        {
            // Extract the numeric part from cluster code
            if (clusterCode.StartsWith("C") && clusterCode.Length >= 2)
            {
                string clusterNumber = clusterCode.Substring(1);

                // Extract site code from server name (everything before the first dash)
                int dashIndex = serverName.IndexOf('-');
                if (dashIndex > 0)
                {
                    string siteCode = serverName.Substring(0, dashIndex);

                    // Take first 2 characters of site code as cluster prefix
                    if (siteCode.Length >= 2)
                    {
                        string clusterPrefix = siteCode.Substring(0, 2);
                        return $"{clusterPrefix}{clusterNumber}";
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the children services configuration based on server type
        /// </summary>
        public static List<Dictionary<string, string>> GetServerChildren(string serverType)
        {
            return serverType.ToUpper() switch
            {
                "COR" => new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { { "Virtual Cluster", "VC" } },
                    new Dictionary<string, string> { { "File Server", "FileServer" } },
                    new Dictionary<string, string> { { "CoOp Service", "CoOp" } },
                    new Dictionary<string, string> { { "Survey Service", "Surveys" } },
                    new Dictionary<string, string> { { "FS Drive Publisher", "FileServerSetUp" } },
                    new Dictionary<string, string> { { "DBCWS", "DBCWS" } }
                },
                "API" => new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { { "L7 Healthcheck", "Not Available" } },
                    new Dictionary<string, string> { { "Drone Service", "Not Available" } },
                    new Dictionary<string, string> { { "API Website", "API" } },
                    new Dictionary<string, string> { { "AutoSite", "Not Available" } },
                    new Dictionary<string, string> { { "DBCWS", "DBCWS" } }
                },
                "WEB" => new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { { "Agent", "Agent" } },
                    new Dictionary<string, string> { { "Authentication Server", "AuthorizationServer" } },
                    new Dictionary<string, string> { { "Cache Site", "CacheSite" } },
                    new Dictionary<string, string> { { "inContact", "inContact" } },
                    new Dictionary<string, string> { { "inControl", "inContact" } },
                    new Dictionary<string, string> { { "Report Service", "ReportService" } },
                    new Dictionary<string, string> { { "Security", "Security" } },
                    new Dictionary<string, string> { { "WebScripting", "WebScripting" } },
                    new Dictionary<string, string> { { "DBCWS", "DBCWS" } }
                },
                "MED" => new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { { "Virtual Cluster", "VC" } },
                    new Dictionary<string, string> { { "Media Server", "MediaServer" } },
                    new Dictionary<string, string> { { "CoOp Service", "CoOp" } },
                    new Dictionary<string, string> { { "Drone Service", "DroneService" } },
                    new Dictionary<string, string> { { "DBCWS", "DBCWS" } }
                },
                "IVR" or "AGM" or "AGT" => new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { { "Virtual Cluster", "VC" } },
                    new Dictionary<string, string> { { "CoOp Service", "CoOp" } },
                    new Dictionary<string, string> { { "Drone Service", "DroneService" } },
                    new Dictionary<string, string> { { "DBCWS", "DBCWS" } }
                },
                _ => new List<Dictionary<string, string>>()
            };
        }

        /// <summary>
        /// Checks if a cluster exists in the servers.json file
        /// </summary>
        public static bool ClusterExists(string clusterName)
        {
            try
            {
                if (!File.Exists(ServersJsonPath))
                    return false;

                string jsonContent = File.ReadAllText(ServersJsonPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var topology = JsonSerializer.Deserialize<TopologyRoot>(jsonContent, options);

                if (topology?.ClusterListContainer?.Clusters == null)
                    return false;

                return topology.ClusterListContainer.Clusters
                    .Any(c => c.Name.Equals(clusterName, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Adds a new server to the specified cluster in servers.json
        /// </summary>
        public static bool AddServerToCluster(string serverName, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                // Parse the server name
                var (clusterCode, serverType, isValid) = ParseServerName(serverName);
                
                if (!isValid)
                {
                    errorMessage = $"Invalid server name format: {serverName}\n" +
                                 "Expected format: XXX-CY[Y]SSS## where SSS is a valid server type (COR, API, WEB, MED, IVR, AGM, AGT)\n" +
                                 "Examples: TCB-C1COR01, TCA-C34COR01, TOA-C32API01";
                    return false;
                }

                // Map cluster code to cluster name using the server name
                string clusterName = MapClusterCodeToName(serverName, clusterCode);

                if (string.IsNullOrEmpty(clusterName))
                {
                    errorMessage = $"Could not determine cluster name for server: {serverName} (cluster code: {clusterCode})";
                    return false;
                }

                // Load existing topology
                if (!File.Exists(ServersJsonPath))
                {
                    errorMessage = $"Servers configuration file not found: {ServersJsonPath}";
                    return false;
                }

                string jsonContent = File.ReadAllText(ServersJsonPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                var topology = JsonSerializer.Deserialize<TopologyRoot>(jsonContent, options);

                if (topology?.ClusterListContainer?.Clusters == null)
                {
                    errorMessage = "Failed to parse servers.json or invalid structure";
                    return false;
                }

                // Find the target cluster
                var targetCluster = topology.ClusterListContainer.Clusters
                    .FirstOrDefault(c => c.Name.Equals(clusterName, StringComparison.OrdinalIgnoreCase));

                // If cluster doesn't exist, create it
                if (targetCluster == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Cluster '{clusterName}' not found, creating new cluster section");

                    targetCluster = new ClusterNode
                    {
                        Name = clusterName,
                        Servers = new List<ServerNode>()
                    };

                    topology.ClusterListContainer.Clusters.Add(targetCluster);

                    System.Diagnostics.Debug.WriteLine($"Created new cluster: {clusterName}");
                }

                // Check if server already exists in the cluster
                if (targetCluster.Servers.Any(s => s.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase)))
                {
                    errorMessage = $"Server '{serverName}' already exists in cluster '{clusterName}'";
                    return false;
                }

                // Create the new server entry
                var newServer = new ServerNode
                {
                    Name = serverName,
                    Children = GetServerChildren(serverType)
                };

                // Add the server to the cluster
                targetCluster.Servers.Add(newServer);

                // Save the updated topology back to file
                string updatedJson = JsonSerializer.Serialize(topology, options);
                File.WriteAllText(ServersJsonPath, updatedJson);

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error adding server: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Gets the log directory path for a remote machine
        /// </summary>
        public static string GetLogDirectoryPath(string serverName)
        {
            return $@"\\{serverName}\D$\Proj\LogFiles";
        }
    }
}
