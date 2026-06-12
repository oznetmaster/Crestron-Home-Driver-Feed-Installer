// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;

using CrestronHomeDriverFeedInstaller.Core.Abstractions;
using CrestronHomeDriverFeedInstaller.Core.Models;

namespace CrestronHomeDriverFeedInstaller.Core.Services;

public sealed class JsonAppSettingsStore : IAppSettingsStore
	{
	private static readonly JsonSerializerOptions SerializerOptions = new ()
		{
		WriteIndented = true
		};

	private readonly string settingsPath;
	private readonly IAppDataPathProvider appDataPathProvider;

	public JsonAppSettingsStore (IAppDataPathProvider appDataPathProvider)
		{
		this.appDataPathProvider = appDataPathProvider;
		var settingsDirectory = appDataPathProvider.ApplicationDataDirectory;
		Directory.CreateDirectory (settingsDirectory);
		settingsPath = Path.Combine (settingsDirectory, "settings.json");
		}

	public async Task<AppSettings> LoadAsync (CancellationToken cancellationToken = default)
		{
		return await Task.Run (Load, cancellationToken);
		}

	public async Task SaveAsync (AppSettings settings, CancellationToken cancellationToken = default)
		{
		await Task.Run (() => Save (settings), cancellationToken);
		}

	public AppSettings Load ()
		{
		if (!File.Exists (settingsPath))
			{
			return CreateDefaultSettings ();
			}

		try
			{
			using var stream = File.OpenRead (settingsPath);
			var settings = JsonSerializer.Deserialize<AppSettings> (stream, SerializerOptions) ?? CreateDefaultSettings ();
			ApplyDefaults (settings);
			return settings;
			}
		catch (Exception)
			{
			return CreateDefaultSettings ();
			}
		}

	public void Save (AppSettings settings)
		{
		ApplyDefaults (settings);
		using var stream = File.Create (settingsPath);
		JsonSerializer.Serialize (stream, settings, SerializerOptions);
		}

	private AppSettings CreateDefaultSettings ()
		{
		return new AppSettings
			{
			CacheDirectory = appDataPathProvider.CacheDirectory
			};
		}

	private void ApplyDefaults (AppSettings settings)
		{
		if (string.IsNullOrWhiteSpace (settings.CacheDirectory))
			{
			settings.CacheDirectory = appDataPathProvider.CacheDirectory;
			}
		}
	}