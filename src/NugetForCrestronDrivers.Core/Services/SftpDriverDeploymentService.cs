using Renci.SshNet;
using NugetForCrestronDrivers.Core.Abstractions;
using NugetForCrestronDrivers.Core.Models;

namespace NugetForCrestronDrivers.Core.Services;

public sealed class SftpDriverDeploymentService : ISftpDriverDeploymentService
{
    private const string ImportDirectory = "/user/ThirdPartyDrivers/Import";

    public Task UploadDriverAsync(string driverPackagePath, ProcessorConnectionInfo connectionInfo, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(driverPackagePath))
        {
            throw new FileNotFoundException("Driver package file was not found.", driverPackagePath);
        }

        return Task.Run(() =>
        {
            using var client = new SftpClient(connectionInfo.Host, connectionInfo.Port, connectionInfo.Username, connectionInfo.Password);
            client.Connect();

            if (!client.Exists(ImportDirectory))
            {
                throw new InvalidOperationException($"The processor import folder '{ImportDirectory}' was not found.");
            }

            using var stream = File.OpenRead(driverPackagePath);
            var remotePath = $"{ImportDirectory}/{Path.GetFileName(driverPackagePath)}";
            client.UploadFile(stream, remotePath, canOverride: true);
            client.Disconnect();
        }, cancellationToken);
    }
}
