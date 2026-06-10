using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.Core.Abstractions;

public interface ICredentialStore
	{
	Task<IReadOnlyList<SavedProcessorCredential>> ListAsync (CancellationToken cancellationToken = default);

	Task<SavedProcessorCredential?> GetAsync (string host, int port, CancellationToken cancellationToken = default);

	Task SaveAsync (ProcessorConnectionInfo connectionInfo, CancellationToken cancellationToken = default);

	Task DeleteAsync (string host, int port, CancellationToken cancellationToken = default);
	}
