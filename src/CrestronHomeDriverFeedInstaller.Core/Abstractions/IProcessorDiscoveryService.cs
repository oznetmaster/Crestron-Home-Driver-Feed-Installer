using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.Core.Abstractions;

public interface IProcessorDiscoveryService
	{
	Task<ProcessorDiscoveryResult> DiscoverAsync (TimeSpan timeout, CancellationToken cancellationToken = default);
	}

