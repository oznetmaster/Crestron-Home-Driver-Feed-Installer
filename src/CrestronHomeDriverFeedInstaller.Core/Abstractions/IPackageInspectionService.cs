// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.Core.Abstractions;

public interface IPackageInspectionService
	{
	Task<DriverPackageInfo> InspectPackageAsync (string packageId, string version, string packageArchivePath, string workingDirectory, CancellationToken cancellationToken = default);
	}
