using System.IO;
using System.Text.Json;
using Xcelerator.Models.Topology;

namespace Xcelerator.Services
{
    /// <summary>
    /// Service for loading and mapping infrastructure topology from JSON
    /// </summary>
    public static class TopologyMapper
    {
        /// <summary>
        /// Loads and parses the infrastructure topology from servers.json
        /// </summary>
        /// <param name="filePath">Path to the servers.json file</param>
        /// <returns>ClusterListContainer with parsed topology</returns>
        public static ClusterListContainer? LoadTopology(string filePath)
        {
            if (!File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"Topology file not found: {filePath}");
                return null;
            }

            try
            {
                string jsonContent = File.ReadAllText(filePath);

                // Deserialize the root object which contains the ClusterListContainer wrapper
                var root = JsonSerializer.Deserialize<TopologyRoot>(jsonContent);

                if (root?.ClusterListContainer != null)
                {
                    // Parse all services from children dictionaries
                    ParseServices(root.ClusterListContainer);
                    return root.ClusterListContainer;
                }

                System.Diagnostics.Debug.WriteLine("Failed to deserialize topology: root or ClusterListContainer is null");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading topology from {filePath}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Parses the children dictionaries into ServiceNode objects for all servers
        /// </summary>
        private static void ParseServices(ClusterListContainer container)
        {
            foreach (var cluster in container.Clusters)
            {
                foreach (var server in cluster.Servers)
                {
                    server.Services.Clear();

                    foreach (var childDict in server.Children)
                    {
                        // Each dictionary should have exactly one key-value pair
                        foreach (var kvp in childDict)
                        {
                            var service = new ServiceNode(kvp.Key, kvp.Value);
                            server.Services.Add(service);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Maps topology data to existing cluster objects by matching cluster names
        /// </summary>
        /// <param name="clusters">Collection of cluster objects to enrich with topology</param>
        /// <param name="topology">Loaded topology container</param>
        public static void MapTopologyToClusters(IEnumerable<Models.Cluster> clusters, ClusterListContainer topology)
        {
            if (topology == null || clusters == null)
                return;

            // Create a dictionary for fast lookup by cluster name
            var topologyDict = topology.Clusters.ToDictionary(
                c => c.Name,
                c => c,
                StringComparer.OrdinalIgnoreCase
            );

            foreach (var cluster in clusters)
            {
                if (topologyDict.TryGetValue(cluster.Name, out var topologyNode))
                {
                    cluster.Topology = topologyNode;
                    System.Diagnostics.Debug.WriteLine(
                        $"Mapped topology to cluster '{cluster.Name}': {topologyNode.Servers.Count} servers"
                    );
                }
            }
        }

        /// <summary>
        /// Prints the complete topology hierarchy to debug output
        /// Useful for verification and debugging
        /// </summary>
        public static void PrintTopology(ClusterListContainer topology)
        {
            if (topology == null)
            {
                System.Diagnostics.Debug.WriteLine("Topology is null");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"=== Infrastructure Topology ===");
            System.Diagnostics.Debug.WriteLine($"Total Clusters: {topology.Clusters.Count}");

            foreach (var cluster in topology.Clusters)
            {
                System.Diagnostics.Debug.WriteLine($"\n[Cluster] {cluster.Name}");
                System.Diagnostics.Debug.WriteLine($"  Servers: {cluster.Servers.Count}");

                foreach (var server in cluster.Servers)
                {
                    System.Diagnostics.Debug.WriteLine($"  [Server] {server.Name}");
                    System.Diagnostics.Debug.WriteLine($"    Services: {server.Services.Count}");

                    foreach (var service in server.Services)
                    {
                        System.Diagnostics.Debug.WriteLine($"      [Service] {service.DisplayName} -> {service.InternalName}");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"\n===============================");
        }
    }
}
