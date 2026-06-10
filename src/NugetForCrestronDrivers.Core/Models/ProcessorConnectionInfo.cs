namespace NugetForCrestronDrivers.Core.Models;

public sealed class ProcessorConnectionInfo
{
    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 22;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public bool RememberCredentials { get; init; }
}
