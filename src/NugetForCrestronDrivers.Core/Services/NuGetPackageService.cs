using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NugetForCrestronDrivers.Core.Abstractions;
using NugetForCrestronDrivers.Core.Models;

namespace NugetForCrestronDrivers.Core.Services;

public sealed class NuGetPackageService : INuGetPackageService
{
    public async Task<IReadOnlyList<PackageSearchResult>> SearchPackagesAsync(string feedUrl, string searchTerm, int take, CancellationToken cancellationToken = default)
    {
        var repository = CreateRepository(feedUrl);
        var searchResource = await repository.GetResourceAsync<PackageSearchResource>(cancellationToken);
        var results = await searchResource.SearchAsync(searchTerm, new SearchFilter(includePrerelease: false), skip: 0, take, NullLogger.Instance, cancellationToken);

        return results
            .Select(metadata => new PackageSearchResult
            {
                Id = metadata.Identity.Id,
                Version = metadata.Identity.Version.ToNormalizedString(),
                Description = metadata.Description ?? string.Empty,
                Authors = metadata.Authors ?? string.Empty,
                DownloadCount = metadata.DownloadCount,
                HasPackageTypes = false
            })
            .OrderBy(result => result.Id)
            .ToArray();
    }

    public async Task<string> DownloadPackageAsync(string feedUrl, string packageId, string version, string cacheDirectory, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(cacheDirectory);

        var packagePath = Path.Combine(cacheDirectory, $"{packageId}.{version}.nupkg");
        if (File.Exists(packagePath))
        {
            return packagePath;
        }

        var repository = CreateRepository(feedUrl);
        var downloadResource = await repository.GetResourceAsync<DownloadResource>(cancellationToken);
        var packageIdentity = new PackageIdentity(packageId, NuGetVersion.Parse(version));
        var cache = new SourceCacheContext();
        var result = await downloadResource.GetDownloadResourceResultAsync(
            packageIdentity,
            new PackageDownloadContext(cache),
            SettingsUtility.GetGlobalPackagesFolder(Settings.LoadDefaultSettings(root: null)),
            NullLogger.Instance,
            cancellationToken);

        if (result.Status != DownloadResourceResultStatus.Available || result.PackageStream is null)
        {
            throw new InvalidOperationException($"Package '{packageId} {version}' could not be downloaded from '{feedUrl}'.");
        }

        await using var sourceStream = result.PackageStream;
        await using var targetStream = File.Create(packagePath);
        await sourceStream.CopyToAsync(targetStream, cancellationToken);
        await targetStream.FlushAsync(cancellationToken);

        return packagePath;
    }

    private static SourceRepository CreateRepository(string feedUrl)
    {
        var providers = Repository.Provider.GetCoreV3();
        return new SourceRepository(new PackageSource(feedUrl), providers);
    }
}
