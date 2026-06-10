using NugetForCrestronDrivers.Core.Models;

namespace NugetForCrestronDrivers.Core.Abstractions;

public interface INuGetPackageService
{
    Task<IReadOnlyList<PackageSearchResult>> SearchPackagesAsync(string feedUrl, string searchTerm, int take, CancellationToken cancellationToken = default);

    Task<string> DownloadPackageAsync(string feedUrl, string packageId, string version, string cacheDirectory, CancellationToken cancellationToken = default);
}
