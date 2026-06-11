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

	public JsonAppSettingsStore ()
		{
		var settingsDirectory = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), "CrestronHomeDriverFeedInstaller");
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
			return new AppSettings (); 
			}

		try
			{
			using var stream = File.OpenRead (settingsPath);
			return JsonSerializer.Deserialize<AppSettings> (stream, SerializerOptions) ?? new AppSettings ();
			}
		catch (Exception)
			{
			return new AppSettings ();
			}
		}

	public void Save (AppSettings settings)
		{
		using var stream = File.Create (settingsPath);
		JsonSerializer.Serialize (stream, settings, SerializerOptions);
		}
	}