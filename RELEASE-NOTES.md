# Crestron Home Driver Feed Installer 1.0.9

This patch updates the Windows installer's security dependencies and corrects its runtime detection and version information.

- Upgrade SSH.NET to the official upstream 2026.0.0 release, which includes security fixes. Driver packages continue to upload over SFTP.
- Update the PKCS dependency to 10.0.13 and require the x64 .NET 10 Desktop Runtime 10.0.14 or a later compatible .NET 10 patch. The bootstrapper installs the runtime when needed; Internet access is required for that download.
- Align the application, MSI and bootstrapper at 1.0.9. Earlier public installers reported MSI/bootstrapper version 1.0.6 even when the application was newer.
- Add release-build checks for version consistency, published dependencies and the compiled runtime prerequisite.

Download and run **Crestron Home Driver Feed Installer.exe** to update an existing installation. The installer retains its existing upgrade identities. This release does not change driver package discovery or the processor upload workflow.

See the [changelog](https://github.com/oznetmaster/Crestron-Home-Driver-Feed-Installer/blob/master/CHANGELOG.md) for the full list of changes.
