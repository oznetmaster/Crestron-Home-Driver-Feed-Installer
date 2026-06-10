# CrestronHomeDriverFeedInstaller

CrestronHomeDriverFeedInstaller is a utility for discovering and installing Crestron Home driver packages that are distributed through feed-based package sources.

This repository also defines the authoritative **v1 custom publishing standard** for Crestron Home driver NuGet packages.

Crestron Home Driver NuGet Publishing Standard v1 is **not** an official Crestron product, specification, or compatibility program. It is an open source packaging standard created to facilitate community distribution and discovery of Crestron Home drivers through NuGet.

## Crestron Home Driver NuGet Publishing Standard v1

### Purpose

This standard defines a dedicated NuGet package category for Crestron Home driver distribution.

These packages are **not** general-purpose .NET library packages. They are distribution wrappers for a final Crestron driver `.pkg` artifact.

### Category Name

- **Category**: Crestron Home driver NuGet package
- **Package type**: `CrestronHomeDriver`
- **Platform**: Crestron Home
- **Payload type**: `.pkg`

### Design Goals

A compliant package must be:

- searchable from NuGet feeds using package metadata
- easy to validate programmatically before installation
- unambiguous about containing a deployable Crestron driver payload
- free of normal `lib/` DLL payloads used by conventional NuGet packages

## Required Package Metadata

Every published Crestron Home driver NuGet package must include all of the following metadata.

### Required NuGet metadata

- `PackageType`: `CrestronHomeDriver`
- `PackageReadmeFile`: `README.md`
- `PackageLicenseFile`: `LICENSE`
- `PackageTags` containing all required tags listed below
- `Description` clearly stating that the package contains a final `.pkg` Crestron driver payload
- `RepositoryUrl`
- `RepositoryType=git`
- `PackageProjectUrl`
- `PublishRepositoryUrl=true`
- `Authors`
- `Company`

### Required tags

Every package must include these tags:

- `crestron`
- `crestron-home`
- `driver`
- `pkg`

Additional tags are allowed for device class, vendor, or protocol, for example:

- `thermostat`
- `weather`
- `shade`
- `lighting`
- vendor names such as `somfy`, `wiser`, or `overkiz`

### Recommended package identity

Recommended package ID format:

- `CrestronHomeDriver.<Vendor>.<DriverName>`

Examples:

- `CrestronHomeDriver.Wiser.WiserHeat`
- `CrestronHomeDriver.WeatherLinkLive.WeatherStation`
- `CrestronHomeDriver.Overkiz.Shades`

Existing packages may retain a stable package ID if needed, but new packages should prefer the recommended convention.

## Required Package Payload Rules

A compliant Crestron Home driver package must satisfy all of the following rules.

### Payload content

- must contain **exactly one** final driver `.pkg` file
- that `.pkg` file must be placed at the **package root**
- must **not** contain driver DLL payloads in `lib/`
- must **not** contain normal library-target output such as `lib/net472/*.dll`
- may include documentation files such as `README.md` and `LICENSE`
- may include the standard package metadata files produced by NuGet

### Dependency rules

Because this package category is a delivery wrapper and not a code-consumption library:

- package dependency generation should be suppressed
- consumers should install and use the contained `.pkg`, not reference the NuGet package as a .NET assembly dependency

## Required Manifest File

Every package must include a root manifest file named:

- `crestron-driver-package.json`

This manifest is required for deterministic inspection and validation.

### Required manifest fields

The manifest must contain at least these fields:

```json
{
  "standardVersion": "1.0",
  "packageType": "CrestronDriver",
  "driverPlatform": "Crestron Home",
  "payloadType": "pkg",
  "payloadFile": "ExampleDriver.pkg",
  "containsManagedDllPayload": false,
	"packageId": "CrestronHomeDriver.Vendor.DriverName",
  "packageVersion": "1.2.3",
  "displayName": "Example Driver",
  "vendor": "Example Vendor",
  "driverCategory": "Thermostat"
}
```

### Manifest requirements

- `standardVersion` must be `1.0` for this standard
- `packageType` must be `CrestronHomeDriver`
- `driverPlatform` must be `Crestron Home`
- `payloadType` must be `pkg`
- `payloadFile` must match the actual root `.pkg` file name
- `containsManagedDllPayload` must be `false`
- `packageId` must match the NuGet package ID
- `packageVersion` must match the published NuGet version

Additional fields may be added in later revisions as long as the required fields remain valid.

## Required Search Semantics

A search tool targeting this package category should use a two-stage filter.

### Stage 1: searchable metadata filter

Search should prefer packages that satisfy all of the following:

- package type is `CrestronHomeDriver`
- tags include `crestron`, `crestron-home`, `driver`, and `pkg`
- package description indicates the package contains a final `.pkg` payload

Optional narrowing is allowed:

- title or package ID contains `Crestron`

### Stage 2: package validation

Before presenting a package as installable, the client should validate that:

- the package contains `crestron-driver-package.json`
- the package contains exactly one root `.pkg`
- the manifest matches the actual payload
- no driver DLL payload exists in `lib/`

## Versioning Rules

- NuGet package version must match the release version for the packaged driver
- if Git tags use a `v` prefix, the NuGet version should be the same semantic version without the prefix
- the packaged `.pkg` must be built from the corresponding Release build output

## Release Rules

A compliant publishing workflow should:

1. build the driver in `Release`
2. produce the final `.pkg`
3. create the NuGet package from that `.pkg`
4. include the required manifest, README, and LICENSE
5. publish the NuGet package
6. keep the `.pkg` available as a GitHub release asset when GitHub releases are used

## Compliance Checklist

A package is compliant with Crestron Driver NuGet Publishing Standard v1 only if all of the following are true:

- [ ] `PackageType` is `CrestronDriver`
- [ ] required tags are present
- [ ] package contains exactly one root `.pkg`
- [ ] package contains no `lib/` DLL payload
- [ ] package contains `README.md`
- [ ] package contains `LICENSE`
- [ ] package contains `crestron-driver-package.json`
- [ ] manifest values match the actual package contents
- [ ] package was built from Release output

## Notes for This Repository

CrestronHomeDriverFeedInstaller should use this standard as the basis for package discovery, inspection, and installation behavior.

In particular, search results should ultimately favor packages that are compliant with this standard rather than generic NuGet packages.
