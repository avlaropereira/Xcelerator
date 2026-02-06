# Add Server Feature Documentation

## Overview
The **Add Server** feature allows users to dynamically add new servers to the topology configuration through a user-friendly dialog. When a server is added, it's automatically configured with the appropriate services based on its type and added to the correct cluster in the `servers.json` file.

## Feature Components

### 1. AddServerDialog
- **Location**: `Xcelerator\Views\AddServerDialog.xaml` and `AddServerDialog.xaml.cs`
- **Purpose**: Modal dialog for entering server names
- **Features**:
  - Dynamic title showing the target cluster name
  - Server name validation with detailed error messages
  - Confirmation dialog with server details before adding
  - Visual feedback during processing
  - Success/error notifications

### 2. ServerConfigManager
- **Location**: `Xcelerator\Services\ServerConfigManager.cs`
- **Purpose**: Manages server configuration operations
- **Key Methods**:
  - `ParseServerName()`: Extracts cluster code and server type from server name
  - `MapClusterCodeToName()`: Derives cluster name from server name and cluster code
  - `GetServerChildren()`: Returns service configuration based on server type
  - `ClusterExists()`: Checks if a cluster exists in the configuration
  - `AddServerToCluster()`: Adds server to the JSON configuration (creates cluster if needed)
  - `GetLogDirectoryPath()`: Generates the log directory path for a server

## Server Naming Convention

### Format
```
XXX-CY[Y]SSS##
```

Where:
- **XXX**: Site code (2-3 letters)
  - Examples: `TCA`, `TOA`, `SOA`, `TOB`, `SOB`, `TCB`
- **C**: Cluster prefix (always 'C')
- **Y[Y]**: Cluster number (1-2 digits)
  - Examples: `1`, `5`, `30`, `32`, `34`
- **SSS**: Server type (3 letters) - **MUST be a valid type**
  - `COR`: Core server
  - `API`: API server
  - `WEB`: Web server
  - `MED`: Media server
  - `IVR`: IVR server
  - `AGM`: Agent Manager server
  - `AGT`: Agent server
- **##**: Server instance number (1-2 digits)
  - Examples: `1`, `01`, `02`, `03`

### Examples
- `TCB-C1COR01`: Cluster 1, Core server, instance 01
- `TCB-C1COR1`: Cluster 1, Core server, instance 1
- `TCA-C34COR01`: Cluster 34, Core server, instance 01
- `TOB-C32API01`: Cluster 32, API server, instance 01
- `SOA-C30WEB01`: Cluster 30, Web server, instance 01
- `TCA-C5MED01`: Cluster 5, Media server, instance 01

### Validation Rules
The system validates that:
1. Server name matches the basic pattern `XXX-CY[Y]SSS##`
2. The server type (`SSS`) is one of the recognized types: **COR, API, WEB, MED, IVR, AGM, AGT**
3. If the server type is not recognized, the validation will fail

## Cluster Mapping

### Automatic Cluster Resolution
The system automatically derives cluster names from the server name:

**Logic:**
- Extracts the first 2 letters from the site code (XXX)
- Appends the cluster number from the cluster code (CY[Y])

**Examples:**

| Server Name | Site Code | Cluster Code | Cluster Name | Explanation |
|-------------|-----------|--------------|--------------|-------------|
| TCA-C1COR01 | TCA | C1 | TC1 | TC (from TCA) + 1 = TC1 |
| TCB-C1COR01 | TCB | C1 | TC1 | TC (from TCB) + 1 = TC1 |
| TOA-C34COR01 | TOA | C34 | TO34 | TO (from TOA) + 34 = TO34 |
| TOB-C32API01 | TOB | C32 | TO32 | TO (from TOB) + 32 = TO32 |
| SOA-C30WEB01 | SOA | C30 | SO30 | SO (from SOA) + 30 = SO30 |
| TCA-C5MED01 | TCA | C5 | TC5 | TC (from TCA) + 5 = TC5 |

**Note**: The cluster name is dynamically derived from each server name, ensuring consistency and flexibility.

## Server Type Configurations

### COR (Core) Servers
Services:
- Virtual Cluster (VC)
- File Server (FileServer)
- CoOp Service (CoOp)
- Survey Service (Surveys)
- FS Drive Publisher (FileServerSetUp)
- DBCWS (DBCWS)

