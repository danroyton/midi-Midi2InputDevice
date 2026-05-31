# Creating a Release – MidiController

This guide describes how to publish a new release on GitHub.
There are two approaches: **automatically via GitHub Actions** (recommended) or **manually**.

---

## Table of Contents

- [Preparation](#preparation)
- [Option A: Automatically via GitHub Actions](#option-a-automatically-via-github-actions)
- [Option B: Manually (without GitHub Actions)](#option-b-manually-without-github-actions)
- [Setting up the GitHub Actions Workflow](#setting-up-the-github-actions-workflow)
- [Release Checklist](#release-checklist)
- [Versioning Scheme](#versioning-scheme)

---

## Preparation

1. **Determine the version** – New version number following the scheme `v0.3.0` (see [Versioning Scheme](#versioning-scheme)).

2. **Update the CHANGELOG** (if present):
   ```markdown
   ## [0.3.0] – 2025-01-15
   ### New
   - Full trigger editor
   - MIDI output in pre/post phase
   - Single-EXE self-contained release
   ```

3. **Branch is `main` and all changes are committed:**
   ```powershell
   git status          # no open changes
   git log --oneline -5
   ```

4. **Verify the build locally:**
   ```powershell
   dotnet build Midi2InputDevice.slnx -c Release
   dotnet test
   ```

5. **Test publish:**
   ```powershell
   dotnet publish MidiController.Frontend\MidiController.Frontend.csproj `
     /p:PublishProfile=SingleFile -c Release
   
   # Verify the EXE is present:
   dir MidiController.Frontend\bin\Release\publish\
   # Should contain MidiController.Frontend.exe
   ```

---

## Option A: Automatically via GitHub Actions

### Step 1 – Set up the GitHub Actions Workflow

If not already present: [Setting up the GitHub Actions Workflow](#setting-up-the-github-actions-workflow).

### Step 2 – Create and push the Git tag

```powershell
# Create tag locally
git tag v0.3.0 -m "Release v0.3.0: Full trigger editor, MIDI output, Single-EXE"

# Push tag to GitHub (triggers the Actions workflow)
git push origin v0.3.0
```

### Step 3 – Monitor GitHub Actions

1. Open the repository on GitHub.
2. Go to the **Actions** tab → click the running **"Build & Release"** workflow.
3. Once complete, the release will appear automatically under **Releases**.

### Step 4 – Verify the release on GitHub

1. Open the **Releases** tab on GitHub.
2. Click the new release.
3. Check:
   - Correct tag and title (e.g. `v0.3.0`)
   - File attachment `MidiController-v0.3.0-win-x64.zip` is present
   - ZIP contains `MidiController.Frontend.exe`, `appsettings.json`, `appsettings.backend.json`

4. Optional: Manually edit the release description (click **Edit**).

---

## Option B: Manually (without GitHub Actions)

### Step 1 – Create the Single-File EXE

```powershell
cd C:\Users\larsh\source\repos\danroyton\Midi2InputDevice

dotnet publish MidiController.Frontend\MidiController.Frontend.csproj `
  /p:PublishProfile=SingleFile -c Release
```

Output: `MidiController.Frontend\bin\Release\publish\`

### Step 2 – Pack the release ZIP

```powershell
$version = "v0.3.0"
$publishDir = "MidiController.Frontend\bin\Release\publish"
$zipName    = "MidiController-$version-win-x64.zip"

# Pack all required files into the ZIP
Compress-Archive -Force `
  -Path "$publishDir\MidiController.Frontend.exe", `
        "$publishDir\appsettings.json", `
        "$publishDir\appsettings.backend.json" `
  -DestinationPath $zipName

Write-Host "ZIP created: $zipName"
```

### Step 3 – Create and push the Git tag

```powershell
git tag v0.3.0 -m "Release v0.3.0"
git push origin v0.3.0
```

### Step 4 – Create the GitHub Release

1. Open GitHub → Repository → **Releases** tab → **[Draft a new release]**

2. **"Choose a tag"**: Select the tag `v0.3.0` (the one just pushed).

3. Enter a **"Release title"**, e.g.:
   `v0.3.0 – Full Trigger Editor, MIDI Output, Single-EXE`

4. Enter a **description** (Markdown):
   ```markdown
   ## What's new in v0.3.0?
   
   - ✅ Full trigger editor (pre/post assignments, condition blocks, ELSE branches)
   - ✅ MIDI output in pre/post phase (NoteOn, NoteOff, CC, ProgramChange, PitchBend)
   - ✅ Single-EXE self-contained release (no .NET installation required)
   - ✅ System tray blinks only on actual MIDI activity
   - ✅ Keyboard test view
   
   ## Installation
   
   1. Download and extract the ZIP
   2. Launch `MidiController.Frontend.exe`
   3. No installer, no .NET runtime required
   
   ## Notes
   
   - Requires Windows 10/11 (64-bit)
   - On first launch, confirm the SmartScreen warning if it appears
   ```

5. **"Attach binaries"**: Upload the ZIP file created earlier
   (`MidiController-v0.3.0-win-x64.zip`).

6. Enable **"Set as the latest release"**.

7. Click **[Publish release]**.

---

## Setting up the GitHub Actions Workflow

If the workflow `.github/workflows/release.yml` does not yet exist, create this file:

**File path:** `.github/workflows/release.yml`

```yaml
name: Build & Release

on:
  push:
    tags:
      - 'v*.*.*'

jobs:
  build-and-release:
    runs-on: windows-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Set up .NET 10 SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore Midi2InputDevice.slnx

      - name: Build
        run: dotnet build Midi2InputDevice.slnx -c Release --no-restore

      - name: Run tests
        run: dotnet test Midi2InputDevice.slnx -c Release --no-build --verbosity normal

      - name: Single-File Publish
        run: |
          dotnet publish MidiController.Frontend/MidiController.Frontend.csproj `
            /p:PublishProfile=SingleFile -c Release

      - name: Create release ZIP
        shell: pwsh
        run: |
          $tag     = "${{ github.ref_name }}"
          $pubDir  = "MidiController.Frontend/bin/Release/publish"
          $zipName = "MidiController-$tag-win-x64.zip"
          Compress-Archive -Force `
            -Path "$pubDir/MidiController.Frontend.exe", `
                  "$pubDir/appsettings.json", `
                  "$pubDir/appsettings.backend.json" `
            -DestinationPath $zipName
          echo "ZIP_NAME=$zipName" >> $env:GITHUB_ENV

      - name: Create GitHub Release and upload ZIP
        uses: softprops/action-gh-release@v2
        with:
          files: ${{ env.ZIP_NAME }}
          generate_release_notes: true
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

### Activate the workflow

```powershell
# Check in the workflow file
git add .github/workflows/release.yml
git commit -m "ci: add GitHub Actions release workflow"
git push origin main
```

From now on, every tag matching `v*.*.*` triggers an automatic build and release.

---

## Release Checklist

Go through this before every release:

- [ ] `main` is up to date (`git pull`)
- [ ] No uncommitted changes (`git status`)
- [ ] Build successful (`dotnet build -c Release`)
- [ ] Tests passing (`dotnet test`)
- [ ] Publish successful, EXE starts locally
- [ ] CHANGELOG / release description updated
- [ ] Git tag created and pushed
- [ ] Release verified on GitHub (attachment present, download link works)
- [ ] README.md shows the correct current version

---

## Versioning Scheme

This project uses [Semantic Versioning](https://semver.org/):

```
v MAJOR . MINOR . PATCH
  │        │       └── Bug fix / minor corrections
  │        └────────── New feature, backwards compatible
  └─────────────────── Breaking API change / major relaunch
```

Examples:
- `v0.3.0` → new feature release (trigger editor)
- `v0.3.1` → bug fix for v0.3.0
- `v0.4.0` → next feature release (virtual MIDI ports)
- `v1.0.0` → first stable Windows service release

Always set tags in the format `v0.3.0` (with `v` prefix) – the Actions workflow expects this format.
