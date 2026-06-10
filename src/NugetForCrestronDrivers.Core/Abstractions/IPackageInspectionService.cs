using NugetForCrestronDrivers.Core.Models;

namespace NugetForCrestronDrivers.Core.Abstractions;

public interface IPackageInspectionService
{
    Task<DriverPackageInfo> InspectPackageAsync(string packageId, string version, string packageArchivePath, string workingDirectory, CancellationToken cancellationToken = default);
}
