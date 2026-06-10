using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.Core.Abstractions;

public interface ISftpDriverDeploymentService
	{
	Task UploadDriverAsync (string driverPackagePath, ProcessorConnectionInfo connectionInfo, CancellationToken cancellationToken = default);
	}
