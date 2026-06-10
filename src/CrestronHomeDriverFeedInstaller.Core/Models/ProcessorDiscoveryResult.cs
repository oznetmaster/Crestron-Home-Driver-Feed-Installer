namespace CrestronHomeDriverFeedInstaller.Core.Models;

public sealed class ProcessorDiscoveryResult
{
	public IReadOnlyList<DiscoveredProcessorInfo> Processors
		{
		get;
		init;
		} = Array.Empty<DiscoveredProcessorInfo>();

	public IReadOnlyList<string> Diagnostics
		{
		get;
		init;
		} = Array.Empty<string>();
}

