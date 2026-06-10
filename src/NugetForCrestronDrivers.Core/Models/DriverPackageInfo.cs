namespace NugetForCrestronDrivers.Core.Models;

public sealed class DriverPackageInfo
{
    public string PackageId { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;

    public string PackageArchivePath { get; init; } = string.Empty;

    public string DriverPackagePath { get; init; } = string.Empty;

    public string DriversJsonContent { get; init; } = string.Empty;

    public IReadOnlyList<string> Entries { get; init; } = Array.Empty<string>();
}
