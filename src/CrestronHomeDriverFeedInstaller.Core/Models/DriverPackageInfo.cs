// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace CrestronHomeDriverFeedInstaller.Core.Models;

public sealed class DriverPackageInfo
	{
	public string PackageId { get; init; } = string.Empty;

	public string Version { get; init; } = string.Empty;

	public string PackageArchivePath { get; init; } = string.Empty;

	public string DriverPackagePath { get; init; } = string.Empty;

	public string CrestronDriverPackageJsonContent { get; init; } = string.Empty;

	public IReadOnlyList<string> Entries { get; init; } = Array.Empty<string> ();
	}
