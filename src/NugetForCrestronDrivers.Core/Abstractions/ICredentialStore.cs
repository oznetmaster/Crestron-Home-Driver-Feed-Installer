using NugetForCrestronDrivers.Core.Models;

namespace NugetForCrestronDrivers.Core.Abstractions;

public interface ICredentialStore
{
    Task<SavedProcessorCredential?> GetAsync(string host, int port, CancellationToken cancellationToken = default);

    Task SaveAsync(ProcessorConnectionInfo connectionInfo, CancellationToken cancellationToken = default);
}
