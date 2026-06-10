namespace NugetForCrestronDrivers.Core.Models;

public sealed class SavedProcessorCredential
{
    public string Host { get; init; } = string.Empty;

    public int Port { get; init; }

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
