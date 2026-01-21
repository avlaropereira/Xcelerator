# Infrastructure Topology Mapping - Implementation Guide

## Overview

This implementation provides a complete solution for loading, parsing, and mapping infrastructure topology data from JSON configuration files into strongly-typed C# objects. The topology represents a hierarchical structure of **Clusters → Servers → Services**.

---

## Architecture

### 1. **Data Models** (`Xcelerator.Models.Topology`)

Located in: `Xcelerator\Models\Topology\ClusterTopologyModels.cs`

#### TopologyRoot
- **Root object** that matches the JSON structure
- Contains a `ClusterListContainer` property
- Used during deserialization to match the JSON wrapper

#### ClusterListContainer
- **Container** for the cluster list
- Contains a list of `ClusterNode` objects
- Returned by `TopologyMapper.LoadTopology()`

#### ClusterNode
- Represents a **cluster** in the infrastructure
- Properties:
  - `Name`: Cluster identifier
  - `Servers`: List of servers in this cluster

#### ServerNode
- Represents a **server** within a cluster
- Properties:
  - `Name`: Server identifier
  - `Children`: Raw JSON children (list of key-value dictionaries)
  - `Services`: Parsed list of `ServiceNode` objects

#### ServiceNode
- Represents a **service** running on a server
- Properties:
  - `DisplayName`: Human-readable service name (key from JSON)
  - `InternalName`: Service identifier or status (value from JSON)

---

### 2. **Topology Mapper Service** (`Xcelerator.Services`)

Located in: `Xcelerator\Services\TopologyMapper.cs`

A static utility class that provides:

#### LoadTopology(string filePath)
- Loads and deserializes JSON from the specified file path
- Automatically parses all `children` dictionaries into `ServiceNode` objects
- Returns `ClusterListContainer` or `null` if loading fails

#### MapTopologyToClusters(IEnumerable<Cluster> clusters, ClusterListContainer topology)
- Maps topology data to existing `Cluster` objects by matching cluster names (case-insensitive)
- Sets the `Topology` property on matching clusters

#### PrintTopology(ClusterListContainer topology)
- Debug utility that prints the entire hierarchy to debug output
- Useful for verification and troubleshooting

---

### 3. **Integration with Cluster Model**

The `Cluster` model (`Xcelerator\Models\Cluster.cs`) has been extended with:

```csharp
public ClusterNode? Topology { get; set; }
```

This property holds the complete infrastructure topology for each cluster.

---

### 4. **PanelViewModel Integration**

The `InitializeClusters()` method in `PanelViewModel` has been extended to:

1. Load cluster configurations from `cluster.json`
2. Load infrastructure topology from `servers.json`
3. Map topology data to the cluster objects

#### Modified Methods:

**InitializeClusters()**
- Calls `LoadAndMapTopology()` after loading clusters

**LoadAndMapTopology()** *(new method)*
- Loads `servers.json` using `TopologyMapper.LoadTopology()`
- Maps topology to available clusters using `TopologyMapper.MapTopologyToClusters()`
- Optionally prints topology in DEBUG mode

---

## JSON Structure

### Expected Format: `servers.json`

```json
{
  "ClusterListContainer": {
    "Clusters": [
      {
        "Name": "ClusterName",
        "Servers": [
          {
            "Name": "ServerName",
            "children": [
              {"ServiceDisplayName": "ServiceInternalName"},
              {"AnotherService": "service-id-or-status"}
            ]
          }
        ]
      }
    ]
  }
}
```

### Key Points:
- **Root**: Object with `ClusterListContainer` property
- **ClusterListContainer**: Contains `Clusters` array
- **Each Cluster**: Has `Name` and `Servers` array
- **Each Server**: Has `Name` and `children` array
- **Each child**: Single key-value pair representing a service
- **Service Key**: Display name (e.g., "Web API")
- **Service Value**: Internal identifier or status (e.g., "api-service-v2")

---

## Usage Examples

See `Xcelerator\Examples\TopologyUsageExample.cs` for comprehensive examples.

### Example 1: Access Cluster Topology

```csharp
if (cluster.Topology != null)
{
    foreach (var server in cluster.Topology.Servers)
    {
        Console.WriteLine($"Server: {server.Name}");
        foreach (var service in server.Services)
        {
            Console.WriteLine($"  Service: {service.DisplayName} -> {service.InternalName}");
        }
    }
}
```

### Example 2: Find Specific Service

```csharp
var serversWithWebApi = cluster.Topology.Servers
    .Where(s => s.Services.Any(svc => svc.DisplayName == "Web API"))
    .ToList();
```

### Example 3: Get All Unique Services

```csharp
var uniqueServices = cluster.Topology.Servers
    .SelectMany(s => s.Services)
    .Select(s => s.DisplayName)
    .Distinct()
    .OrderBy(name => name)
    .ToList();
```

### Example 4: Count Services Per Server

