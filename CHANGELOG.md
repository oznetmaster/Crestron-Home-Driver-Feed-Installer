# Changelog

## [1.0.9](https://github.com/oznetmaster/Crestron-Home-Driver-Feed-Installer/releases/tag/v1.0.9) - 2026-11-02

- Update SSH.NET from 2025.1.0 to upstream 2026.0.0, including its SSH and SCP security fixes. The installer uses SSH.NET for SFTP uploads to Crestron Home processors.
- Require System.Security.Cryptography.Pkcs 10.0.13 and .NET Desktop Runtime 10.0.14 to include the applicable security fixes.
- Detect the installed x64 .NET 10 Desktop Runtime and install the required patch when needed. Keep the application's minimum runtime aligned with the bootstrapper prerequisite.
- Correct the MSI and bootstrapper versions, which previously remained at 1.0.6, and align them with application version 1.0.9.
- Verify application and installer versions, published dependencies, and compiled runtime detection during release builds.
- Use the pinned shared Cake build component and queue concurrent release runs.

## [1.0.8](https://github.com/oznetmaster/Crestron-Home-Driver-Feed-Installer/releases/tag/v1.0.8) - 2026-06-12

- Fix cached package updates and keep one cached version per package ID.
- Reset the Entries, Manifest and README views to the top when loading a different package.
