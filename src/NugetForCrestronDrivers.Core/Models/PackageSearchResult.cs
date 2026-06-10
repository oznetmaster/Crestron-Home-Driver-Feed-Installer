namespace NugetForCrestronDrivers.Core.Models;

public sealed class PackageSearchResult
{
    public string Id { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Authors { get; init; } = string.Empty;

    public long? DownloadCount { get; init; }

    public bool HasPackageTypes { get; init; }
}
