# Quick Fix: Publishing with Resource Files

## The Problem
When you run `dotnet publish`, the JSON files aren't included in the output, so the published app can't load cluster configurations.

## The Solution

### AUTOMATIC FIX (Recommended)
Just publish normally - the files are now **automatically copied**:

```powershell
dotnet publish -c Release -r win-x64 --self-contained -o ./publish
```

Look for this message in the output:
```
Copied external resource files to .\publish\Resources\
```

That's it! The files are now in `./publish/Resources/` and the app will work.

---

## Alternative: Use the PowerShell Script

If you want more detailed feedback, run:

```powershell
.\Xcelerator\publish-with-resources.ps1
```

You'll see:
```
✅ PUBLISH COMPLETED SUCCESSFULLY!

Configuration files included:
  ✅ cluster.json
  ✅ servers.json
```

---

## What Changed Under the Hood

1. **Smart path detection** - app now checks:
   - First: `<AppDirectory>\Resources\`
   - Fallback: `C:\XceleratorTool\Resources\`

2. **Auto-copy on publish** - MSBuild target copies files automatically

3. **Diagnostic logging** - if files aren't found, you'll see exactly where it looked

4. **Error dialogs** - clear messages showing what's wrong and where files should be

---

## Verify It Works

After publishing:

1. **Check the files exist**:
   ```powershell
   dir ./publish/Resources/
   ```
   You should see:
   - cluster.json
   - servers.json

2. **Run the published app**:
   ```powershell
   ./publish/Xcelerator.exe
   ```

3. **If you see an error dialog**, it will show you exactly which paths were checked

---

## For Deployment

**To deploy to another machine:**

1. Publish with the automatic copy (done above)
2. Copy the entire `./publish` folder to the target machine
3. Run `Xcelerator.exe`

The app is now **fully portable** - no need for `C:\XceleratorTool\Resources\` on the target machine!

---

## Troubleshooting

**Problem**: "Unable to load cluster configuration" error dialog

**Solution**: The dialog shows you which paths were checked. Either:
- Put files in `<AppDirectory>\Resources\`
- OR put files in `C:\XceleratorTool\Resources\`

**Problem**: MSBuild target didn't copy files

**Check**: Do the files exist in `C:\XceleratorTool\Resources\`?
```powershell
dir C:\XceleratorTool\Resources\
```

If yes, the automatic copy should work. If not, copy them there first.

---

## Quick Commands Reference

```powershell
# Publish with automatic resource copy
dotnet publish -c Release -r win-x64 --self-contained -o ./publish

# Publish with detailed feedback
.\Xcelerator\publish-with-resources.ps1

# Check if files were copied
dir ./publish/Resources/

# Test the published app
./publish/Xcelerator.exe
```

That's it! Your published application now includes all necessary configuration files. 🎉