```csharp
foreach (var server in cluster.Topology.Servers)
{
    Console.WriteLine($"{server.Name}: {server.Services.Count} services");
}
```

---

## File Locations

### Configuration Files (Runtime)
- **Cluster Config**: `C:\XceleratorTool\Resources\cluster.json`
- **Topology Config**: `C:\XceleratorTool\Resources\servers.json`

### Source Files
- **Models**: `Xcelerator\Models\Topology\ClusterTopologyModels.cs`
- **Service**: `Xcelerator\Services\TopologyMapper.cs`
- **Integration**: `Xcelerator\ViewModels\PanelViewModel.cs`
- **Examples**: `Xcelerator\Examples\TopologyUsageExample.cs`
- **Sample JSON**: `Xcelerator\Examples\sample-servers.json`

---

## Features

### ✅ Complete Hierarchy Mapping
- No nodes are skipped
- Full Cluster → Server → Service hierarchy preserved

### ✅ Strongly Typed Models
- Type-safe access to all topology elements
- IntelliSense support

### ✅ Extensible Design
- Adding new clusters, servers, or services requires no code changes
- Just update the JSON files

### ✅ Case-Insensitive Matching
- Cluster name matching is case-insensitive
- Robust against naming inconsistencies

### ✅ Error Handling
- Graceful handling of missing or malformed files
- Debug output for troubleshooting

### ✅ LINQ-Friendly
- Easy querying and filtering using LINQ
- Supports complex topology analysis

---

## Verification

### Debug Output
When running in DEBUG mode, the topology is automatically printed to the debug output:

```
=== Infrastructure Topology ===
Total Clusters: 3

[Cluster] Production-Cluster-01
  Servers: 3
  [Server] prod-server-01
    Services: 4
      [Service] Web API -> api-service-v2
      [Service] Database -> postgres-primary
      [Service] Cache -> redis-master
      [Service] Message Queue -> rabbitmq-prod
...
```

### Access in Code
After initialization, any cluster object can access its topology:

```csharp
var cluster = AvailableClusters.FirstOrDefault(c => c.Name == "Production-Cluster-01");
if (cluster?.Topology != null)
{
    Console.WriteLine($"Cluster has {cluster.Topology.Servers.Count} servers");
}
```

---

## Extension Points

### Adding Custom Properties
To add custom properties to the topology models, simply extend the classes in `ClusterTopologyModels.cs`:

```csharp
public class ServerNode
{
    // Existing properties...
    
    // New custom properties
    public string? IpAddress { get; set; }
    public int Port { get; set; }
    public string? Region { get; set; }
}
```

Update the JSON accordingly:

```json
{
  "Name": "server-name",
  "IpAddress": "192.168.1.100",
  "Port": 8080,
  "Region": "us-east-1",
  "children": [...]
}
```

### Custom Mapping Logic
To implement custom mapping behavior, extend or modify the `TopologyMapper` class in `TopologyMapper.cs`.

---

## Troubleshooting

### Topology Not Loading
- Check if `servers.json` exists at `C:\XceleratorTool\Resources\servers.json`
- Verify JSON syntax using a JSON validator
- Check debug output for error messages

### Clusters Not Mapping
- Ensure cluster names in `cluster.json` match those in `servers.json`
- Matching is case-insensitive but must be exact otherwise
- Check debug output for mapping results

### Services Not Appearing
- Verify `children` array format in JSON
- Each child must be a single key-value object
- Check that `ParseServices()` is being called

---

## Testing

### Unit Test Example

```csharp
[TestMethod]
public void LoadTopology_ValidFile_ReturnsContainer()
{
    // Arrange
    string path = @"C:\XceleratorTool\Resources\servers.json";
    
    // Act
    var topology = TopologyMapper.LoadTopology(path);
    
    // Assert
    Assert.IsNotNull(topology);
    Assert.IsTrue(topology.Clusters.Count > 0);
}

[TestMethod]
public void MapTopology_MatchingNames_MapsCorrectly()
{
    // Arrange
    var clusters = new List<Cluster>
    {
        new Cluster("Production-Cluster-01"),
        new Cluster("Staging-Cluster-01")
    };
    var topology = TopologyMapper.LoadTopology(@"path\to\servers.json");
    
    // Act
    TopologyMapper.MapTopologyToClusters(clusters, topology);
    
    // Assert
    Assert.IsNotNull(clusters[0].Topology);
    Assert.AreEqual("Production-Cluster-01", clusters[0].Topology.Name);
}
```

---

## Summary

This implementation provides a **production-ready, extensible solution** for managing infrastructure topology in the Xcelerator application. All topology data is:

- ✅ Fully mapped from JSON to strongly-typed objects
- ✅ Integrated with existing cluster management
- ✅ Queryable via LINQ
- ✅ Extensible without code changes
- ✅ Well-documented and testable

The solution handles the complete hierarchy without skipping any nodes and provides clear, consistent naming throughout.