### API Servers
Services:
- L7 Healthcheck (Not Available)
- Drone Service (Not Available)
- API Website (API)
- AutoSite (Not Available)
- DBCWS (DBCWS)

### WEB Servers
Services:
- Agent (Agent)
- Authentication Server (AuthorizationServer)
- Cache Site (CacheSite)
- inContact (inContact)
- inControl (inContact)
- Report Service (ReportService)
- Security (Security)
- WebScripting (WebScripting)
- DBCWS (DBCWS)

### MED (Media) Servers
Services:
- Virtual Cluster (VC)
- Media Server (MediaServer)
- CoOp Service (CoOp)
- Drone Service (DroneService)
- DBCWS (DBCWS)

### IVR/AGM/AGT Servers
Services:
- Virtual Cluster (VC)
- CoOp Service (CoOp)
- Drone Service (DroneService)
- DBCWS (DBCWS)

## Usage Flow

### 1. User Opens Dialog
```csharp
// From LiveLogMonitorView
var dialog = new AddServerDialog(viewModel.ClusterName)
{
    Owner = Window.GetWindow(this)
};
```

### 2. User Enters Server Name
Examples: 
- `TCB-C1COR01` (Cluster 1, Core server)
- `TCA-C34COR01` (Cluster 34, Core server)
- `TOA-C5MED01` (Cluster 5, Media server)

### 3. System Validates Input
- Checks format against regex pattern
- Extracts cluster code (e.g., `C34`) and server type (e.g., `COR`)
- Derives cluster name from server name (e.g., `TCA-C34COR01` → `TC34`)

