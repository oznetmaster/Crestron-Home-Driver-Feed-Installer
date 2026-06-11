# Crestron Home Driver Feed Installer

Crestron Home Driver Feed Installer is a Windows desktop utility for finding, inspecting, caching, and uploading Crestron Home driver packages distributed through NuGet feeds.

It is designed for developers and advanced users who want a repeatable way to publish community drivers as NuGet packages and then install those drivers onto Crestron Home processors.

This repository also defines the **Crestron Home Driver NuGet Publishing Standard v1**, which the app uses as the basis for discovery and validation.

## Public release status

This project is intended for public GitHub release and community use.

Crestron Home Driver NuGet Publishing Standard v1 is **not** an official Crestron product, specification, certification program, or compatibility guarantee. It is an open source community standard for packaging and distributing Crestron Home driver `.pkg` files through NuGet.

Crestron and Crestron Home are trademarks or registered trademarks of Crestron Electronics, Inc. This project is not affiliated with, endorsed by, or sponsored by Crestron Electronics, Inc.

## What the app does

Crestron Home Driver Feed Installer helps you:

- search a NuGet feed for Crestron Home driver packages
- filter results to packages that match the v1 publishing standard
- inspect package metadata before use
- cache downloaded packages locally
- validate cached packages against the latest version on the feed
- highlight cached packages with updates available
- inspect `crestron-driver-package.json` content before upload
- manage saved processor connections and credentials
- upload the selected driver package to a Crestron Home processor

## Key features

- WPF desktop UI for Windows
- support for standard NuGet v3 feeds
- multi-select package search and cache management
- local package cache with update detection
- package detail and manifest inspection
- processor discovery support
- saved processor credentials
- direct upload workflow for cached driver packages
- About dialog and release metadata

## Requirements

### Runtime

- Windows
- .NET Desktop Runtime compatible with the release build
- network access to the target NuGet feed
- network access to the target Crestron Home processor

### Build from source

- Visual Studio 2026 or later with .NET desktop development tools
- .NET 10 SDK

## Getting started

### Option 1: install from a release

1. Download the latest `Crestron Home Driver Feed Installer.exe` installer from the repository Releases page.
2. Run the installer and complete setup.
3. Start Crestron Home Driver Feed Installer from the Start menu or desktop shortcut.
4. Enter or confirm the NuGet feed URL.
5. Search for a driver package.
6. Inspect and cache the package.
7. Select a processor and upload the staged driver package.

### Option 2: run a published executable directly

1. Download the published portable app files from a release artifact if provided.
2. Start `Crestron Home Driver Feed Installer.exe`.
3. Enter or confirm the NuGet feed URL.
4. Search for a driver package.
5. Inspect and cache the package.
6. Select a processor and upload the staged driver package.

### Option 3: build locally

```powershell
dotnet build .\CrestronHomeDriverFeedInstaller.slnx
```

The desktop app project is located at:

- `src/CrestronHomeDriverFeedInstaller.App`

The Windows MSI packaging project is located at:

- `installer/CrestronHomeDriverFeedInstaller.Setup.wixproj`

The Windows bootstrapper project is located at:

- `installer/CrestronHomeDriverFeedInstaller.Bootstrapper.wixproj`

## Release packaging

Public GitHub releases should publish `Crestron Home Driver Feed Installer.exe` as the primary end-user asset.

The MSI packaging project:

- publishes the application for `win-x64`
- packages the framework-dependent published output into an MSI
- creates Start Menu and desktop shortcuts
- installs the application under Program Files

The bootstrapper project:

- builds the MSI
- checks for the required .NET Desktop Runtime
- installs the runtime when needed before installing the app MSI

For source-based release builds, build the bootstrapper project in the solution and publish `installer\bin\Release\Crestron Home Driver Feed Installer.exe` as the primary release asset. The companion `installer\bin\Release\Crestron Home Driver Feed Installer.msi` can also be attached for advanced/manual installation scenarios.

The Windows installer project is located at:

- `installer/CrestronHomeDriverFeedInstaller.Setup.wixproj`

To build the installer directly:

```powershell
dotnet build .\installer\CrestronHomeDriverFeedInstaller.Setup.wixproj
```

## Typical workflow

1. Configure the NuGet feed URL.
2. Search for a driver package.
3. Optionally limit results to v1-compliant packages.
4. Inspect one or more packages.
5. Review package details and `crestron-driver-package.json`.
6. Select or discover a processor.
7. Upload the selected cached package to the processor.
8. Use the cached package update indicators to refresh outdated packages later.

## Repository structure

- `src/CrestronHomeDriverFeedInstaller.App` - WPF desktop application
- `src/CrestronHomeDriverFeedInstaller.Core` - package, cache, discovery, credential, and upload services
- `installer` - WiX-based MSI installer project for release packaging
- `LICENSE` - MIT license for this repository

## Crestron Home Driver NuGet Publishing Standard v1

This repository includes the packaging standard used by the app for package discovery and validation.

Read the full specification here:

- [Crestron Home Driver NuGet Publishing Standard v1](docs/standard/crestron-home-driver-nuget-publishing-standard-v1.md)

At a high level, the v1 standard requires:

- a `CrestronHomeDriver` package type
- required tags for Crestron Home driver discovery
- exactly one root `.pkg` payload
- a root `crestron-driver-package.json` manifest
- no conventional `lib/` DLL payload
- inclusion of `README.md` and `LICENSE`

## License

This repository is licensed under the MIT License. See [LICENSE](LICENSE).
