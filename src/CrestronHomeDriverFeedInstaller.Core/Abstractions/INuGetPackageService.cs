using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.Core.Abstractions;

public interface INuGetPackageService
	{
	Task<IReadOnlyList<PackageSearchResult>> SearchPackagesAsync (string feedUrl, string searchTerm, int take, bool onlyV1Compliant, CancellationToken cancellationToken = default);

	Task<string> DownloadPackageAsync (string feedUrl, string packageId, string version, string cacheDirectory, CancellationToken cancellationToken = default);

	Task<IReadOnlyList<CachedPackageInfo>> ListCachedPackagesAsync (string cacheDirectory, CancellationToken cancellationToken = default);

	Task DeleteCachedPackageAsync (CachedPackageInfo cachedPackage, CancellationToken cancellationToken = default);
	}
