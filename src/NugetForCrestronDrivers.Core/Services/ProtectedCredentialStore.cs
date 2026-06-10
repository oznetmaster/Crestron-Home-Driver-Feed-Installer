using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NugetForCrestronDrivers.Core.Abstractions;
using NugetForCrestronDrivers.Core.Models;

namespace NugetForCrestronDrivers.Core.Services;

public sealed class ProtectedCredentialStore : ICredentialStore
{
    private readonly string storeDirectory;

    public ProtectedCredentialStore()
    {
        storeDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NugetForCrestronDrivers", "Credentials");
        Directory.CreateDirectory(storeDirectory);
    }

    public async Task<SavedProcessorCredential?> GetAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        var path = GetPath(host, port);
        if (!File.Exists(path))
        {
            return null;
        }

        var encrypted = await File.ReadAllBytesAsync(path, cancellationToken);
        var decrypted = ProtectedData.Unprotect(encrypted, optionalEntropy: null, DataProtectionScope.CurrentUser);
        return JsonSerializer.Deserialize<SavedProcessorCredential>(decrypted);
    }

    public async Task SaveAsync(ProcessorConnectionInfo connectionInfo, CancellationToken cancellationToken = default)
    {
        var payload = new SavedProcessorCredential
        {
            Host = connectionInfo.Host,
            Port = connectionInfo.Port,
            Username = connectionInfo.Username,
            Password = connectionInfo.Password
        };

        var json = JsonSerializer.SerializeToUtf8Bytes(payload);
        var encrypted = ProtectedData.Protect(json, optionalEntropy: null, DataProtectionScope.CurrentUser);
        await File.WriteAllBytesAsync(GetPath(connectionInfo.Host, connectionInfo.Port), encrypted, cancellationToken);
    }

    private string GetPath(string host, int port)
    {
        var safeHost = string.Concat(host.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'));
        return Path.Combine(storeDirectory, $"{safeHost}_{port}.bin");
    }
}
