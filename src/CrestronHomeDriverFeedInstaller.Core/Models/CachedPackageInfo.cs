// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

	public string Authors
		{
		get;
		init;
		} = string.Empty;

	public string Description
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

	public string? LatestAvailableVersion
		{
		get;
		set;
		}

	public bool HasNewerVersionAvailable
		{
		get;
		set;
		}

	public string DisplayLabel => $"{PackageId} {Version}";
	}

