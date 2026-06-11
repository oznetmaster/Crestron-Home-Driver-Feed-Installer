// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace CrestronHomeDriverFeedInstaller.Core.Models;

public sealed class PackageSearchResult
	{
	public string Id { get; init; } = string.Empty;

	public string Version { get; init; } = string.Empty;

	public string Description { get; init; } = string.Empty;

	public string Authors { get; init; } = string.Empty;

	public long? DownloadCount
		{
		get; init;
		}

	public bool HasPackageTypes
		{
		get; init;
		}

	public bool IsV1Compliant
		{
		get; init;
		}
	}
