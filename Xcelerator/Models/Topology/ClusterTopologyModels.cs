using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Xcelerator.Models.Topology
{
    /// <summary>
    /// Root object matching the JSON structure with ClusterListContainer wrapper
    /// </summary>
    public class TopologyRoot
    {
        [JsonPropertyName("ClusterListContainer")]
        public ClusterListContainer ClusterListContainer { get; set; } = new ClusterListContainer();
    }

    /// <summary>
    /// Root container for the infrastructure topology JSON
    /// </summary>
    public class ClusterListContainer
    {
        [JsonPropertyName("Clusters")]
        public List<ClusterNode> Clusters { get; set; } = new List<ClusterNode>();
    }

    /// <summary>
    /// Represents a cluster in the infrastructure topology
    /// </summary>
    public class ClusterNode
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("Servers")]
        public List<ServerNode> Servers { get; set; } = new List<ServerNode>();
    }

    /// <summary>
    /// Represents a server within a cluster
    /// </summary>
    public class ServerNode
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("children")]
        public List<Dictionary<string, string>> Children { get; set; } = new List<Dictionary<string, string>>();

        /// <summary>
        /// Parsed services from the children array
        /// </summary>
        [JsonIgnore]
        public List<ServiceNode> Services { get; set; } = new List<ServiceNode>();
    }

    /// <summary>
    /// Represents a service running on a server
    /// </summary>
    public class ServiceNode
    {
        /// <summary>
        /// Display name of the service (key from the dictionary)
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Internal identifier or status (value from the dictionary)
        /// </summary>
        public string InternalName { get; set; } = string.Empty;

        public ServiceNode(string displayName, string internalName)
        {
            DisplayName = displayName;
            InternalName = internalName;
        }
    }
}
