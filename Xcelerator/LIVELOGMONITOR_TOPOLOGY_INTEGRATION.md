# Live Log Monitor - Dynamic Topology Integration

## Overview

The `LiveLogMonitorViewModel` has been updated to dynamically load remote machines and their services from the cluster topology instead of using hardcoded values.

---

## Changes Made

### Before (Hardcoded)
Previously, the `InitializeRemoteMachines` method:
- Parsed cluster names using regex (e.g., "SC10" → "SC", "10")
- Generated hardcoded machine names (COR01, API01, WEB01, MED01)
- Added fixed list of services for each machine type
- Would fail if cluster naming didn't match the expected pattern

### After (Dynamic from Topology)
Now the `InitializeRemoteMachines` method:
- Reads servers directly from `_cluster.Topology.Servers`
- Creates `RemoteMachineItem` objects for each server
- Dynamically adds all services from `server.Services` as children
- Gracefully handles missing or empty topology data

---

## How It Works

### 1. **Topology Check**
```csharp
if (_cluster == null || _cluster.Topology == null)
{
    // No topology available - return without error
    return;
}
```
- Checks if cluster and topology exist
- Returns early if not available (no machines loaded)
- No errors or exceptions thrown

### 2. **Server Iteration**
```csharp
foreach (var server in _cluster.Topology.Servers)
{
    var serverItem = new RemoteMachineItem
    {
        Name = server.Name,
        DisplayName = server.Name,
        IsExpanded = false
    };
    // ...
}
```
- Iterates through all servers from topology
- Creates parent `RemoteMachineItem` for each server
- Uses actual server name from topology

### 3. **Service Children**
```csharp
foreach (var service in server.Services)
{
    var serviceIdentifier = $"{server.Name}-{service.InternalName}";
    var serviceItem = new RemoteMachineItem
    {
        Name = serviceIdentifier,
        DisplayName = service.DisplayName
    };
    serverItem.Children.Add(serviceItem);
}
```
- Adds all services as children of the server
- Uses `service.DisplayName` for UI display
- Creates unique identifier: `{ServerName}-{InternalName}`

---

## Benefits

### ✅ **Dynamic & Flexible**
- Automatically adapts to any cluster structure
- No code changes needed when infrastructure changes
- Supports any number of servers and services

### ✅ **Data-Driven**
- All machine/service information comes from `servers.json`
- Single source of truth for infrastructure topology
- Easy to maintain and update

### ✅ **Graceful Degradation**
- If topology is missing: no machines shown (no crash)
- If topology is empty: returns silently
- Debug logging for troubleshooting

### ✅ **Consistent Naming**
- Uses actual server names from topology
- No regex parsing or name generation
- Works with any naming convention

---

## Example Data Flow

### 1. Topology Loaded (PanelViewModel)
```
servers.json → TopologyMapper.LoadTopology() → cluster.Topology
```

### 2. Live Log Monitor Opens
```
LiveLogMonitorViewModel created → InitializeRemoteMachines() called
```

### 3. Remote Machines Populated
```
cluster.Topology.Servers → RemoteMachineItem objects → _remoteMachines collection
```

### 4. UI Display
```
_remoteMachines → TreeView in UI → User sees servers and services
```

---

## Debug Output

When topology is successfully loaded, you'll see:
```
Loading 10 servers from topology for cluster 'SO30'
  Server 'SOA-C30COR01': 6 services loaded
  Server 'SOB-C30COR01': 6 services loaded
  Server 'SOA-C30API01': 5 services loaded
  ...
Successfully loaded 10 servers with topology data
```

When topology is missing or empty:
```
Cluster or topology is null, no remote machines will be loaded
```
or
```
No servers found in topology for cluster 'SO30'
```

---

## Service Naming

### Service Identifier Format
```
{ServerName}-{ServiceInternalName}
```

**Examples:**
- `SOA-C30COR01-VC` (Virtual Cluster)
- `SOA-C30API01-API` (API Website)
- `SOA-C30WEB01-Agent` (Agent)

**Fallback (if InternalName is empty):**
```
{ServerName}-{DisplayNameWithoutSpaces}
```

---

## UI Behavior

### With Topology
- All servers from `servers.json` appear in the tree
- Each server shows its configured services as children
- Users can expand servers to see available services
- Clicking a service opens a log monitoring tab

### Without Topology
- No servers appear in the tree
- No error messages displayed to user
- Application continues to function normally
- Debug logs indicate missing topology

---

## Integration Points

### Data Source
- **File**: `C:\XceleratorTool\Resources\servers.json`
- **Loaded by**: `PanelViewModel.LoadAndMapTopology()`
- **Mapped to**: `cluster.Topology` property

### View Model
- **Class**: `LiveLogMonitorViewModel`
- **Method**: `InitializeRemoteMachines()`
- **Called from**: Constructor (during view model initialization)

### UI Binding
- **Property**: `RemoteMachines` (ObservableCollection)
- **Bound to**: TreeView or similar hierarchical control
- **Selection**: `SelectedRemoteMachine` property

---

## Troubleshooting

### No Servers Appearing

**Check:**
1. Does `servers.json` exist at `C:\XceleratorTool\Resources\servers.json`?
2. Does the JSON structure match the expected format?
3. Does the cluster name in `cluster.json` match names in `servers.json`?
4. Check debug output for error messages

**Debug Output to Look For:**
```
Topology file not found: C:\XceleratorTool\Resources\servers.json
Failed to load topology
Cluster or topology is null, no remote machines will be loaded
```

### Services Not Appearing

**Check:**
1. Does the server in `servers.json` have a `children` array?
2. Are the service dictionaries properly formatted?
3. Check debug output: `Server 'X': 0 services loaded`

### Wrong Services Appearing

**Check:**
1. Verify `servers.json` content for that specific server
2. Ensure `children` array is correctly formatted
3. Verify topology was loaded after JSON file was updated

---

## Testing

### Manual Test Steps

1. **Create/Update** `servers.json` with test data
2. **Launch** the application
3. **Select** a cluster with topology data
4. **Navigate** to Live Log Monitor
5. **Verify** servers and services appear in the tree
6. **Expand** a server to see its services
7. **Click** a service to open a log tab

### Expected Results
- ✅ All servers from topology appear
- ✅ All services for each server appear as children
- ✅ Service display names are readable
- ✅ Clicking services opens log tabs
- ✅ No errors in debug output

---

## Summary

The `LiveLogMonitorViewModel` now:
- ✅ Dynamically loads machines from topology data
- ✅ Supports any infrastructure structure
- ✅ Handles missing topology gracefully
- ✅ Provides debug logging for troubleshooting
- ✅ Uses consistent naming from topology
- ✅ Requires no code changes for new servers/services

All infrastructure information is driven by `servers.json`, making it easy to maintain and update without touching code.