### 4. Confirmation Dialog
Shows:
- Server Name: `TCA-C34COR01`
- Cluster: `TC34` (or `TC34 (NEW - will be created)` if cluster doesn't exist)
- Type: `COR`
- Log Path: `\\TCA-C34COR01\D$\Proj\LogFiles`

**Note**: If the cluster doesn't exist in servers.json, the system will automatically create it.

### 5. System Adds Server
- Loads `C:\XceleratorTool\Resources\servers.json`
- Derives cluster name from server name (e.g., `TCA-C34COR01` → `TC34`)
- Checks if cluster exists
  - If not, creates a new cluster section
- Creates server entry with appropriate services
- Adds server to the cluster's Servers array
- Saves updated JSON

### 6. Topology Reload (Automatic)
- The topology is automatically reloaded after successful server addition
- The server list is refreshed in the UI
- The new server becomes immediately available for log monitoring

## Log Directory Path

The log directory for each server follows this pattern:
```
\\{ServerName}\D$\Proj\LogFiles
```

Example:
```
\\TCA-C34COR01\D$\Proj\LogFiles
```

## Error Handling

### Invalid Server Name Format
```
Invalid server name format: ABC123

Expected format: XXX-CY[Y]SSS## where SSS is a valid server type (COR, API, WEB, MED, IVR, AGM, AGT)
Examples:
  • TCB-C1COR01 (Cluster 1, Core server)
  • TCA-C34COR01 (Cluster 34, Core server)
  • TOA-C32API01 (Cluster 32, API server)
  • SOA-C30WEB01 (Cluster 30, Web server)
  • TCA-C5MED01 (Cluster 5, Media server)
```

### Server Already Exists
```
Server 'TCA-C34COR01' already exists in cluster 'TO34'
```

### File Access Errors
```
Servers configuration file not found: C:\XceleratorTool\Resources\servers.json
```

## Extending the Feature

### Adding New Server Types
1. Add the server type to the valid types array in `ParseServerName()`:
```csharp
var validServerTypes = new[] { "COR", "API", "WEB", "MED", "IVR", "AGM", "AGT", "NEW" };
```

2. Add service configuration to `GetServerChildren()`:
```csharp
"NEW" => new List<Dictionary<string, string>>
{
    new Dictionary<string, string> { { "Service Name", "ServiceIdentifier" } },
    // Add more services...
}
```

### Adding New Cluster Ranges
The cluster name is now automatically derived from the server name, so no manual mapping is needed.

However, if you want to add custom logic for specific patterns, update `MapClusterCodeToName()`:
```csharp
public static string MapClusterCodeToName(string serverName, string clusterCode)
{
    // Extract site code prefix (first 2 chars)
    string sitePrefix = serverName.Substring(0, 2);
    string clusterNumber = clusterCode.Substring(1);

    // Custom logic for specific cases
    if (sitePrefix == "EU" && clusterNumber == "50")
    {
        return "EURO50"; // Special case
    }

    // Default: combine prefix + number
    return $"{sitePrefix}{clusterNumber}";
}
```

### Custom Validation Rules
Add validation in `AddServerDialog.OkButton_Click()`:
```csharp
// Example: Check if server name follows company naming standards
if (!ServerName.StartsWith("TCA-") && !ServerName.StartsWith("TOA-"))
{
    MessageBox.Show("Server must be from Toronto region", ...);
    return;
}
```

## Integration Points

### LiveLogMonitorViewModel
- **Method**: `ReloadTopology()`
- **Purpose**: Refreshes the remote machines list after adding a server
- **Triggered**: User chooses to reload after successful server addition

### PanelViewModel
- **Consideration**: May need to reload topology when switching clusters
- **Future Enhancement**: Auto-refresh on file change detection

### TopologyMapper
- **Integration**: Uses existing `LoadTopology()` method
- **Maintains**: Consistency with current topology loading mechanism

## JSON File Structure

### Before Adding Server
```json
{
  "ClusterListContainer": {
    "Clusters": [
      {
        "Name": "TC34",
        "Servers": [
          { "Name": "TOA-C34COR01", "children": [...] }
        ]
      }
    ]
  }
}
```

### After Adding TCA-C34COR01
```json
{
  "ClusterListContainer": {
    "Clusters": [
      {
        "Name": "TC34",
        "Servers": [
          { "Name": "TOA-C34COR01", "children": [...] },
          { 
            "Name": "TCA-C34COR01", 
            "children": [
              {"Virtual Cluster":"VC"},
              {"File Server": "FileServer"},
              {"CoOp Service": "CoOp"},
              {"Survey Service": "Surveys"},
              {"FS Drive Publisher": "FileServerSetUp"},
              {"DBCWS": "DBCWS"}
            ]
          }
        ]
      }
    ]
  }
}
```

## Best Practices

### For Users
1. Always verify the server name format before submitting
2. Review the confirmation dialog carefully
3. Choose to reload topology immediately for instant availability
4. Check the status bar for confirmation messages

### For Developers
1. Always validate input before processing
2. Provide clear, actionable error messages
3. Use consistent naming conventions
4. Keep JSON file structure consistent
5. Test with various server types and cluster configurations

## Testing Scenarios

### Valid Inputs
- `TCB-C1COR01` → TC1 cluster, COR type ✓
- `TCB-C1COR1` → TC1 cluster, COR type ✓
- `TCA-C34COR01` → TC34 cluster, COR type ✓
- `TOB-C32API01` → TO32 cluster, API type ✓
- `SOA-C30WEB01` → SO30 cluster, WEB type ✓
- `TCA-C5MED01` → TC5 cluster, MED type ✓

### Invalid Inputs
- `INVALID` → Format error (doesn't match pattern)
- `TCA-C34XXX01` → Invalid server type (XXX not recognized)
- `TOA-C34COR01` → Already exists (if duplicate)

### Edge Cases
- Empty input
- Special characters
- Mixed case (should normalize to uppercase)
- Extra spaces (should trim)

## Future Enhancements

1. **Bulk Import**: Import multiple servers from CSV
2. **Server Removal**: Delete servers from configuration
3. **Service Customization**: Allow custom service configurations
4. **Validation Rules**: Configurable validation patterns
5. **Auto-Discovery**: Detect servers automatically from network
6. **Backup/Restore**: JSON file versioning and rollback
7. **Change Tracking**: Log all configuration changes
8. **Network Validation**: Verify server accessibility before adding

## Troubleshooting

### Server Not Appearing After Addition
1. Check if topology reload was completed
2. Verify JSON file was updated correctly
3. Restart the application if needed

### JSON File Corruption
1. Backup location: `C:\XceleratorTool\Resources\servers.json.bak` (implement if needed)
2. Validate JSON structure using online validators
3. Compare with known good configuration

### Permission Errors
1. Ensure write access to `C:\XceleratorTool\Resources\`
2. Run application with appropriate privileges
3. Check antivirus/security software settings
