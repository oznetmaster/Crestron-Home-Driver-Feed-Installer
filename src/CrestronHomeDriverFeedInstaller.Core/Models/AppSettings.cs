// Copyright ©2026 Neil Colvin
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace CrestronHomeDriverFeedInstaller.Core.Models;

public sealed class AppSettings
	{
	public string FeedUrl { get; set; } = "https://api.nuget.org/v3/index.json";

	public string CacheDirectory { get; set; } = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), "CrestronHomeDriverFeedInstaller", "Cache");

	public double? WindowLeft { get; set; }

	public double? WindowTop { get; set; }

	public double? WindowWidth { get; set; }

	public double? WindowHeight { get; set; }

	public string? LastUsedProcessorHost { get; set; }
	}
