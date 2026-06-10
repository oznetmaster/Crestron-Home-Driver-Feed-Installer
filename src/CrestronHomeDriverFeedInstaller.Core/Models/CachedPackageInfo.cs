namespace CrestronHomeDriverFeedInstaller.Core.Models;

public sealed class CachedPackageInfo
	{
	public string CacheKey => $"{PackageId}|{Version}";

	public string PackageId
		{
		get;
		init;
		} = string.Empty;

	public string Version
		{
		get;
		init;
		} = string.Empty;

	public string PackageArchivePath
		{
		get;
		init;
		} = string.Empty;

	public string? ExtractedDriverPackagePath
		{
		get;
		init;
		}

	public string DisplayLabel => $"{PackageId} {Version}";
	}

