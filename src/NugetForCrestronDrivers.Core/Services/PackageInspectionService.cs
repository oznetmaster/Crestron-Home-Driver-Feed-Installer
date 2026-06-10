using System.IO.Compression;
using System.Text;
using NugetForCrestronDrivers.Core.Abstractions;
using NugetForCrestronDrivers.Core.Models;

namespace NugetForCrestronDrivers.Core.Services;

public sealed class PackageInspectionService : IPackageInspectionService
{
    public async Task<DriverPackageInfo> InspectPackageAsync(string packageId, string version, string packageArchivePath, string workingDirectory, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(workingDirectory);

        using var packageArchive = ZipFile.OpenRead(packageArchivePath);
        var pkgEntry = packageArchive.Entries.FirstOrDefault(entry => entry.FullName.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase));
        if (pkgEntry is null)
        {
            throw new InvalidOperationException("The selected NuGet package does not contain a .pkg driver package.");
        }

        var driverPackagePath = Path.Combine(workingDirectory, pkgEntry.Name);
        await using (var pkgStream = pkgEntry.Open())
        await using (var output = File.Create(driverPackagePath))
        {
            await pkgStream.CopyToAsync(output, cancellationToken);
        }

        string driversJsonContent;
        using (var driverArchive = ZipFile.OpenRead(driverPackagePath))
        {
            var driversJsonEntry = driverArchive.Entries.FirstOrDefault(entry => entry.FullName.EndsWith("drivers.json", StringComparison.OrdinalIgnoreCase));
            if (driversJsonEntry is null)
            {
                driversJsonContent = "drivers.json was not found in the .pkg archive.";
            }
            else
            {
                using var reader = new StreamReader(driversJsonEntry.Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                driversJsonContent = await reader.ReadToEndAsync(cancellationToken);
            }
        }

        return new DriverPackageInfo
        {
            PackageId = packageId,
            Version = version,
            PackageArchivePath = packageArchivePath,
            DriverPackagePath = driverPackagePath,
            DriversJsonContent = driversJsonContent,
            Entries = packageArchive.Entries.Select(entry => entry.FullName).OrderBy(name => name).ToArray()
        };
    }
}
