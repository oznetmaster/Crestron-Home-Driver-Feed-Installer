// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.Core.Abstractions;

public interface ISftpDriverDeploymentService
	{
	Task UploadDriverAsync (string driverPackagePath, ProcessorConnectionInfo connectionInfo, CancellationToken cancellationToken = default);
	}
