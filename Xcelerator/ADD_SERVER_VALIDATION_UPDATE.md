# Add Server Validation Update - Summary

## Changes Made

### Issue
The original validation was too strict and rejected valid server names like `TCB-C1COR01` because it required exactly 2 digits for the cluster number. The validation logic needed to be more flexible and focus on recognizing valid server types.

### Solution
Updated the server name parsing and validation to:
1. **Accept flexible cluster numbers** (1-2 digits instead of requiring exactly 2)
2. **Validate based on server type recognition** (COR, API, WEB, MED, IVR, AGM, AGT)
3. **Add support for MED (Media) server type**
4. **Update cluster name mapping** to support single-digit clusters

## Modified Files

### 1. `ServerConfigManager.cs`

#### ParseServerName() Method
**Before:**
```csharp
var pattern = @"^[A-Z]{2,3}-C(\d{2})([A-Z]{3})\d{2}$";
```

**After:**
```csharp
// Now accepts 1-2 digits for cluster number and server instance
var pattern = @"^[A-Z]{2,3}-C(\d{1,2})([A-Z]{3})\d{1,2}$";

// Added validation for recognized server types
var validServerTypes = new[] { "COR", "API", "WEB", "MED", "IVR", "AGM", "AGT" };
if (validServerTypes.Contains(serverType))
{
    return (clusterCode, serverType, true);
}
```

**Key Changes:**
- `\d{2}` changed to `\d{1,2}` for cluster number
- `\d{2}` changed to `\d{1,2}` for server instance number
- Added explicit validation for server types
- Returns invalid if server type is not recognized

#### MapClusterCodeToName() Method
**Before:**
```csharp
if (clusterCode.StartsWith("C") && clusterCode.Length == 3)
{
    string number = clusterCode.Substring(1);
    int clusterNum = int.Parse(number);
    // Only handled 30-39 range
}
```

**After:**
```csharp
if (clusterCode.StartsWith("C") && clusterCode.Length >= 2)
{
    string number = clusterCode.Substring(1);
    if (int.TryParse(number, out int clusterNum))
    {
        // Single digit clusters (1-9)
        if (clusterNum >= 1 && clusterNum <= 9)
        {
            return $"TO{number}";
        }
        // 30-39 range
        if (clusterNum >= 30 && clusterNum <= 39) { ... }
    }
}
```

**Key Changes:**
- Changed `Length == 3` to `Length >= 2` to support single-digit clusters
- Added range for clusters 1-9 (maps to TO1-TO9)
- Added safer parsing with `TryParse`

#### GetServerChildren() Method
**Added MED Server Type:**
```csharp
"MED" => new List<Dictionary<string, string>>
{
    new Dictionary<string, string> { { "Virtual Cluster", "VC" } },
    new Dictionary<string, string> { { "Media Server", "MediaServer" } },
    new Dictionary<string, string> { { "CoOp Service", "CoOp" } },
    new Dictionary<string, string> { { "Drone Service", "DroneService" } },
    new Dictionary<string, string> { { "DBCWS", "DBCWS" } }
}
```

### 2. `AddServerDialog.xaml.cs`

**Updated Error Message:**
```csharp
MessageBox.Show(
    $"Invalid server name format: {ServerName}\n\n" +
    "Expected format: XXX-CY[Y]SSS##\n" +
    "Where SSS must be a valid server type: COR, API, WEB, MED, IVR, AGM, or AGT\n\n" +
    "Examples:\n" +
    "  • TCB-C1COR01 (Cluster 1, Core server)\n" +
    "  • TCA-C34COR01 (Cluster 34, Core server)\n" +
    "  • TOA-C32API01 (Cluster 32, API server)\n" +
    "  • SOA-C30WEB01 (Cluster 30, Web server)\n" +
    "  • TCA-C5MED01 (Cluster 5, Media server)",
    ...
);
```

### 3. `ADD_SERVER_FEATURE.md`

Updated documentation to reflect:
- New flexible format: `XXX-CY[Y]SSS##`
- Single-digit cluster support
- MED server type addition
- Updated examples showing valid variations
- Expanded cluster mapping table
- Updated validation rules section

## New Valid Server Name Patterns

### Previously Invalid (Now Valid)
- `TCB-C1COR01` - Single digit cluster ✓
- `TCA-C5MED01` - Single digit cluster + MED type ✓
- `TOA-C1API1` - Single digit cluster + single digit instance ✓

### Still Valid
- `TCA-C34COR01` - Standard format ✓
- `TOB-C32API01` - Standard format ✓
- `SOA-C30WEB01` - Standard format ✓

