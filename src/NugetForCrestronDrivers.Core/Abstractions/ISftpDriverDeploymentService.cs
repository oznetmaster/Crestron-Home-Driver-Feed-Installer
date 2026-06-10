using NugetForCrestronDrivers.Core.Models;

namespace NugetForCrestronDrivers.Core.Abstractions;

public interface ISftpDriverDeploymentService
{
    Task UploadDriverAsync(string driverPackagePath, ProcessorConnectionInfo connectionInfo, CancellationToken cancellationToken = default);
}
