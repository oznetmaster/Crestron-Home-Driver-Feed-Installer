using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

using CrestronHomeDriverFeedInstaller.Core.Abstractions;
using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.Core.Services;

public sealed class NuGetPackageService : INuGetPackageService
	{
	private static readonly string[] _requiredTags = ["crestron", "crestron-home", "driver", "pkg"];
	private const int SEARCH_PAGE_SIZE = 50;
	private const int MAX_SEARCH_PAGES = 5;

	public async Task<IReadOnlyList<PackageSearchResult>> SearchPackagesAsync (string feedUrl, string searchTerm, int take, bool onlyV1Compliant, CancellationToken cancellationToken = default)
		{
		var repository = CreateRepository (feedUrl);
		var searchResource = await repository.GetResourceAsync<PackageSearchResource> (cancellationToken);
		var filter = new SearchFilter (includePrerelease: false);

		var acceptedResults = new List<PackageSearchResult> ();
		var seenPackages = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
		var candidateLimit = onlyV1Compliant ? take : Math.Max (take * 3, take);

		for (var pageIndex = 0; pageIndex < MAX_SEARCH_PAGES; pageIndex++)
			{
			var skip = pageIndex * SEARCH_PAGE_SIZE;
			var page = (await searchResource.SearchAsync (searchTerm, filter, skip, SEARCH_PAGE_SIZE, NullLogger.Instance, cancellationToken)).ToArray ();
			if (page.Length == 0)
				{
				break;
				}

			foreach (var metadata in page)
				{
				cancellationToken.ThrowIfCancellationRequested ();

				var packageKey = $"{metadata.Identity.Id}|{metadata.Identity.Version.ToNormalizedString ()}";
				if (!seenPackages.Add (packageKey))
					{
					continue;
					}

				var tags = SplitTags (metadata.Tags);
				var hasRequiredMetadata = HasRequiredMetadata (tags);

				var result = new PackageSearchResult
					{
					Id = metadata.Identity.Id,
					Version = metadata.Identity.Version.ToNormalizedString (),
					Description = metadata.Description ?? string.Empty,
					Authors = metadata.Authors ?? string.Empty,
					DownloadCount = metadata.DownloadCount,
					HasPackageTypes = false,
					IsV1Compliant = false
					};

				if (!onlyV1Compliant)
					{
					acceptedResults.Add (new PackageSearchResult
						{
						Id = result.Id,
						Version = result.Version,
						Description = result.Description,
						Authors = result.Authors,
						DownloadCount = result.DownloadCount,
						HasPackageTypes = result.HasPackageTypes,
						IsV1Compliant = hasRequiredMetadata && await IsV1CompliantPackageAsync (feedUrl, metadata.Identity.Id, metadata.Identity.Version.ToNormalizedString (), cancellationToken)
						});

					if (acceptedResults.Count >= candidateLimit)
						{
						return acceptedResults
							.OrderBy (searchResult => searchResult.Id)
							.Take (take)
							.ToArray ();
						}

					continue;
					}

				if (!hasRequiredMetadata)
					{
					continue;
					}

				if (!await IsV1CompliantPackageAsync (feedUrl, metadata.Identity.Id, metadata.Identity.Version.ToNormalizedString (), cancellationToken))
					{
					continue;
					}

				acceptedResults.Add (new PackageSearchResult
					{
					Id = result.Id,
					Version = result.Version,
					Description = result.Description,
					Authors = result.Authors,
					DownloadCount = result.DownloadCount,
					HasPackageTypes = result.HasPackageTypes,
					IsV1Compliant = true
					});

				if (acceptedResults.Count >= take)
					{
					return acceptedResults
						.OrderBy (searchResult => searchResult.Id)
						.ToArray ();
					}
				}

			if (page.Length < SEARCH_PAGE_SIZE)
				{
				continue;
				}
			}

		return acceptedResults
			.OrderBy (searchResult => searchResult.Id)
			.Take (take)
			.ToArray ();
		}

	public async Task<string> DownloadPackageAsync (string feedUrl, string packageId, string version, string cacheDirectory, CancellationToken cancellationToken = default)
		{
		Directory.CreateDirectory (cacheDirectory);

		var packagePath = Path.Combine (cacheDirectory, $"{packageId}.{version}.nupkg");
		if (File.Exists (packagePath))
			{
			return packagePath;
			}

		var repository = CreateRepository (feedUrl);
		var downloadResource = await repository.GetResourceAsync<DownloadResource> (cancellationToken);
		var packageIdentity = new PackageIdentity (packageId, NuGetVersion.Parse (version));
		var cache = new SourceCacheContext ();
		var result = await downloadResource.GetDownloadResourceResultAsync (
			 packageIdentity,
			 new PackageDownloadContext (cache),
			 SettingsUtility.GetGlobalPackagesFolder (Settings.LoadDefaultSettings (root: null)),
			 NullLogger.Instance,
			 cancellationToken);

		if (result.Status != DownloadResourceResultStatus.Available || result.PackageStream is null)
			{
			throw new InvalidOperationException ($"Package '{packageId} {version}' could not be downloaded from '{feedUrl}'.");
			}

		await using var sourceStream = result.PackageStream;
		await using var targetStream = File.Create (packagePath);
		await sourceStream.CopyToAsync (targetStream, cancellationToken);
		await targetStream.FlushAsync (cancellationToken);

		return packagePath;
		}

	public Task<IReadOnlyList<CachedPackageInfo>> ListCachedPackagesAsync (string cacheDirectory, CancellationToken cancellationToken = default)
		{
		Directory.CreateDirectory (cacheDirectory);
		var extractedRoot = Path.Combine (cacheDirectory, "Extracted");
		var extractedPackages = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
		if (Directory.Exists (extractedRoot))
			{
			foreach (var extractedFile in Directory.EnumerateFiles (extractedRoot, "*.pkg", SearchOption.AllDirectories))
				{
				var relativePath = Path.GetRelativePath (extractedRoot, extractedFile).Split (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				if (relativePath.Length >= 3)
					{
					extractedPackages[$"{relativePath[0]}|{relativePath[1]}"] = extractedFile;
					}
				}
			}

		var cachedPackages = new List<CachedPackageInfo> ();
		foreach (var packagePath in Directory.EnumerateFiles (cacheDirectory, "*.nupkg", SearchOption.TopDirectoryOnly))
			{
				cancellationToken.ThrowIfCancellationRequested ();
				using var packageArchiveReader = new PackageArchiveReader (packagePath);
				var nuspecReader = packageArchiveReader.NuspecReader;
				var packageId = nuspecReader.GetId ();
				var version = nuspecReader.GetVersion ().ToNormalizedString ();
				extractedPackages.TryGetValue ($"{packageId}|{version}", out var extractedPackagePath);

				cachedPackages.Add (new CachedPackageInfo
					{
					PackageId = packageId,
					Version = version,
					PackageArchivePath = packagePath,
					ExtractedDriverPackagePath = extractedPackagePath
					});
			}

		return Task.FromResult<IReadOnlyList<CachedPackageInfo>> (
			cachedPackages
				.OrderBy (cachedPackage => cachedPackage.PackageId)
				.ThenByDescending (cachedPackage => cachedPackage.Version)
				.ToArray ());
		}

	public Task DeleteCachedPackageAsync (CachedPackageInfo cachedPackage, CancellationToken cancellationToken = default)
		{
		if (File.Exists (cachedPackage.PackageArchivePath))
			{
			File.Delete (cachedPackage.PackageArchivePath);
			}

		if (!string.IsNullOrWhiteSpace (cachedPackage.ExtractedDriverPackagePath))
			{
			var extractedVersionDirectory = Path.GetDirectoryName (cachedPackage.ExtractedDriverPackagePath);
			if (!string.IsNullOrWhiteSpace (extractedVersionDirectory) && Directory.Exists (extractedVersionDirectory))
				{
				Directory.Delete (extractedVersionDirectory, recursive: true);
				}
			}

		return Task.CompletedTask;
		}

	private static SourceRepository CreateRepository (string feedUrl)
		{
		var providers = Repository.Provider.GetCoreV3 ();
		return new SourceRepository (new PackageSource (feedUrl), providers);
		}

	private static bool HasRequiredMetadata (ISet<string> tags)
		{
		return _requiredTags.All (tags.Contains);
		}

	private async Task<bool> IsV1CompliantPackageAsync (string feedUrl, string packageId, string version, CancellationToken cancellationToken)
		{
		var tempRoot = Path.Combine (Path.GetTempPath (), "NugetForCrestronDrivers", "SearchValidation", packageId, version);
		Directory.CreateDirectory (tempRoot);

		var packagePath = await DownloadPackageAsync (feedUrl, packageId, version, tempRoot, cancellationToken);
		using var packageArchiveReader = new PackageArchiveReader (packagePath);
		var packageFiles = (await packageArchiveReader.GetFilesAsync (cancellationToken)).ToArray ();
		var rootPkgFiles = packageFiles.Where (file => !file.Contains ('/') && file.EndsWith (".pkg", StringComparison.OrdinalIgnoreCase)).ToArray ();
		var hasManifest = packageFiles.Any (file => string.Equals (file, "crestron-driver-package.json", StringComparison.OrdinalIgnoreCase));
		var hasLibDll = packageFiles.Any (file => file.StartsWith ("lib/", StringComparison.OrdinalIgnoreCase) && file.EndsWith (".dll", StringComparison.OrdinalIgnoreCase));

		return rootPkgFiles.Length == 1 && hasManifest && !hasLibDll;
		}

	private static ISet<string> SplitTags (string? tags)
		{
		return tags?
			.Split ([' ', ';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select (tag => tag.ToLowerInvariant ())
			.ToHashSet (StringComparer.OrdinalIgnoreCase)
			?? new HashSet<string> (StringComparer.OrdinalIgnoreCase);
		}
	}
