// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using CrestronHomeDriverFeedInstaller.Core.Abstractions;
using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.Core.Services;

public sealed class ProtectedCredentialStore : ICredentialStore
	{
	private readonly string storeDirectory;

	public ProtectedCredentialStore ()
		{
		storeDirectory = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), "CrestronHomeDriverFeedInstaller", "Credentials");
		Directory.CreateDirectory (storeDirectory);
		}

	public async Task<IReadOnlyList<SavedProcessorCredential>> ListAsync (CancellationToken cancellationToken = default)
		{
		var results = new List<SavedProcessorCredential> ();
		foreach (var path in Directory.EnumerateFiles (storeDirectory, "*.bin", SearchOption.TopDirectoryOnly))
			{
			cancellationToken.ThrowIfCancellationRequested ();

			try
				{
				var encrypted = await File.ReadAllBytesAsync (path, cancellationToken);
				var decrypted = ProtectedData.Unprotect (encrypted, optionalEntropy: null, DataProtectionScope.CurrentUser);
				var credential = JsonSerializer.Deserialize<SavedProcessorCredential> (decrypted);
				if (credential is not null)
					{
					results.Add (credential);
					}
				}
			catch (Exception)
				{
				// Ignore unreadable credential entries so one corrupt file does not block the list.
				}
			}

		return results
			 .OrderBy (credential => credential.DisplayName)
			 .ToArray ();
		}

	public async Task<SavedProcessorCredential?> GetAsync (string host, int port, CancellationToken cancellationToken = default)
		{
		var path = GetPath (host, port);
		if (!File.Exists (path))
			{
			return null;
			}

		var encrypted = await File.ReadAllBytesAsync (path, cancellationToken);
		var decrypted = ProtectedData.Unprotect (encrypted, optionalEntropy: null, DataProtectionScope.CurrentUser);
		return JsonSerializer.Deserialize<SavedProcessorCredential> (decrypted);
		}

	public async Task SaveAsync (ProcessorConnectionInfo connectionInfo, CancellationToken cancellationToken = default)
		{
		var payload = new SavedProcessorCredential
			{
			DisplayName = connectionInfo.DisplayName,
			Host = connectionInfo.Host,
			Port = connectionInfo.Port,
			Username = connectionInfo.Username,
			Password = connectionInfo.Password
			};

		var json = JsonSerializer.SerializeToUtf8Bytes (payload);
		var encrypted = ProtectedData.Protect (json, optionalEntropy: null, DataProtectionScope.CurrentUser);
		await File.WriteAllBytesAsync (GetPath (connectionInfo.Host, connectionInfo.Port), encrypted, cancellationToken);
		}

	public Task DeleteAsync (string host, int port, CancellationToken cancellationToken = default)
		{
		var path = GetPath (host, port);
		if (File.Exists (path))
			{
			File.Delete (path);
			}

		return Task.CompletedTask;
		}

	private string GetPath (string host, int port)
		{
		var safeHost = string.Concat (host.Select (ch => char.IsLetterOrDigit (ch) ? ch : '_'));
		return Path.Combine (storeDirectory, $"{safeHost}_{port}.bin");
		}
	}