### Invalid Examples
- `TCA-C34XXX01` - Invalid server type (XXX) ✗
- `INVALID-FORMAT` - Doesn't match pattern ✗
- `TCA-COR01` - Missing cluster number ✗

## Validation Logic Flow

```
1. Input: "TCB-C1COR01"
   ↓
2. Regex Match: ^[A-Z]{2,3}-C(\d{1,2})([A-Z]{3})\d{1,2}$
   ✓ TCB (3 letters) - site code
   ✓ C1 (1 digit) - cluster code
   ✓ COR (3 letters) - server type
   ✓ 01 (2 digits) - instance
   ↓
3. Server Type Validation
   ✓ "COR" is in validServerTypes array
   ↓
4. Result: Valid
   - ClusterCode: "C1"
   - ServerType: "COR"
   - IsValid: true
```

## Supported Server Types

| Type | Description | Services Configured |
|------|-------------|-------------------|
| COR  | Core Server | VC, FileServer, CoOp, Surveys, FileServerSetUp, DBCWS |
| API  | API Server  | L7 Healthcheck, Drone Service, API, AutoSite, DBCWS |
| WEB  | Web Server  | Agent, Auth, Cache, inContact, inControl, Reports, Security, WebScripting, DBCWS |
| MED  | Media Server | VC, MediaServer, CoOp, DroneService, DBCWS |
| IVR  | IVR Server  | VC, CoOp, DroneService, DBCWS |
| AGM  | Agent Manager | VC, CoOp, DroneService, DBCWS |
| AGT  | Agent Server | VC, CoOp, DroneService, DBCWS |

## Cluster Mapping Examples

| Input | Cluster Code | Cluster Name | Status |
|-------|--------------|--------------|--------|
| TCB-C1COR01 | C1 | TO1 | ✓ Valid |
| TCA-C5MED01 | C5 | TO5 | ✓ Valid |
| SOA-C30WEB01 | C30 | SO30 | ✓ Valid (exception) |
| TCA-C34COR01 | C34 | TO34 | ✓ Valid |
| TOB-C32API01 | C32 | TO32 | ✓ Valid |

## Testing Recommendations

### Test Cases to Verify
1. **Single-digit clusters:**
   - `TCB-C1COR01` → Should map to TO1
   - `TCA-C5MED01` → Should map to TO5

2. **Double-digit clusters:**
   - `TCA-C34COR01` → Should map to TO34
   - `TOB-C32API01` → Should map to TO32

3. **MED server type:**
   - `TCA-C5MED01` → Should configure with Media Server services

4. **Invalid server types:**
   - `TCA-C34XXX01` → Should be rejected
   - `TCA-C34NEW01` → Should be rejected (unless NEW is added)

5. **Single-digit instance numbers:**
   - `TCB-C1COR1` → Should be valid
   - `TCA-C34API1` → Should be valid

## Extension Guide

### To Add a New Server Type (e.g., "DBM" for Database Manager):

1. **Update ParseServerName() validation:**
```csharp
var validServerTypes = new[] { "COR", "API", "WEB", "MED", "IVR", "AGM", "AGT", "DBM" };
```

2. **Add service configuration:**
```csharp
"DBM" => new List<Dictionary<string, string>>
{
    new Dictionary<string, string> { { "Database Manager", "DBManager" } },
    new Dictionary<string, string> { { "Backup Service", "BackupService" } },
    new Dictionary<string, string> { { "DBCWS", "DBCWS" } }
}
```

3. **Update documentation** in `ADD_SERVER_FEATURE.md`

### To Add a New Cluster Range (e.g., 10-19 for development):

```csharp
// Development clusters (DE) use 10-19 range
if (clusterNum >= 10 && clusterNum <= 19)
{
    return $"DE{number}";
}
```

## Benefits of This Update

1. ✅ **More Flexible** - Accepts valid variations in server naming
2. ✅ **Type-Focused** - Validation centers on recognizing server types
3. ✅ **Extensible** - Easy to add new server types by updating one array
4. ✅ **Better Error Messages** - Clearly states what server types are valid
5. ✅ **Support for Small Clusters** - Single-digit clusters now work
6. ✅ **Consistency** - Server instance numbers can also be 1-2 digits

## Backward Compatibility

✅ **All previously valid server names remain valid**
- `TCA-C34COR01` still works
- `TOB-C32API01` still works
- `SOA-C30WEB01` still works

✅ **No breaking changes to existing functionality**
- JSON structure remains the same
- Service configurations remain the same
- Existing servers in JSON are unaffected

## Build Status

✅ Build successful
✅ No compilation errors
✅ All changes integrated
